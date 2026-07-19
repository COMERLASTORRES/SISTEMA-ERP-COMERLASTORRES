using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

public interface IRoleService
{
    Task<IReadOnlyList<Role>> GetAllByTenantAsync(Guid tenantId);
    Task<Role?> GetByIdAsync(Guid id);
    Task<Role> CreateAsync(Guid tenantId, string name, string? description, bool isSystemRole = false);
    Task<Role> UpdateAsync(Guid id, string name, string? description);
    Task DeleteAsync(Guid id);
    Task AssignPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);
}
