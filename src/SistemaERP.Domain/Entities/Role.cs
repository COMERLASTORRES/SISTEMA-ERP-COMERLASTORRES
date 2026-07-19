namespace SistemaERP.Domain.Entities;

/// <summary>
/// Role representa un conjunto de permisos asignables a usuarios. Pertenece a un Tenant,
/// de modo que cada empresa puede definir sus propios roles (Admin, Supervisor, Cajero,
/// Compras, Vendedor, etc.). La relación con los permisos es muchos-a-muchos vía
/// RolePermission. El modelo queda preparado para permisos dinámicos, Claims/JWT,
/// políticas de autorización y auditoría de asignación, sin cambios estructurales.
/// </summary>
public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Roles de sistema (ej. Admin) que no deben ser eliminados ni renombrados libremente
    /// por el cliente. Default false: los roles creados por la empresa son personalizados.
    /// </summary>
    public bool IsSystemRole { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
