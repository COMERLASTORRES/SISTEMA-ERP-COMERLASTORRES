namespace SistemaERP.Domain.Entities;

/// <summary>
/// RolePermission es la tabla intermedia de la relación muchos-a-muchos entre Role y
/// Permission. Llave primaria compuesta (RoleId, PermissionId) con cascade delete para
/// eliminar automáticamente las asignaciones al borrar el rol o el permiso.
/// </summary>
public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
