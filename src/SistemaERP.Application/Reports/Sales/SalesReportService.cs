using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Application.Reports.Dtos;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Reports.Sales;

/// <summary>
/// Implementación del reporte de ventas por período. Todas las consultas son proyecciones
/// (Select) directas a SalesReportItemDto sobre IQueryable, con paginación y conteo en base
/// de datos y AsNoTracking en la raíz del repositorio, para máximo rendimiento.
///
/// El resumen (totales) se calcula sobre el conjunto filtrado completo, no sobre la página,
/// usando una proyección agregada en la base de datos. Así TotalCount, TotalAmount, etc.
/// son coherentes entre sí sin cargar entidades completas en memoria.
/// </summary>
public class SalesReportService : ISalesReportService
{
    private readonly ISalesReportRepository _repository;

    public SalesReportService(ISalesReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedReportResult<SalesReportItemDto>> GetSalesByPeriodAsync(ReportFilterDto filter)
    {
        filter ??= new ReportFilterDto();

        // Tabla: prioridad IncludeAllStatuses > DocumentStatus explícito > default Confirmadas.
        var tableQuery = BuildTableQuery(filter);

        // Resumen: SIEMPRE solo sobre ventas Confirmed, independientemente del filtro de
        // la tabla. Reusa los filtros compartidos y suma únicamente las confirmadas.
        var sharedQuery = BuildSharedQuery(filter);
        var confirmedQuery = sharedQuery.Where(s => s.Status == SaleStatus.Confirmed);

        // Proyección con LEFT JOIN a Customers (CustomerName) y Users (VendorName por
        // CreatedBy, que es una FK conceptual sin navegación). Todo en base de datos.
        var projected = ProjectSales(tableQuery);

        // Resumen agregado sobre las ventas Confirmed (en base de datos), sin importar
        // el filtro de estado de la tabla.
        var summary = await ProjectSales(confirmedQuery)
            .GroupBy(_ => 1)
            .Select(g => new ReportSummaryDto
            {
                TotalAmount = g.Sum(x => x.Total),
                TotalTax = g.Sum(x => x.Tax),
                TotalSubtotal = g.Sum(x => x.Subtotal),
                TotalRecords = g.Count(),
            })
            .FirstOrDefaultAsync() ?? new ReportSummaryDto();

        // Ordenamiento (por defecto: fecha de venta descendente).
        var ordered = ApplySorting(projected, filter);

        // Paginación en base de datos.
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;
        var totalCount = await projected.CountAsync();
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedReportResult<SalesReportItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Summary = summary,
        };
    }

    public async Task<IReadOnlyList<SalesReportItemDto>> GetAllSalesByPeriodAsync(ReportFilterDto filter)
    {
        filter ??= new ReportFilterDto();

        // Misma lógica de filtros y proyección que GetSalesByPeriodAsync, pero sin paginar.
        // Se usa para la exportación a Excel del reporte por período.
        var tableQuery = BuildTableQuery(filter);
        var projected = ProjectSales(tableQuery);
        var ordered = ApplySorting(projected, filter);

        return await ordered.ToListAsync();
    }

    public async Task<byte[]> GenerateSalesReportExcelAsync(IReadOnlyList<SalesReportItemDto> items)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Reporte Ventas");

        // ---- Encabezados ----
        var headers = new[]
        {
            "Número", "Fecha", "Cliente", "Vendedor",
            "Tipo de Pago", "Estado de Pago", "Estado",
            "Subtotal", "IGV", "Total"
        };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // ---- Filas ----
        for (var row = 0; row < items.Count; row++)
        {
            var item = items[row];
            var r = row + 2; // fila 1 = headers

            var paymentTypeLabel = item.PaymentType switch
            {
                PaymentType.Cash => "Contado",
                PaymentType.Credit => "Crédito",
                _ => "—"
            };

            var paymentStatusLabel = item.PaymentStatus switch
            {
                PaymentStatus.Pending => "Pendiente",
                PaymentStatus.Partial => "Parcial",
                PaymentStatus.Paid => "Pagado",
                _ => "—"
            };

            var statusLabel = item.Status switch
            {
                SaleStatus.Draft => "Borrador",
                SaleStatus.Confirmed => "Confirmado",
                SaleStatus.Cancelled => "Cancelado",
                _ => "—"
            };

            worksheet.Cell(r, 1).Value = item.SaleNumber;
            worksheet.Cell(r, 2).Value = item.SaleDate.ToString("dd/MM/yyyy");
            worksheet.Cell(r, 3).Value = item.CustomerName;
            worksheet.Cell(r, 4).Value = item.VendorName;
            worksheet.Cell(r, 5).Value = paymentTypeLabel;
            worksheet.Cell(r, 6).Value = paymentStatusLabel;
            worksheet.Cell(r, 7).Value = statusLabel;
            worksheet.Cell(r, 8).Value = item.Subtotal;
            worksheet.Cell(r, 9).Value = item.Tax;
            worksheet.Cell(r, 10).Value = item.Total;

            // Formato numérico para columnas de dinero
            worksheet.Cell(r, 8).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(r, 9).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(r, 10).Style.NumberFormat.Format = "#,##0.00";
        }

        // Ancho automático de columnas
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return await Task.FromResult(stream.ToArray());
    }

    // ---- helpers ----

    /// <summary>
    /// Aplica los filtros compartidos (fecha, cliente, vendedor, almacén, tipo/pago, búsqueda)
    /// sobre IQueryable&lt;Sale&gt; con operadores traducibles a SQL. La normalización de fechas
    /// (UTC + extensión de DateTo al final del día) se hace aquí para que ambos métodos
    /// (paginado y unpaginado) se comporten igual.
    /// </summary>
    private IQueryable<Sale> BuildSharedQuery(ReportFilterDto filter)
    {
        // Normalización de fechas: el DateTime que llega del query string se bindea como
        // Kind=Unspecified, y PostgreSQL (timestamp with time zone) solo acepta UTC. Lo
        // forzamos a UTC para evitar el error "Cannot write DateTime with Kind=Unspecified".
        // DateTo se extiende al final del día (23:59:59.999) para incluir el día completo,
        // ya que el frontend envía solo la fecha (yyyy-MM-dd) del picker y el usuario espera
        // "hasta ese día inclusive". DateFrom usa 00:00:00 del día.
        var dateFromUtc = filter.DateFrom.HasValue
            ? DateTime.SpecifyKind(filter.DateFrom.Value, DateTimeKind.Utc)
            : (DateTime?)null;
        var dateToUtc = filter.DateTo.HasValue
            ? DateTime.SpecifyKind(
                filter.DateTo.Value.Date.AddDays(1).AddTicks(-1),
                DateTimeKind.Utc)
            : (DateTime?)null;

        var query = _repository.Query();
        if (dateFromUtc.HasValue)
            query = query.Where(s => s.SaleDate >= dateFromUtc.Value);
        if (dateToUtc.HasValue)
            query = query.Where(s => s.SaleDate <= dateToUtc.Value);
        if (filter.CustomerId.HasValue)
            query = query.Where(s => s.CustomerId == filter.CustomerId.Value);
        if (filter.VendorId.HasValue)
            query = query.Where(s => s.CreatedBy == filter.VendorId.Value);
        if (filter.WarehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == filter.WarehouseId.Value);
        if (filter.PaymentType.HasValue)
            query = query.Where(s => s.PaymentType == filter.PaymentType.Value);
        if (filter.PaymentStatus.HasValue)
            query = query.Where(s => s.PaymentStatus == filter.PaymentStatus.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(s => s.SaleNumber.Contains(search));
        }
        return query;
    }

    /// <summary>
    /// Construye la query de la tabla aplicando los filtros compartidos y la prioridad
    /// de estado (IncludeAllStatuses &gt; DocumentStatus &gt; Confirmadas por defecto).
    /// 1) Sin nada (ausente)                  -&gt; solo Confirmadas (default).
    /// 2) IncludeAllStatuses == true          -&gt; sin filtro de estado (todas).
    /// 3) DocumentStatus presente (y no all)  -&gt; ese estado específico.
    /// </summary>
    private IQueryable<Sale> BuildTableQuery(ReportFilterDto filter)
    {
        var sharedQuery = BuildSharedQuery(filter);
        if (filter.IncludeAllStatuses)
        {
            return sharedQuery; // Caso 2: todas las ventas.
        }
        if (filter.DocumentStatus.HasValue)
        {
            return sharedQuery.Where(s => s.Status == filter.DocumentStatus.Value); // Caso 3
        }
        return sharedQuery.Where(s => s.Status == SaleStatus.Confirmed); // Caso 1 (default)
    }

    /// <summary>
    /// Proyecta IQueryable&lt;Sale&gt; a IQueryable&lt;SalesReportItemDto&gt; con LEFT JOIN a
    /// Customers (CustomerName) y Users (VendorName por CreatedBy). Todo en base de datos.
    /// </summary>
    private IQueryable<SalesReportItemDto> ProjectSales(IQueryable<Sale> sales)
    {
        return
            from sale in sales
            join customer in _repository.CustomersQueryable() on sale.CustomerId equals customer.Id into customerGroup
            from customer in customerGroup.DefaultIfEmpty()
            join vendor in _repository.UsersQueryable() on sale.CreatedBy equals vendor.Id into vendorGroup
            from vendor in vendorGroup.DefaultIfEmpty()
            select new SalesReportItemDto
            {
                SaleNumber = sale.SaleNumber,
                SaleDate = sale.SaleDate,
                CustomerName = customer != null ? customer.Name : string.Empty,
                VendorName = vendor != null ? vendor.FullName : string.Empty,
                PaymentType = sale.PaymentType,
                PaymentStatus = sale.PaymentStatus,
                Status = sale.Status,
                Subtotal = sale.Subtotal,
                Tax = sale.Tax,
                Total = sale.Total,
            };
    }

    private static IQueryable<SalesReportItemDto> ApplySorting(IQueryable<SalesReportItemDto> query, ReportFilterDto f)
    {
        var descending = f.SortDirection == SortDirection.Desc;
        // Mapeo de SortBy (nombre externo) a miembro del DTO. Por defecto: SaleDate.
        return f.SortBy?.ToLowerInvariant() switch
        {
            "salenumber" => descending ? query.OrderByDescending(x => x.SaleNumber) : query.OrderBy(x => x.SaleNumber),
            "saledate" => descending ? query.OrderByDescending(x => x.SaleDate) : query.OrderBy(x => x.SaleDate),
            "customername" => descending ? query.OrderByDescending(x => x.CustomerName) : query.OrderBy(x => x.CustomerName),
            "vendorname" => descending ? query.OrderByDescending(x => x.VendorName) : query.OrderBy(x => x.VendorName),
            "total" => descending ? query.OrderByDescending(x => x.Total) : query.OrderBy(x => x.Total),
            "subtotal" => descending ? query.OrderByDescending(x => x.Subtotal) : query.OrderBy(x => x.Subtotal),
            "tax" => descending ? query.OrderByDescending(x => x.Tax) : query.OrderBy(x => x.Tax),
            "status" => descending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "paymenttype" => descending ? query.OrderByDescending(x => x.PaymentType) : query.OrderBy(x => x.PaymentType),
            "paymentstatus" => descending ? query.OrderByDescending(x => x.PaymentStatus) : query.OrderBy(x => x.PaymentStatus),
            _ => descending ? query.OrderByDescending(x => x.SaleDate) : query.OrderBy(x => x.SaleDate),
        };
    }
}
