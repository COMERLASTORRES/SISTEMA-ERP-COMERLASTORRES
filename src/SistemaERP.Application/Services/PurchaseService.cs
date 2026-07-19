using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Application.Services;

namespace SistemaERP.Application.Services
{
    public class PurchaseService : IPurchaseService
    {
        private const decimal TAX_RATE = 0.18m; // IGV 18%
        private const string NUMBER_PREFIX = "PUR-";

        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IStockMovementService _stockMovementService;
        private readonly ICashRegisterService _cashRegisterService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PurchaseService> _logger;

        public PurchaseService(
            IPurchaseRepository purchaseRepository,
            IStockMovementService stockMovementService,
            ICashRegisterService cashRegisterService,
            IUnitOfWork unitOfWork,
            ILogger<PurchaseService> logger)
        {
            _purchaseRepository = purchaseRepository;
            _stockMovementService = stockMovementService;
            _cashRegisterService = cashRegisterService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Purchase>> GetAllAsync()
        {
            return await _purchaseRepository.GetAllAsync();
        }

        public async Task<Purchase?> GetByIdAsync(Guid id)
        {
            return await _purchaseRepository.GetByIdAsync(id);
        }

        public async Task<Purchase> CreateDraftAsync(Purchase purchase)
        {
            ValidateItems(purchase);
            ValidatePaymentTerms(purchase);

            purchase.PurchaseNumber = await GenerateNextPurchaseNumberAsync(purchase.TenantId);
            RecalculateTotals(purchase);
            CalculateDueDate(purchase);
            purchase.PaymentStatus = purchase.PaymentType == PaymentType.Cash
                ? PaymentStatus.Paid
                : PaymentStatus.Pending;
            purchase.Status = PurchaseStatus.Draft;

            _logger.LogInformation("Creating purchase draft {PurchaseNumber} for tenant {TenantId}.",
                purchase.PurchaseNumber, purchase.TenantId);

            return await _purchaseRepository.AddAsync(purchase);
        }

        public async Task<Purchase> UpdateDraftAsync(Purchase purchase)
        {
            var existing = await _purchaseRepository.GetByIdAsync(purchase.Id);
            if (existing == null)
                throw new InvalidOperationException("La compra no existe.");

            if (existing.Status != PurchaseStatus.Draft)
                throw new InvalidOperationException(
                    "Solo se puede editar una compra en estado Borrador (Draft).");

            ValidateItems(purchase);
            ValidatePaymentTerms(purchase);

            // Mantener el número y la fecha de creación originales.
            existing.SupplierId = purchase.SupplierId;
            existing.WarehouseId = purchase.WarehouseId;
            existing.VoucherType = purchase.VoucherType;
            existing.VoucherNumber = purchase.VoucherNumber;
            existing.PurchaseDate = purchase.PurchaseDate;
            existing.Currency = purchase.Currency;
            existing.ExchangeRate = purchase.ExchangeRate;
            existing.PaymentType = purchase.PaymentType;
            existing.PaymentMethod = purchase.PaymentMethod;
            existing.CreditDays = purchase.CreditDays;
            existing.Observations = purchase.Observations;

            // Reemplazar los items: se eliminan los previos y se agregan los nuevos.
            existing.Items.Clear();
            foreach (var item in purchase.Items)
            {
                existing.Items.Add(new PurchaseItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    DiscountPercentage = item.DiscountPercentage,
                    LineSubtotal = CalculateLineSubtotal(item),
                });
            }

            RecalculateTotals(existing);
            CalculateDueDate(existing);
            existing.PaymentStatus = existing.PaymentType == PaymentType.Cash
                ? PaymentStatus.Paid
                : PaymentStatus.Pending;

            _logger.LogInformation("Updating purchase draft {PurchaseId}.", existing.Id);
            return await _purchaseRepository.UpdateAsync(existing);
        }

        public async Task ConfirmAsync(Guid purchaseId, Guid userId)
        {
            var purchase = await _purchaseRepository.GetByIdAsync(purchaseId);
            if (purchase == null)
                throw new InvalidOperationException("La compra no existe.");

            if (purchase.Status != PurchaseStatus.Draft)
                throw new InvalidOperationException(
                    "Solo se puede confirmar una compra en estado Borrador (Draft).");

            // Para compras al contado, la caja abierta es pre-requisito. Se resuelve ANTES
            // de tocar el stock para fallar temprano sin efectos parciales.
            CashRegister? openCashRegister = null;
            if (purchase.PaymentType == PaymentType.Cash)
            {
                openCashRegister = await _cashRegisterService.GetOpenCashRegisterForUserAsync(
                    purchase.TenantId, userId);
                if (openCashRegister == null)
                    throw new InvalidOperationException(
                        "Debe abrir una caja antes de confirmar compras al contado.");
            }

            // Transacción: registrar entradas de stock, el movimiento de caja (si aplica) y
            // confirmar la compra de forma atómica.
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var item in purchase.Items)
                {
                    await _stockMovementService.CreateAsync(new StockMovement
                    {
                        TenantId = purchase.TenantId,
                        ProductId = item.ProductId,
                        Type = StockMovementType.Entrada,
                        Quantity = item.Quantity,
                        Reason = $"Compra {purchase.PurchaseNumber}",
                    });
                }

                // Egreso de caja para compras al contado (después de registrar el ingreso de stock).
                if (purchase.PaymentType == PaymentType.Cash && openCashRegister != null)
                {
                    await _cashRegisterService.RegisterMovementAsync(
                        cashRegisterId: openCashRegister.Id,
                        type: CashMovementType.Expense,
                        reason: MovementReason.SupplierPayment,
                        paymentMethod: purchase.PaymentMethod ?? PaymentMethod.Cash,
                        amount: purchase.Total,
                        description: $"Compra {purchase.PurchaseNumber}",
                        saleId: null,
                        userId: userId,
                        purchaseId: purchase.Id);
                }

                purchase.Status = PurchaseStatus.Confirmed;
                purchase.ConfirmedBy = userId;
                purchase.ConfirmedAt = DateTime.UtcNow;

                await _purchaseRepository.UpdateAsync(purchase);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Purchase {PurchaseNumber} confirmed by user {UserId}.",
                    purchase.PurchaseNumber, userId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Purchase {PurchaseId} confirmation failed. Rolling back.", purchaseId);
                throw new InvalidOperationException(
                    "No se pudo confirmar la compra. Se revirtió la operación (stock no actualizado).", ex);
            }
        }

        public async Task CancelAsync(Guid purchaseId, Guid userId, string? reason = null)
        {
            var purchase = await _purchaseRepository.GetByIdAsync(purchaseId);
            if (purchase == null)
                throw new InvalidOperationException("La compra no existe.");

            // Solo se cancela una compra Confirmed. Draft se elimina con DeleteAsync;
            // una ya Cancelled no se vuelve a cancelar.
            if (purchase.Status == PurchaseStatus.Draft)
                throw new InvalidOperationException("Una compra en borrador no se cancela; debe eliminarse.");
            if (purchase.Status == PurchaseStatus.Cancelled)
                throw new InvalidOperationException("La compra ya está cancelada.");

            // Transacción: revertir stock (salida) y, si fue al contado, el movimiento de
            // caja (ingreso inverso al egreso original), y marcar la compra como Cancelada.
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1) Reversión de stock: salida compensatoria por cada item (revierte la entrada).
                foreach (var item in purchase.Items)
                {
                    await _stockMovementService.CreateAsync(new StockMovement
                    {
                        TenantId = purchase.TenantId,
                        ProductId = item.ProductId,
                        Type = StockMovementType.Salida,
                        Quantity = item.Quantity,
                        Reason = $"Reversión de compra {purchase.PurchaseNumber}",
                    });
                }

                // 2) Reversión de caja: solo si la compra fue al contado (generó un Expense al
                // confirmar). Se registra un Income inverso por el mismo monto, enlazando el
                // mismo PurchaseId (el índice único ahora incluye Type, así que Expense e Income
                // coexisten). Las compras a crédito no tienen movimiento de caja que revertir.
                if (purchase.PaymentType == PaymentType.Cash)
                {
                    var openCashRegister = await _cashRegisterService.GetOpenCashRegisterForUserAsync(
                        purchase.TenantId, userId);
                    if (openCashRegister == null)
                        throw new InvalidOperationException(
                            "Debe abrir una caja antes de cancelar una compra al contado.");

                    await _cashRegisterService.RegisterMovementAsync(
                        cashRegisterId: openCashRegister.Id,
                        type: CashMovementType.Income,
                        reason: MovementReason.SupplierPayment,
                        paymentMethod: purchase.PaymentMethod ?? PaymentMethod.Cash,
                        amount: purchase.Total,
                        description: $"Reversión de compra {purchase.PurchaseNumber}",
                        purchaseId: purchase.Id,
                        userId: userId);
                }

                // 3) Marcar como cancelada con auditoría.
                purchase.Status = PurchaseStatus.Cancelled;
                purchase.CancelledBy = userId;
                purchase.CancelledAt = DateTime.UtcNow;
                purchase.CancellationReason = reason;

                await _purchaseRepository.UpdateAsync(purchase);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Purchase {PurchaseNumber} cancelled by user {UserId}. Stock and cash reverted.",
                    purchase.PurchaseNumber, userId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                // Propaga el mensaje real de la causa (p. ej. fallo de reversión de caja) en
                // lugar de un genérico, para que el frontend y los logs muestren la razón exacta.
                var inner = ex is InvalidOperationException ? ex : ex.InnerException;
                _logger.LogError(ex, "Purchase {PurchaseId} cancellation failed. Rolling back. Cause: {Cause}",
                    purchaseId, inner?.Message);
                throw new InvalidOperationException(
                    inner?.Message ?? "No se pudo cancelar la compra. Se revirtió la operación (stock y caja no modificados).",
                    ex);
            }
        }

        public async Task DeleteAsync(Guid purchaseId)
        {
            var purchase = await _purchaseRepository.GetByIdAsync(purchaseId);
            if (purchase == null)
                throw new InvalidOperationException("La compra no existe.");

            if (purchase.Status != PurchaseStatus.Draft)
                throw new InvalidOperationException(
                    "Solo se puede eliminar una compra en estado Borrador (Draft). Las compras confirmadas o canceladas nunca se borran físicamente, por trazabilidad.");

            await _purchaseRepository.DeleteAsync(purchaseId);
        }

        // --- Helpers ---

        private async Task<string> GenerateNextPurchaseNumberAsync(Guid tenantId)
        {
            var last = await _purchaseRepository.GetLastPurchaseNumberAsync(tenantId);
            int next = 1;
            if (!string.IsNullOrEmpty(last) && last.StartsWith(NUMBER_PREFIX))
            {
                var numericPart = last.Substring(NUMBER_PREFIX.Length);
                if (int.TryParse(numericPart, out var lastNumber))
                    next = lastNumber + 1;
            }
            return $"{NUMBER_PREFIX}{next:D6}";
        }

        private static void RecalculateTotals(Purchase purchase)
        {
            decimal subtotal = 0;
            foreach (var item in purchase.Items)
            {
                item.LineSubtotal = CalculateLineSubtotal(item);
                subtotal += item.LineSubtotal;
            }
            purchase.Subtotal = subtotal;
            purchase.Tax = Math.Round(subtotal * TAX_RATE, 2);
            purchase.Total = subtotal + purchase.Tax;
        }

        private static decimal CalculateLineSubtotal(PurchaseItem item)
        {
            return item.Quantity * item.UnitCost * (1 - item.DiscountPercentage / 100m);
        }

        private static void ValidateItems(Purchase purchase)
        {
            if (purchase.Items == null || !purchase.Items.Any())
                throw new InvalidOperationException("La compra debe tener al menos un item.");

            foreach (var item in purchase.Items)
            {
                if (item.Quantity <= 0)
                    throw new InvalidOperationException("La cantidad debe ser mayor a cero en todos los items.");
                if (item.UnitCost <= 0)
                    throw new InvalidOperationException("El costo unitario debe ser mayor a cero en todos los items.");
            }
        }

        private static void ValidatePaymentTerms(Purchase purchase)
        {
            if (purchase.PaymentType == PaymentType.Cash && purchase.PaymentMethod == null)
                throw new InvalidOperationException("Debe indicar el método de pago para compras al contado.");

            if (purchase.PaymentType == PaymentType.Credit &&
                (!purchase.CreditDays.HasValue || purchase.CreditDays.Value <= 0))
                throw new InvalidOperationException("Debe indicar los días de crédito para compras a crédito.");
        }

        private static void CalculateDueDate(Purchase purchase)
        {
            if (purchase.PaymentType == PaymentType.Credit && purchase.CreditDays.HasValue)
            {
                purchase.DueDate = purchase.PurchaseDate.AddDays(purchase.CreditDays.Value);
            }
            else
            {
                purchase.DueDate = null;
            }
        }
    }
}
