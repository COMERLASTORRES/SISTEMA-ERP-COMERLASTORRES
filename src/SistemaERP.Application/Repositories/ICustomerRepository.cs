using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Repositories;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer> AddAsync(Customer customer);
    Task<Customer> UpdateAsync(Customer customer);
    Task DeleteAsync(Guid id);
}
