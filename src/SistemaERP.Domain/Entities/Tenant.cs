namespace SistemaERP.Domain.Entities;

public class Tenant : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? RUC { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Si es true, las salidas de stock (ventas) pueden dejar el stock en negativo.
    /// Si es false (por defecto), el sistema valida que haya stock suficiente antes de confirmar una venta.
    /// </summary>
    public bool AllowNegativeStock { get; set; } = false;
}
