namespace SistemaERP.Domain.Entities;

public class Tenant : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? RUC { get; set; }
    public bool IsActive { get; set; } = true;
}
