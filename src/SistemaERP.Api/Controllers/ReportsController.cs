using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaERP.Application.Reports.Dtos;
using SistemaERP.Application.Reports.Sales;
using SistemaERP.Application.Reports.StockMovements;
using SistemaERP.Domain.Entities;
using SistemaERP.Domain;

namespace SistemaERP.Api.Controllers;

/// <summary>
/// Fachada de reportes. Por ahora expone los reportes de Ventas; en el futuro agregará
/// Compras, Inventario, Caja, Cuentas por Cobrar/Pagar y Dashboard, cada uno delegando en
/// su servicio especializado de dominio (ISalesReportService, IPurchaseReportService, ...).
/// </summary>
[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ISalesReportService _salesReportService;
    private readonly IStockMovementReportService _stockMovementReportService;

    public ReportsController(ISalesReportService salesReportService,
        IStockMovementReportService stockMovementReportService)
    {
        _salesReportService = salesReportService;
        _stockMovementReportService = stockMovementReportService;
    }

    /// <summary>
    /// Reporte de ventas por período. Los filtros se pasan como query parameters:
    /// dateFrom, dateTo, customerId, supplierId, vendorId, warehouseId, paymentType,
    /// paymentStatus, documentStatus, search, page, pageSize, sortBy, sortDirection.
    /// </summary>
    [HttpGet("sales/by-period")]
    [Authorize(Policy = PermissionCodes.ReportsView)]
    public async Task<IActionResult> GetSalesByPeriod([FromQuery] ReportFilterDto filter)
    {
        var result = await _salesReportService.GetSalesByPeriodAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Exporta el reporte de ventas por período a Excel (.xlsx). Usa los mismos filtros
    /// que el reporte visual, pero sin paginación.
    /// </summary>
    [HttpGet("sales/by-period/export/excel")]
    [Authorize(Policy = PermissionCodes.ReportsExport)]
    public async Task<IActionResult> ExportSalesByPeriodExcel([FromQuery] ReportFilterDto filter)
    {
        var items = await _salesReportService.GetAllSalesByPeriodAsync(filter);
        var bytes = await _salesReportService.GenerateSalesReportExcelAsync(items);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ventas-por-periodo.xlsx");
    }

    /// <summary>
    /// Exporta movimientos de stock a Excel (.xlsx). Filtros soportados:
    /// dateFrom, dateTo, productId (nullable - sin filtro), type (nullable).
    /// </summary>
    [HttpGet("stock-movements/export/excel")]
    [Authorize(Policy = PermissionCodes.ReportsExport)]
    public async Task<IActionResult> ExportStockMovementsExcel(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] Guid? productId,
        [FromQuery] StockMovementType? type)
    {
        var items = await _stockMovementReportService.GetAllMovementsAsync(dateFrom, dateTo, productId, type);
        var bytes = await _stockMovementReportService.GenerateStockMovementsExcelAsync(items);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "movimientos-stock.xlsx");
    }
}
