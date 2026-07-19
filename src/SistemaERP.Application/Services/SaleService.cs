using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Application.Services;

namespace SistemaERP.Application.Services
{
    public class SaleService : ISaleService
    {
        private const decimal TAX_RATE = 0.18m; // IGV 18%
        private const string NUMBER_PREFIX = "VEN-";

        private readonly ISaleRepository _saleRepository;
        private readonly IStockMovementService _stockMovementService;
        private readonly IProductRepository _productRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly ICashRegisterService _cashRegisterService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SaleService> _logger;

        public SaleService(
            ISaleRepository saleRepository,
            IStockMovementService stockMovementService,
            IProductRepository productRepository,
            ITenantRepository tenantRepository,
            ICashRegisterService cashRegisterService,
            IUnitOfWork unitOfWork,
            ILogger<SaleService> logger)
        {
            _saleRepository = saleRepository;
            _stockMovementService = stockMovementService;
            _productRepository = productRepository;
            _tenantRepository = tenantRepository;
            _cashRegisterService = cashRegisterService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Sale>> GetAllAsync()
        {
            return await _saleRepository.GetAllAsync();
        }

        public async Task<Sale?> GetByIdAsync(Guid id)
        {
            return await _saleRepository.GetByIdAsync(id);
        }

        public async Task<Sale> CreateDraftAsync(Sale sale)
        {
            ValidateItems(sale);
            ValidatePaymentTerms(sale);

            sale.SaleNumber = await GenerateNextSaleNumberAsync(sale.TenantId);
            RecalculateTotals(sale);
            CalculateDueDate(sale);
            sale.PaymentStatus = sale.PaymentType == PaymentType.Cash
                ? PaymentStatus.Paid
                : PaymentStatus.Pending;
            sale.Status = SaleStatus.Draft;

            _logger.LogInformation("Creating sale draft {SaleNumber} for tenant {TenantId}.",
                sale.SaleNumber, sale.TenantId);

            return await _saleRepository.AddAsync(sale);
        }

        public async Task<Sale> UpdateDraftAsync(Sale sale)
        {
            var existing = await _saleRepository.GetByIdAsync(sale.Id);
            if (existing == null)
                throw new InvalidOperationException("La venta no existe.");

            if (existing.Status != SaleStatus.Draft)
                throw new InvalidOperationException(
                    "Solo se puede editar una venta en estado Borrador (Draft).");

            ValidateItems(sale);
            ValidatePaymentTerms(sale);

            // Mantener el número y la fecha de creación originales.
            existing.CustomerId = sale.CustomerId;
            existing.WarehouseId = sale.WarehouseId;
            existing.VoucherType = sale.VoucherType;
            existing.VoucherNumber = sale.VoucherNumber;
            existing.SaleDate = sale.SaleDate;
            existing.Currency = sale.Currency;
            existing.ExchangeRate = sale.ExchangeRate;
            existing.PaymentType = sale.PaymentType;
            existing.PaymentMethod = sale.PaymentMethod;
            existing.CreditDays = sale.CreditDays;
            existing.Observations = sale.Observations;

            // Reemplazar los items: se eliminan los previos y se agregan los nuevos.
            existing.Items.Clear();
            foreach (var item in sale.Items)
            {
                existing.Items.Add(new SaleItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountPercentage = item.DiscountPercentage,
                    TaxPercentage = item.TaxPercentage,
                    LineSubtotal = item.LineSubtotal,
                    LineTax = item.LineTax,
                    LineTotal = item.LineTotal,
                });
            }

            RecalculateTotals(existing);
            CalculateDueDate(existing);
            existing.PaymentStatus = existing.PaymentType == PaymentType.Cash
                ? PaymentStatus.Paid
                : PaymentStatus.Pending;

            _logger.LogInformation("Updating sale draft {SaleId}.", existing.Id);
            return await _saleRepository.UpdateAsync(existing);
        }

        public async Task<StockValidationResult> ValidateStockAsync(Guid tenantId, IEnumerable<SaleItem> items)
        {
            var result = new StockValidationResult();

            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            // Si no se puede resolver el tenant, no se bloquea aquí; la confirmación
            // tiene su propia validación. Devolvemos un resultado sin errores de stock.
            if (tenant == null)
                return result;

            // Si el tenant permite stock negativo, no validamos disponibilidad.
            if (tenant.AllowNegativeStock)
                return result;

            foreach (var item in items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                var available = product?.Stock ?? 0;

                if (item.Quantity > available)
                {
                    result.Errors.Add(new StockValidationError
                    {
                        ProductId = item.ProductId,
                        ProductName = product?.Name,
                        RequestedQuantity = item.Quantity,
                        AvailableStock = available,
                    });
                }
            }

            return result;
        }

        public async Task ConfirmAsync(Guid saleId, Guid userId)
        {
            var sale = await _saleRepository.GetByIdAsync(saleId);
            if (sale == null)
                throw new InvalidOperationException("La venta no existe.");

            if (sale.Status != SaleStatus.Draft)
                throw new InvalidOperationException(
                    "Solo se puede confirmar una venta en estado Borrador (Draft).");

            // Validación previa de stock: si hay errores, no iniciamos la transacción.
            var stockResult = await ValidateStockAsync(sale.TenantId, sale.Items);
            if (!stockResult.IsValid)
            {
                var detail = string.Join("; ",
                    stockResult.Errors.Select(e =>
                        $"{e.ProductName ?? e.ProductId.ToString()}: solicitado {e.RequestedQuantity}, disponible {e.AvailableStock}"));
                throw new InvalidOperationException(
                    $"Stock insuficiente para confirmar la venta. {detail}");
            }

            // Para ventas al contado, la caja abierta es pre-requisito. Se resuelve ANTES
            // de tocar el stock para fallar temprano sin efectos parciales.
            CashRegister? openCashRegister = null;
            if (sale.PaymentType == PaymentType.Cash)
            {
                openCashRegister = await _cashRegisterService.GetOpenCashRegisterForUserAsync(
                    sale.TenantId, userId);
                if (openCashRegister == null)
                    throw new InvalidOperationException(
                        "Debe abrir una caja antes de confirmar ventas al contado.");
            }

            // Transacción: registrar salidas de stock, el movimiento de caja (si aplica) y
            // confirmar la venta de forma atómica.
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var item in sale.Items)
                {
                    await _stockMovementService.CreateAsync(new StockMovement
                    {
                        TenantId = sale.TenantId,
                        ProductId = item.ProductId,
                        Type = StockMovementType.Salida,
                        Quantity = item.Quantity,
                        Reason = $"Venta {sale.SaleNumber}",
                    });
                }

                // Movimiento de caja para ventas al contado (después de descontar el stock).
                if (sale.PaymentType == PaymentType.Cash && openCashRegister != null)
                {
                    await _cashRegisterService.RegisterMovementAsync(
                        cashRegisterId: openCashRegister.Id,
                        type: CashMovementType.Income,
                        reason: MovementReason.Sale,
                        paymentMethod: sale.PaymentMethod ?? PaymentMethod.Cash,
                        amount: sale.Total,
                        description: $"Venta {sale.SaleNumber}",
                        saleId: sale.Id,
                        userId: userId);
                }

                sale.Status = SaleStatus.Confirmed;
                sale.ConfirmedBy = userId;
                sale.ConfirmedAt = DateTime.UtcNow;

                await _saleRepository.UpdateAsync(sale);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Sale {SaleNumber} confirmed by user {UserId}.",
                    sale.SaleNumber, userId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Sale {SaleId} confirmation failed. Rolling back.", saleId);
                throw new InvalidOperationException(
                    "No se pudo confirmar la venta. Se revirtió la operación (stock no actualizado).", ex);
            }
        }

        public async Task CancelAsync(Guid saleId)
        {
            var sale = await _saleRepository.GetByIdAsync(saleId);
            if (sale == null)
                throw new InvalidOperationException("La venta no existe.");

            if (sale.Status == SaleStatus.Cancelled)
                throw new InvalidOperationException("La venta ya está cancelada.");

            // NOTA (simplificación consciente): si la venta estaba Confirmed, cancelar
            // NO revierte el stock automáticamente por ahora. No se generan movimientos
            // de entrada compensatorios. Esto se podría mejorar en el futuro, pero se
            // deja fuera de alcance deliberadamente.
            sale.Status = SaleStatus.Cancelled;

            _logger.LogInformation("Sale {SaleId} cancelled (status was {Status}).",
                saleId, sale.Status);
            await _saleRepository.UpdateAsync(sale);
        }

        public async Task DeleteAsync(Guid saleId)
        {
            var sale = await _saleRepository.GetByIdAsync(saleId);
            if (sale == null)
                throw new InvalidOperationException("La venta no existe.");

            if (sale.Status != SaleStatus.Draft)
                throw new InvalidOperationException(
                    "Solo se puede eliminar una venta en estado Borrador (Draft). Las ventas confirmadas o canceladas nunca se borran físicamente, por trazabilidad.");

            await _saleRepository.DeleteAsync(saleId);
        }

        // --- Helpers ---

        private async Task<string> GenerateNextSaleNumberAsync(Guid tenantId)
        {
            var last = await _saleRepository.GetLastSaleNumberAsync(tenantId);
            int next = 1;
            if (!string.IsNullOrEmpty(last) && last.StartsWith(NUMBER_PREFIX))
            {
                var numericPart = last.Substring(NUMBER_PREFIX.Length);
                if (int.TryParse(numericPart, out var lastNumber))
                    next = lastNumber + 1;
            }
            return $"{NUMBER_PREFIX}{next:D6}";
        }

        private static void RecalculateTotals(Sale sale)
        {
            decimal subtotal = 0;
            foreach (var item in sale.Items)
            {
                item.LineSubtotal = CalculateLineSubtotal(item);
                item.LineTax = Math.Round(item.LineSubtotal * item.TaxPercentage / 100m, 2);
                item.LineTotal = item.LineSubtotal + item.LineTax;
                subtotal += item.LineSubtotal;
            }
            sale.Subtotal = subtotal;
            sale.Tax = Math.Round(sale.Items.Sum(i => i.LineTax), 2);
            sale.Total = subtotal + sale.Tax;
        }

        private static decimal CalculateLineSubtotal(SaleItem item)
        {
            return item.Quantity * item.UnitPrice * (1 - item.DiscountPercentage / 100m);
        }

        private static void CalculateDueDate(Sale sale)
        {
            if (sale.PaymentType == PaymentType.Credit && sale.CreditDays.HasValue)
            {
                sale.DueDate = sale.SaleDate.AddDays(sale.CreditDays.Value);
            }
            else
            {
                sale.DueDate = null;
            }
        }

        private static void ValidateItems(Sale sale)
        {
            if (sale.Items == null || !sale.Items.Any())
                throw new InvalidOperationException("La venta debe tener al menos un item.");

            foreach (var item in sale.Items)
            {
                if (item.Quantity <= 0)
                    throw new InvalidOperationException("La cantidad debe ser mayor a cero en todos los items.");
                if (item.UnitPrice <= 0)
                    throw new InvalidOperationException("El precio unitario debe ser mayor a cero en todos los items.");
            }
        }

        private static void ValidatePaymentTerms(Sale sale)
        {
            if (sale.PaymentType == PaymentType.Cash && sale.PaymentMethod == null)
                throw new InvalidOperationException("Debe indicar el método de pago para ventas al contado.");

            if (sale.PaymentType == PaymentType.Credit &&
                (!sale.CreditDays.HasValue || sale.CreditDays.Value <= 0))
                throw new InvalidOperationException("Debe indicar los días de crédito para ventas a crédito.");
        }
    }
}
