using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SistemaERP.Application.Repositories;
using SistemaERP.Application.Services;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services
{
    public class UserService : IUserService
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly ILogger<UserService> _logger;

        public UserService(
            ITenantRepository tenantRepository,
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            ILogger<UserService> logger)
        {
            _tenantRepository = tenantRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _logger = logger;
        }

        public async Task<User> RegisterAsync(string tenantName, string email, string password, string fullName)
        {
            if (string.IsNullOrWhiteSpace(tenantName))
            {
                _logger.LogWarning("Attempt to register with empty tenant name.");
                throw new InvalidOperationException("Tenant name is required.");
            }

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Attempt to register with empty email or password.");
                throw new InvalidOperationException("Email and password are required.");
            }

            var existing = await _userRepository.GetByEmailAsync(email);
            if (existing != null)
            {
                _logger.LogWarning("Attempt to register with already-used email {Email}.", email);
                throw new InvalidOperationException("A user with this email already exists.");
            }

            var tenant = await _tenantRepository.AddAsync(new Tenant { Name = tenantName });

            var user = new User
            {
                TenantId = tenant.Id,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = fullName,
                Role = "Admin",
                IsActive = true
            };

            user = await _userRepository.AddAsync(user);

            // Todo tenant nuevo arranca con un rol Admin de sistema que tiene TODOS los
            // permisos del catálogo y se asigna a su primer usuario, garantizando un admin
            // funcional desde el inicio. Se crea dentro de RegisterAsync para que el rol
            // quede disponible aunque el seed de permisos aún no haya corrido.
            await EnsureSystemAdminRoleAsync(tenant.Id, user.Id);

            _logger.LogInformation("Registered admin user {Email} for tenant {TenantId}.", email, tenant.Id);
            return user;
        }

        private async Task EnsureSystemAdminRoleAsync(Guid tenantId, Guid userId)
        {
            var existingAdmin = (await _roleRepository.GetAllByTenantAsync(tenantId))
                .FirstOrDefault(r => r.IsSystemRole && r.Name == "Admin");
            if (existingAdmin != null)
            {
                // Ya existe el rol Admin (ej. otro usuario del mismo tenant): solo asignarlo.
                await AssignRolesAsync(userId, new[] { existingAdmin.Id });
                return;
            }

            var allPermissions = await _permissionRepository.GetAllAsync();
            var adminRole = new Role
            {
                TenantId = tenantId,
                Name = "Admin",
                Description = "Administrador del sistema (acceso total).",
                IsSystemRole = true,
                RolePermissions = allPermissions
                    .Select(p => new RolePermission { PermissionId = p.Id })
                    .ToList(),
            };

            adminRole = await _roleRepository.AddAsync(adminRole);
            await AssignRolesAsync(userId, new[] { adminRole.Id });
        }

        /// <summary>
        /// Sincroniza el rol de sistema de cada tenant (identificado por IsSystemRole == true,
        /// no por su nombre, que puede variar entre tenants): le agrega los códigos de
        /// permiso recién incorporados al catálogo (ej. tras añadir users.* o roles.*) sin
        /// eliminar los que ya tuviera. Así un admin nunca pierde acceso por un nuevo permiso.
        /// Es idempotente y seguro de ejecutar en cada arranque.
        /// </summary>
        public async Task SyncSystemAdminRolesAsync()
        {
            var allPermissionIds = (await _permissionRepository.GetAllAsync())
                .Select(p => p.Id)
                .ToHashSet();
            if (allPermissionIds.Count == 0) return;

            var tenants = await _tenantRepository.GetAllAsync();
            foreach (var tenant in tenants)
            {
                var admin = (await _roleRepository.GetAllByTenantAsync(tenant.Id))
                    .FirstOrDefault(r => r.IsSystemRole);
                if (admin == null) continue;

                var adminWithPerms = await _roleRepository.GetByIdWithPermissionsAsync(admin.Id);
                if (adminWithPerms == null) continue;

                var existing = new HashSet<Guid>(
                    adminWithPerms.RolePermissions.Select(rp => rp.PermissionId));
                var missing = allPermissionIds
                    .Where(pid => !existing.Contains(pid))
                    .Select(pid => new RolePermission { RoleId = admin.Id, PermissionId = pid })
                    .ToList();

                if (missing.Count == 0) continue;

                adminWithPerms.RolePermissions =
                    adminWithPerms.RolePermissions.Concat(missing).ToList();
                await _roleRepository.UpdateAsync(adminWithPerms);
            }
        }

        public async Task<User?> ValidateCredentialsAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Login failed for email {Email}: user not found or inactive.", email);
                return null;
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed for email {Email}: invalid password.", email);
                return null;
            }

            return user;
        }

        public Task<IReadOnlyList<User>> GetAllByTenantAsync(Guid tenantId)
        {
            return _userRepository.GetAllByTenantAsync(tenantId);
        }

        public Task<User?> GetByIdWithRolesAsync(Guid userId)
        {
            return _userRepository.GetByIdWithRolesAsync(userId);
        }

        // Reemplaza el set completo de roles del usuario.
        public async Task AssignRolesAsync(Guid userId, IEnumerable<Guid> roleIds)
        {
            var user = await _userRepository.GetByIdWithRolesAsync(userId);
            if (user == null) throw new InvalidOperationException("El usuario no existe.");

            var requestedIds = roleIds.Distinct().ToList();
            var roles = (await _roleRepository.GetAllByTenantAsync(user.TenantId))
                .Where(r => requestedIds.Contains(r.Id))
                .ToList();
            var validIds = new HashSet<Guid>(roles.Select(r => r.Id));

            // Tolerante a ids inexistentes/duplicados: asigna solo los roles válidos del tenant.
            user.UserRoles = validIds
                .Select(rid => new UserRole { UserId = user.Id, RoleId = rid })
                .ToList();

            _logger.LogInformation("Assigned {Count} roles to user {UserId}.", validIds.Count, userId);
            await _userRepository.UpdateAsync(user);
        }

        // Unión de los permisos de todos los roles del usuario (sin duplicados).
        public async Task<IReadOnlyList<Permission>> GetUserPermissionsAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdWithRolesAsync(userId);
            if (user == null) return Array.Empty<Permission>();

            return user.UserRoles
                .Select(ur => ur.Role)
                .SelectMany(r => r.RolePermissions)
                .Select(rp => rp.Permission)
                .Where(p => p != null)
                .GroupBy(p => p!.Id)
                .Select(g => g.First()!)
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Code)
                .ToList();
        }

        // Crea un usuario adicional dentro de un tenant existente (no crea tenant nuevo).
        public async Task<User> CreateUserAsync(Guid tenantId, string email, string password, string fullName)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Email y contraseña son requeridos.");

            var existing = await _userRepository.GetByEmailAsync(email);
            if (existing != null)
                throw new InvalidOperationException("Ya existe un usuario con este email.");

            var user = new User
            {
                TenantId = tenantId,
                Email = email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = fullName.Trim(),
                Role = "Employee",
                IsActive = true,
            };

            _logger.LogInformation("Creating user {Email} for tenant {TenantId}.", user.Email, tenantId);
            return await _userRepository.AddAsync(user);
        }

        // Actualiza datos básicos del usuario (nombre y estado activo).
        public async Task<User> UpdateUserAsync(Guid userId, string fullName, bool isActive)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new InvalidOperationException("El usuario no existe.");

            user.FullName = fullName.Trim();
            user.IsActive = isActive;

            _logger.LogInformation("Updating user {UserId}.", userId);
            return await _userRepository.UpdateAsync(user);
        }
    }
}
