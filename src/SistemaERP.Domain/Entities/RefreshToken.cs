using System;

namespace SistemaERP.Domain.Entities;

public class RefreshToken : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    // Navigation
    public User? User { get; set; }
    public RefreshToken? ReplacedByToken { get; set; }

    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
}