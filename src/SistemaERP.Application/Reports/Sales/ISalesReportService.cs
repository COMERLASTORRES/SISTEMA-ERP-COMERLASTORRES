using System.Threading.Tasks;
using SistemaERP.Application.Reports.Dtos;

namespace SistemaERP.Application.Reports.Sales;

/// <summary>
/// Servicio especializado de reportes de Ventas.
///
/// Arquitectura de reportes (escalable): cada dominio (Ventas, Compras, Inventario, Caja,
/// Cuentas por Cobrar, Cuentas por Pagar, Dashboard, etc.) tendrá su PROPIO servicio
/// especializado (ISalesReportService, IPurchaseReportService, IInventoryReportService, ...).
/// Se evita deliberadamente un único IReportService gigante: los reportes de un ERP crecen
/// mucho y un servicio genérico terminaría acumulando decenas de métodos difíciles de
/// mantener. En el futuro todos estos servicios podrán exponerse tras una fachada o módulo
/// común de Reportes (ReportsController) sin duplicar la lógica de consulta de cada dominio.
/// </summary>
public interface ISalesReportService
{
    /// <summary>
    /// Reporte de ventas por período. Aplica los filtros de ReportFilterDto, pagina en base
    /// de datos y calcula el resumen agregado (totales) sobre el conjunto filtrado completo.
    /// </summary>
    Task<PagedReportResult<SalesReportItemDto>> GetSalesByPeriodAsync(ReportFilterDto filter);
}
