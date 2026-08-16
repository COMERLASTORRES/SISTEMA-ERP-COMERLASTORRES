using System;

namespace SistemaERP.Domain.Entities;

public class PasswordResetToken : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    // Navigation
    public User? User { get; set; }

    public bool IsValid => UsedAt == null && ExpiresAt > DateTime.UtcNow;
}