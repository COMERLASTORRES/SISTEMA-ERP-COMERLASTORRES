namespace SistemaERP.Domain.Entities;

/// <summary>
/// Permission es la fuente única de verdad de todos los permisos disponibles en el
/// sistema. Su código sigue el formato "modulo.accion" (ej. sales.view, sales.create).
///
/// Los permisos son GLOBALES para toda la plataforma (no están asociados a un Tenant),
/// de modo que cualquier empresa comparte el mismo catálogo. Agregar un permiso nuevo
/// solo requiere insertar un registro (seed o migración de datos) — sin modificar el
/// código de autorización. El código se referencia desde el código mediante constantes
/// centralizadas (PermissionCodes, en etapa posterior), nunca como literales dispersos.
/// </summary>
public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Código único en formato "modulo.accion".</summary>
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
