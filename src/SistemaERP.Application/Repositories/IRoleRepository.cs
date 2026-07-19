using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Repositories;

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> GetAllByTenantAsync(Guid tenantId);
    Task<bool> ExistsByNameAsync(Guid tenantId, string name);
    Task<Role?> GetByIdAsync(Guid id);
    Task<Role?> GetByIdWithPermissionsAsync(Guid id);
    Task<int> GetUserCountAsync(Guid roleId);
    Task<Role> AddAsync(Role role);
    Task<Role> UpdateAsync(Role role);
    Task DeleteAsync(Role role);
}
