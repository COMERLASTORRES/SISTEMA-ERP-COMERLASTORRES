using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Application.Services;

namespace SistemaERP.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository productRepository, ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all products.");
        return await _productRepository.GetAllAsync();
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching product with Id {ProductId}.", id);
        return await _productRepository.GetByIdAsync(id);
    }

    public async Task<Product> CreateAsync(Product product)
    {
        // Business validation: ensure product code is unique per tenant
        var existing = await _productRepository.GetAllAsync();
        foreach (var p in existing)
        {
            if (p.TenantId == product.TenantId && string.Equals(p.Code, product.Code, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Attempt to create product with duplicate code {Code} for tenant {TenantId}.", product.Code, product.TenantId);
                throw new InvalidOperationException($"Product code '{product.Code}' already exists for this tenant.");
            }
        }

        _logger.LogInformation("Creating product {ProductCode} for tenant {TenantId}.", product.Code, product.TenantId);
        return await _productRepository.AddAsync(product);
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        // Business validation: ensure product code is unique per tenant (excluding itself)
        var existing = await _productRepository.GetAllAsync();
        foreach (var p in existing)
        {
            if (p.Id != product.Id && p.TenantId == product.TenantId && string.Equals(p.Code, product.Code, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Attempt to update product {ProductId} to duplicate code {Code} for tenant {TenantId}.", product.Id, product.Code, product.TenantId);
                throw new InvalidOperationException($"Product code '{product.Code}' already exists for this tenant.");
            }
        }

        try
        {
            _logger.LogInformation("Updating product {ProductId}.", product.Id);
            return await _productRepository.UpdateAsync(product);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Product {ProductId} was modified by another process.", product.Id);
            throw new InvalidOperationException("The product was modified by another process. Please refresh and try again.");
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting product {ProductId}.", id);
            await _productRepository.DeleteAsync(id);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Product {ProductId} was modified by another process and could not be deleted.", id);
            throw new InvalidOperationException("The product was modified by another process and could not be deleted.");
        }
    }
}