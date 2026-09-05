using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Application.DTOs;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer> CreateAsync(CreateCustomerDto dto, Guid tenantId);
    Task<Customer> UpdateAsync(UpdateCustomerDto dto);
    Task DeleteAsync(Guid id);
}