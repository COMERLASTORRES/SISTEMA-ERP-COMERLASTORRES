using System;
namespace SistemaERP.Domain.Entities;
public class Customer : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public CustomerType CustomerType { get; set; }
    public decimal CreditLimit { get; set; }
    public bool IsActive { get; set; } = true;
}
public enum DocumentType
{
    DNI,
    RUC,
    CE,
    SinDocumento
}
public enum CustomerType
{
    Regular,
    Mayorista,
    Corporativo
}
