using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaERP.Application.Reports.Dtos;
using SistemaERP.Application.Reports.Sales;
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

    public ReportsController(ISalesReportService salesReportService)
    {
        _salesReportService = salesReportService;
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
}
