using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Application.DTOs;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product> CreateAsync(CreateProductDto dto, Guid tenantId);
    Task<Product> UpdateAsync(UpdateProductDto dto);
    Task DeleteAsync(Guid id);
}