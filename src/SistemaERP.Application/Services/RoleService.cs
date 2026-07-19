using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly ILogger<RoleService> _logger;

        public RoleService(
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            ILogger<RoleService> logger)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Role>> GetAllByTenantAsync(Guid tenantId)
        {
            return await _roleRepository.GetAllByTenantAsync(tenantId);
        }

        public async Task<Role?> GetByIdAsync(Guid id)
        {
            return await _roleRepository.GetByIdWithPermissionsAsync(id);
        }

        public async Task<Role> CreateAsync(Guid tenantId, string name, string? description, bool isSystemRole = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("El nombre del rol es obligatorio.");

            var role = new Role
            {
                TenantId = tenantId,
                Name = name.Trim(),
                Description = description,
                IsSystemRole = isSystemRole,
            };

            _logger.LogInformation("Creating role {RoleName} for tenant {TenantId}.", role.Name, tenantId);
            return await _roleRepository.AddAsync(role);
        }

        public async Task<Role> UpdateAsync(Guid id, string name, string? description)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null) throw new InvalidOperationException("El rol no existe.");

            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("El nombre del rol es obligatorio.");

            // Los roles de sistema solo permiten editar la descripción, no el nombre.
            if (role.IsSystemRole && !string.Equals(role.Name, name.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("No se puede renombrar un rol de sistema.");

            role.Name = name.Trim();
            role.Description = description;

            _logger.LogInformation("Updating role {RoleId}.", id);
            return await _roleRepository.UpdateAsync(role);
        }

        public async Task DeleteAsync(Guid id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null) throw new InvalidOperationException("El rol no existe.");

            if (role.IsSystemRole)
                throw new InvalidOperationException("No se puede eliminar un rol de sistema.");

            var userCount = await _roleRepository.GetUserCountAsync(id);
            if (userCount > 0)
                throw new InvalidOperationException("No se puede eliminar un rol que tiene usuarios asignados.");

            _logger.LogInformation("Deleting role {RoleId}.", id);
            await _roleRepository.DeleteAsync(role);
        }

        // Reemplaza el set completo de permisos del rol. Carga el rol con sus permisos
        // actuales, elimina los existentes y agrega los nuevos en una sola operación.
        public async Task AssignPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
        {
            var role = await _roleRepository.GetByIdWithPermissionsAsync(roleId);
            if (role == null) throw new InvalidOperationException("El rol no existe.");

            var requestedIds = permissionIds.Distinct().ToList();
            var permissions = await _permissionRepository.GetByIdsAsync(requestedIds);
            var validIds = new HashSet<Guid>(permissions.Select(p => p.Id));

            // Ignora ids inexistentes en lugar de fallar, para ser tolerante a desincronización.
            role.RolePermissions = validIds
                .Select(pid => new RolePermission { RoleId = role.Id, PermissionId = pid })
                .ToList();

            _logger.LogInformation("Assigned {Count} permissions to role {RoleId}.", validIds.Count, roleId);
            await _roleRepository.UpdateAsync(role);
        }
    }
}
