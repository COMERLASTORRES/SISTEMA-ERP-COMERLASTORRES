using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Application.DTOs;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

public interface ISupplierService
{
    Task<IReadOnlyList<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(Guid id);
    Task<Supplier> CreateAsync(CreateSupplierDto dto, Guid tenantId);
    Task<Supplier> UpdateAsync(UpdateSupplierDto dto);
    Task DeleteAsync(Guid id);
}