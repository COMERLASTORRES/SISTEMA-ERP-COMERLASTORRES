using System;

namespace SistemaERP.Domain.Entities;

public class PurchaseItem : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PurchaseId { get; set; }
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal DiscountPercentage { get; set; } = 0;

    // Calculado: (Quantity * UnitCost) * (1 - DiscountPercentage / 100).
    public decimal LineSubtotal { get; set; }
}
