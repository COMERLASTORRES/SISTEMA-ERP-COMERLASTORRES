namespace SistemaERP.Domain.Entities;

/// <summary>
/// UserRole es la tabla intermedia de la relación muchos-a-muchos entre User y Role,
/// permitiendo que un usuario tenga múltiples roles. Llave primaria compuesta
/// (UserId, RoleId) con cascade delete al borrar el usuario o el rol.
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
