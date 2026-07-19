using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

public interface IUserService
{
    Task<User> RegisterAsync(string tenantName, string email, string password, string fullName);
    Task<User?> ValidateCredentialsAsync(string email, string password);

    // Reemplaza el set completo de roles del usuario.
    Task AssignRolesAsync(Guid userId, IEnumerable<Guid> roleIds);

    // Devuelve la unión de los permisos de todos los roles del usuario (sin duplicados).
    Task<IReadOnlyList<Permission>> GetUserPermissionsAsync(Guid userId);
}
