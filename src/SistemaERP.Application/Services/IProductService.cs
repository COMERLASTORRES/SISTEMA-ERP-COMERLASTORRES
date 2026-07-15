using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product> CreateAsync(Product product);
    Task<Product> UpdateAsync(Product product);
    Task DeleteAsync(Guid id);
}