using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Application.Reports.Dtos;
using SistemaERP.Application.Reports.Sales;

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

    /// <summary>
    /// Obtiene TODAS las ventas filtradas (sin paginar) para exportación a Excel.
    /// Usa los mismos filtros que GetSalesByPeriodAsync pero ignora Page/PageSize.
    /// </summary>
    Task<IReadOnlyList<SalesReportItemDto>> GetAllSalesByPeriodAsync(ReportFilterDto filter);

    /// <summary>
    /// Genera un archivo Excel (.xlsx) con el reporte de ventas por período.
    /// Columnas: Número, Fecha, Cliente, Vendedor, Tipo de Pago, Estado de Pago, Estado, Subtotal, IGV, Total.
    /// </summary>
    Task<byte[]> GenerateSalesReportExcelAsync(IReadOnlyList<SalesReportItemDto> items);
}
