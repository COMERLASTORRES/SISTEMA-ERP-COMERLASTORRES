using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaERP.Application.DTOs;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Application.Services;

namespace SistemaERP.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository, ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
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

    public async Task<Product> CreateAsync(CreateProductDto dto, Guid tenantId)
    {
        // Business validation: ensure product code is unique per tenant
        var existing = await _productRepository.GetAllAsync();
        foreach (var p in existing)
        {
            if (p.TenantId == tenantId && string.Equals(p.Code, dto.Code, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Attempt to create product with duplicate code {Code} for tenant {TenantId}.", dto.Code, tenantId);
                throw new InvalidOperationException($"Product code '{dto.Code}' already exists for this tenant.");
            }
        }

        // Validate CategoryId belongs to same tenant (if provided)
        if (dto.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId.Value);
            if (category == null || category.TenantId != tenantId)
            {
                throw new InvalidOperationException("Category not found or does not belong to this tenant.");
            }
        }

        var product = new Product
        {
            TenantId = tenantId,
            Code = dto.Code,
            Name = dto.Name,
            Barcode = dto.Barcode,
            PurchasePrice = dto.PurchasePrice,
            SalePrice = dto.SalePrice,
            Stock = dto.Stock,
            StockMinimum = dto.StockMinimum,
            CategoryId = dto.CategoryId,
            IsActive = dto.IsActive
        };

        _logger.LogInformation("Creating product {ProductCode} for tenant {TenantId}.", dto.Code, tenantId);
        return await _productRepository.AddAsync(product);
    }

    public async Task<Product> UpdateAsync(UpdateProductDto dto)
    {
        // Get existing product (tenant check done by controller via GetByIdAsync)
        var existingProduct = await _productRepository.GetByIdAsync(dto.Id);
        if (existingProduct == null)
        {
            throw new InvalidOperationException("Product not found.");
        }

        // Business validation: ensure product code is unique per tenant (excluding itself)
        var existing = await _productRepository.GetAllAsync();
        foreach (var p in existing)
        {
            if (p.Id != dto.Id && p.TenantId == existingProduct.TenantId && string.Equals(p.Code, dto.Code, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Attempt to update product {ProductId} to duplicate code {Code} for tenant {TenantId}.", dto.Id, dto.Code, existingProduct.TenantId);
                throw new InvalidOperationException($"Product code '{dto.Code}' already exists for this tenant.");
            }
        }

        // Validate CategoryId belongs to same tenant (if provided)
        if (dto.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId.Value);
            if (category == null || category.TenantId != existingProduct.TenantId)
            {
                throw new InvalidOperationException("Category not found or does not belong to this tenant.");
            }
        }

        try
        {
            // Map fields
            existingProduct.Code = dto.Code;
            existingProduct.Name = dto.Name;
            existingProduct.Barcode = dto.Barcode;
            existingProduct.PurchasePrice = dto.PurchasePrice;
            existingProduct.SalePrice = dto.SalePrice;
            existingProduct.Stock = dto.Stock;
            existingProduct.StockMinimum = dto.StockMinimum;
            existingProduct.CategoryId = dto.CategoryId;
            existingProduct.IsActive = dto.IsActive;

            _logger.LogInformation("Updating product {ProductId}.", dto.Id);
            return await _productRepository.UpdateAsync(existingProduct);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Product {ProductId} was modified by another process.", dto.Id);
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