namespace SistemaERP.Domain.Entities
{
    public enum StockMovementType
    {
        Entrada,
        Salida,
        Ajuste
    }

    public class StockMovement : AuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid ProductId { get; set; }
        public StockMovementType Type { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int PreviousStock { get; set; }
        public int NewStock { get; set; }
        public string? CreatedBy { get; set; }
    }
}
