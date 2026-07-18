using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Repositories;

public interface ITenantRepository
{
    Task<IReadOnlyList<Tenant>> GetAllAsync();
    Task<Tenant?> GetByIdAsync(Guid id);
    Task<Tenant> AddAsync(Tenant tenant);
}
