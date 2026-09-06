using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Reports.StockMovements;

/// <summary>
/// Fila de un reporte de movimientos de stock. Proyección plana de la entidad StockMovement
/// con el nombre del producto resuelto en la consulta. NUNCA se expone la entidad de
/// dominio al cliente.
/// </summary>
public class StockMovementReportItemDto
{
    public DateTime CreatedAt { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
}