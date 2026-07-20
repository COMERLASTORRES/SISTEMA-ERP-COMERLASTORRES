using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

public interface IUserService
{
    Task<User> RegisterAsync(string tenantName, string email, string password, string fullName);
    Task<User?> ValidateCredentialsAsync(string email, string password);

    // Lista los usuarios de un tenant (ordenados por nombre).
    Task<IReadOnlyList<User>> GetAllByTenantAsync(Guid tenantId);

    // Devuelve un usuario con sus roles cargados.
    Task<User?> GetByIdWithRolesAsync(Guid userId);

    // Reemplaza el set completo de roles del usuario.
    Task AssignRolesAsync(Guid userId, IEnumerable<Guid> roleIds);

    // Crea un usuario adicional dentro de un tenant existente (no crea tenant nuevo).
    Task<User> CreateUserAsync(Guid tenantId, string email, string password, string fullName);

    // Actualiza datos básicos del usuario (nombre, estado activo).
    Task<User> UpdateUserAsync(Guid userId, string fullName, bool isActive);

    // Devuelve la unión de los permisos de todos los roles del usuario (sin duplicados).
    Task<IReadOnlyList<Permission>> GetUserPermissionsAsync(Guid userId);

    // Sincroniza el rol de sistema "Admin" de cada tenant con los permisos nuevos del catálogo.
    Task SyncSystemAdminRolesAsync();
}
