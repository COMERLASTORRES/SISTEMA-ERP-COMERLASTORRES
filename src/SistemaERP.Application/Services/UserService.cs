using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILogger<UserService> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(
            ITenantRepository tenantRepository,
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ILogger<UserService> logger,
            IUnitOfWork unitOfWork)
        {
            _tenantRepository = tenantRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _logger = logger;
            _unitOfWork = unitOfWork;
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

            await _unitOfWork.BeginTransactionAsync();
            try
            {
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
                await EnsureSystemAdminRoleAsync(tenant.Id, user);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Registered admin user {Email} for tenant {TenantId}.", email, tenant.Id);
                return user;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error registering tenant {TenantName}", tenantName);
                throw;
            }
        }

        private async Task EnsureSystemAdminRoleAsync(Guid tenantId, User user)
        {
            var existingAdmin = (await _roleRepository.GetAllByTenantAsync(tenantId))
                .FirstOrDefault(r => r.IsSystemRole && r.Name == "Admin");
            if (existingAdmin != null)
            {
                // Ya existe el rol Admin (ej. otro usuario del mismo tenant): solo asignarlo.
                var adminRoleId = existingAdmin.Id;
                var userRole = new UserRole { UserId = user.Id, RoleId = adminRoleId };
                user.UserRoles.Add(userRole);
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
            var adminUserRole = new UserRole { UserId = user.Id, RoleId = adminRole.Id };
            user.UserRoles.Add(adminUserRole);
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

        // Obtiene el Tenant por su Id (incluye el nombre).
        public async Task<Tenant?> GetTenantAsync(Guid tenantId)
        {
            return await _tenantRepository.GetByIdAsync(tenantId);
        }

        // ==================== Refresh Token Operations ====================

        /// <summary>
        /// Genera un refresh token opaco (aleatorio), guarda su hash en BD y devuelve el token plano.
        /// El token plano SOLO se devuelve una vez (al crear/rotar); el hash se usa para validación.
        /// </summary>
        public async Task<RefreshTokenResult> IssueRefreshTokenAsync(Guid userId, int expiryDays)
        {
            var plainToken = GenerateOpaqueToken();
            var tokenHash = HashToken(plainToken);
            var expiresAt = DateTime.UtcNow.AddDays(expiryDays);

            var refreshToken = new RefreshToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
            };

            await _refreshTokenRepository.AddAsync(refreshToken);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Issued refresh token {TokenId} for user {UserId}.", refreshToken.Id, userId);
            return RefreshTokenResult.Create(plainToken, tokenHash, expiresAt, refreshToken.Id, userId);
        }

        /// <summary>
        /// Rota un refresh token: valida el token actual, lo revoca, y emite uno nuevo.
        /// Implementa "refresh token rotation" — el token anterior se invalida (RevokedAt) y se
        /// referencia desde el nuevo (ReplacedByTokenId), permitiendo detectar reutilización.
        /// </summary>
        public async Task<RefreshTokenResult> RotateRefreshTokenAsync(string refreshToken, int expiryDays)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("RotateRefreshTokenAsync called with empty token.");
                return null!;
            }

            var tokenHash = HashToken(refreshToken);
            var existing = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

            if (existing == null)
            {
                _logger.LogWarning("Refresh token not found in database.");
                return null!;
            }

            if (!existing.IsActive)
            {
                // Token ya revocado o expirado — posible intento de reutilización (token theft detection)
                if (existing.RevokedAt != null && existing.ReplacedByTokenId != null)
                {
                    // El token fue rotado legítimamente antes; el cliente tiene un token viejo.
                    // Esto es normal si el cliente no guardó el nuevo token.
                    _logger.LogInformation("Refresh token {TokenId} already rotated (replaced by {ReplacedBy}).", existing.Id, existing.ReplacedByTokenId);
                }
                else
                {
                    // Token revocado sin rotación = logout o revocación manual
                    _logger.LogWarning("Refresh token {TokenId} is revoked/expired (possible reuse attack).", existing.Id);
                }
                return null!;
            }

            // Revocar el token actual
            existing.RevokedAt = DateTime.UtcNow;

            // Emitir nuevo token
            var plainNewToken = GenerateOpaqueToken();
            var newTokenHash = HashToken(plainNewToken);
            var newExpiresAt = DateTime.UtcNow.AddDays(expiryDays);

            var newToken = new RefreshToken
            {
                UserId = existing.UserId,
                TokenHash = newTokenHash,
                ExpiresAt = newExpiresAt,
                ReplacedByTokenId = existing.Id, // Enlaza el nuevo al anterior para auditoría
            };

            await _refreshTokenRepository.AddAsync(newToken);
            await _refreshTokenRepository.UpdateAsync(existing);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Rotated refresh token {OldTokenId} -> {NewTokenId} for user {UserId}.", existing.Id, newToken.Id, existing.UserId);
            return RefreshTokenResult.Create(plainNewToken, newTokenHash, newExpiresAt, newToken.Id, existing.UserId);
        }

        /// <summary>
        /// Revoca un refresh token específico (logout). Marca RevokedAt = ahora.
        /// </summary>
        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return;

            var tokenHash = HashToken(refreshToken);
            var existing = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

            if (existing != null && existing.RevokedAt == null)
            {
                existing.RevokedAt = DateTime.UtcNow;
                await _refreshTokenRepository.UpdateAsync(existing);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Revoked refresh token {TokenId} for user {UserId}.", existing.Id, existing.UserId);
            }
        }

        /// <summary>
        /// Revoca TODOS los refresh tokens activos de un usuario (logout everywhere / cambio de contraseña).
        /// </summary>
        public async Task RevokeAllUserRefreshTokensAsync(Guid userId)
        {
            var tokens = await _refreshTokenRepository.GetByUserIdAsync(userId);
            var active = tokens.Where(t => t.IsActive).ToList();

            if (active.Count == 0) return;

            foreach (var token in active)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            // Actualizar en lote
            foreach (var token in active)
            {
                await _refreshTokenRepository.UpdateAsync(token);
            }
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Revoked {Count} refresh tokens for user {UserId}.", active.Count, userId);
        }

        // ==================== Helpers ====================

        private static string GenerateOpaqueToken()
        {
            // 32 bytes = 256 bits de entropía, codificado en base64url (sin padding)
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string HashToken(string token)
        {
            // SHA-256 del token plano. No usamos BCrypt porque el token ya tiene alta entropía
            // y no necesita work factor; SHA-256 es determinístico y rápido para lookup.
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hash);
        }
    }
}
