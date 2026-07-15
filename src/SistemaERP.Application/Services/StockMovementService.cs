using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Application.Services;

namespace SistemaERP.Application.Services
{
    public class StockMovementService : IStockMovementService
    {
        private readonly IStockMovementRepository _stockMovementRepository;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<StockMovementService> _logger;

        public StockMovementService(
            IStockMovementRepository stockMovementRepository,
            IProductRepository productRepository,
            ILogger<StockMovementService> logger)
        {
            _stockMovementRepository = stockMovementRepository;
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<StockMovement>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all stock movements.");
            return await _stockMovementRepository.GetAllAsync();
        }

        public async Task<IReadOnlyList<StockMovement>> GetByProductIdAsync(Guid productId)
        {
            _logger.LogInformation("Fetching stock movements for product {ProductId}.", productId);
            return await _stockMovementRepository.GetByProductIdAsync(productId);
        }

        public async Task<StockMovement?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Fetching stock movement with Id {StockMovementId}.", id);
            return await _stockMovementRepository.GetByIdAsync(id);
        }

        public async Task<StockMovement> CreateAsync(StockMovement stockMovement)
        {
            // Fetch the product (multi-tenant query filter applies)
            var product = await _productRepository.GetByIdAsync(stockMovement.ProductId);
            if (product == null)
            {
                _logger.LogWarning("Attempt to create stock movement for nonexistent product {ProductId}.", stockMovement.ProductId);
                throw new InvalidOperationException($"Product '{stockMovement.ProductId}' was not found.");
            }

            if (product.TenantId != stockMovement.TenantId)
            {
                _logger.LogWarning("Stock movement tenant {MovementTenant} does not match product tenant {ProductTenant}.", stockMovement.TenantId, product.TenantId);
                throw new InvalidOperationException("The product does not belong to the current tenant.");
            }

            if (stockMovement.Quantity <= 0)
            {
                _logger.LogWarning("Attempt to create stock movement with non-positive quantity {Quantity}.", stockMovement.Quantity);
                throw new InvalidOperationException("Movement quantity must be greater than zero.");
            }

            var previousStock = product.Stock;
            int newStock;

            switch (stockMovement.Type)
            {
                case StockMovementType.Entrada:
                    newStock = previousStock + stockMovement.Quantity;
                    break;
                case StockMovementType.Salida:
                    newStock = previousStock - stockMovement.Quantity;
                    break;
                case StockMovementType.Ajuste:
                    newStock = stockMovement.Quantity;
                    break;
                default:
                    _logger.LogWarning("Unsupported stock movement type {Type}.", stockMovement.Type);
                    throw new InvalidOperationException($"Unsupported movement type '{stockMovement.Type}'.");
            }

            if (newStock < 0)
            {
                _logger.LogWarning("Stock movement would result in negative stock for product {ProductId}.", stockMovement.ProductId);
                throw new InvalidOperationException("The operation would result in negative stock.");
            }

            stockMovement.PreviousStock = previousStock;
            stockMovement.NewStock = newStock;

            product.Stock = newStock;

            _logger.LogInformation("Creating {Type} stock movement for product {ProductId}: {Previous} -> {New}.", stockMovement.Type, stockMovement.ProductId, previousStock, newStock);

            try
            {
                await _productRepository.UpdateAsync(product);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Product {ProductId} was modified by another process while recording stock movement.", stockMovement.ProductId);
                throw new InvalidOperationException("The product was modified by another process. Please refresh and try again.");
            }

            return await _stockMovementRepository.AddAsync(stockMovement);
        }
    }
}
