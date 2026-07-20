using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Reports.Sales;

/// <summary>
/// Fila de un reporte de ventas por período. Proyección plana de la entidad Sale:
/// NUNCA se expone la entidad de dominio al cliente. Los nombres (cliente/vendedor)
/// vienen resueltos desde sus entidades relacionadas en la consulta.
/// </summary>
public class SalesReportItemDto
{
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public SaleStatus Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
}
