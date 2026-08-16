using System;
using System.Collections.Generic;

namespace SistemaERP.Domain.Entities;

public class User : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    [Obsolete("Solo por compatibilidad. La autorización utiliza UserRole + Role + Permission.")]
    public string Role { get; set; } = "Employee";

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Roles asignados al usuario (muchos-a-muchos vía UserRole). Sustituye progresivamente
    /// al campo Role para una autorización granular basada en permisos.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Refresh tokens emitidos para este usuario.
    /// </summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
