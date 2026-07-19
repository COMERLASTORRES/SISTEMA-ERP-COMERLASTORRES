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
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PurchaseService> _logger;

        public PurchaseService(
            IPurchaseRepository purchaseRepository,
            IStockMovementService stockMovementService,
            IUnitOfWork unitOfWork,
            ILogger<PurchaseService> logger)
        {
            _purchaseRepository = purchaseRepository;
            _stockMovementService = stockMovementService;
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

            purchase.PurchaseNumber = await GenerateNextPurchaseNumberAsync(purchase.TenantId);
            RecalculateTotals(purchase);
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

            // Mantener el número y la fecha de creación originales.
            existing.SupplierId = purchase.SupplierId;
            existing.WarehouseId = purchase.WarehouseId;
            existing.VoucherType = purchase.VoucherType;
            existing.VoucherNumber = purchase.VoucherNumber;
            existing.PurchaseDate = purchase.PurchaseDate;
            existing.Currency = purchase.Currency;
            existing.ExchangeRate = purchase.ExchangeRate;
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

            // Transacción: registrar entradas de stock y confirmar la compra de forma atómica.
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

        public async Task CancelAsync(Guid purchaseId)
        {
            var purchase = await _purchaseRepository.GetByIdAsync(purchaseId);
            if (purchase == null)
                throw new InvalidOperationException("La compra no existe.");

            if (purchase.Status == PurchaseStatus.Cancelled)
                throw new InvalidOperationException("La compra ya está cancelada.");

            // NOTA (simplificación consciente): si la compra estaba Confirmed, cancelar
            // NO revierte el stock automáticamente por ahora. No se generan movimientos
            // de reversión. Esto se podría mejorar en el futuro con movimientos de salida
            // compensatorios, pero se deja fuera de alcance deliberadamente.
            purchase.Status = PurchaseStatus.Cancelled;

            _logger.LogInformation("Purchase {PurchaseId} cancelled (status was {Status}).",
                purchaseId, purchase.Status);
            await _purchaseRepository.UpdateAsync(purchase);
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
    }
}
