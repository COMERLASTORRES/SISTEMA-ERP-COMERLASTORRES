namespace SistemaERP.Application.Reports.Dtos;

/// <summary>
/// Resumen de métricas agregadas para un reporte. Estructura única reutilizada por todos
/// los reportes para evitar definir un objeto de totales distinto en cada servicio.
/// Los campos son opcionales porque no todo reporte los utiliza (ej. Inventario no tiene IGV).
/// </summary>
public class ReportSummaryDto
{
    /// <summary>Suma de los montos totales (con IGV).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Suma del IGV (impuesto).</summary>
    public decimal TotalTax { get; set; }

    /// <summary>Suma de los subtotales (sin IGV).</summary>
    public decimal TotalSubtotal { get; set; }

    /// <summary>Cantidad de documentos/registros en el conjunto filtrado.</summary>
    public int TotalRecords { get; set; }
}
