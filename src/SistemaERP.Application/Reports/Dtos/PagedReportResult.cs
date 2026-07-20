namespace SistemaERP.Application.Reports.Dtos;

/// <summary>
/// Resultado paginado genérico para cualquier reporte. Evita duplicar la envoltura
/// de paginación en cada servicio de reporte.
/// </summary>
/// <typeparam name="T">Tipo de elemento del reporte (ej. SalesReportItemDto).</typeparam>
public class PagedReportResult<T>
{
    /// <summary>Elementos de la página actual.</summary>
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>Total de registros que cumplen el filtro (sin paginar).</summary>
    public int TotalCount { get; set; }

    /// <summary>Página devuelta.</summary>
    public int Page { get; set; }

    /// <summary>Tamaño de página aplicado.</summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Resumen opcional (totales). Se calcula sobre el conjunto filtrado completo, no solo
    /// sobre la página, para que las métricas agregadas sean coherentes con TotalCount.
    /// </summary>
    public ReportSummaryDto? Summary { get; set; }
}
