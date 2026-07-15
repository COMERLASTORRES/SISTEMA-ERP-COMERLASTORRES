using System.ComponentModel.DataAnnotations;

namespace SistemaERP.Domain.Entities;

public class Product : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public int Stock { get; set; }
    public int StockMinimum { get; set; }
    public Guid? CategoryId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
