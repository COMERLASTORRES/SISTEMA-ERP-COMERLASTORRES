using System;
using System.Linq;
using System.Threading.Tasks;
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

        // Filtros compartidos (fecha, cliente, vendedor, almacén, tipo/pago, búsqueda)
        // aplicados sobre IQueryable<Sale> con operadores traducibles a SQL. El filtro de
        // estado de documento se aplica por separado a la tabla y al resumen (ver abajo).
        var sharedQuery = _repository.Query();
        if (dateFromUtc.HasValue)
            sharedQuery = sharedQuery.Where(s => s.SaleDate >= dateFromUtc.Value);
        if (dateToUtc.HasValue)
            sharedQuery = sharedQuery.Where(s => s.SaleDate <= dateToUtc.Value);
        if (filter.CustomerId.HasValue)
            sharedQuery = sharedQuery.Where(s => s.CustomerId == filter.CustomerId.Value);
        if (filter.VendorId.HasValue)
            sharedQuery = sharedQuery.Where(s => s.CreatedBy == filter.VendorId.Value);
        if (filter.WarehouseId.HasValue)
            sharedQuery = sharedQuery.Where(s => s.WarehouseId == filter.WarehouseId.Value);
        if (filter.PaymentType.HasValue)
            sharedQuery = sharedQuery.Where(s => s.PaymentType == filter.PaymentType.Value);
        if (filter.PaymentStatus.HasValue)
            sharedQuery = sharedQuery.Where(s => s.PaymentStatus == filter.PaymentStatus.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            sharedQuery = sharedQuery.Where(s => s.SaleNumber.Contains(search));
        }

        // Tabla: prioridad IncludeAllStatuses > DocumentStatus explícito > default Confirmadas.
        // 1) Sin nada (ausente)                          -> solo Confirmadas (default).
        // 2) IncludeAllStatuses == true                  -> sin filtro de estado (todas).
        // 3) DocumentStatus presente (y no all)         -> ese estado específico.
        var tableQuery = sharedQuery;
        if (filter.IncludeAllStatuses)
        {
            // Caso 2: todas las ventas, sin filtro de estado.
        }
        else if (filter.DocumentStatus.HasValue)
        {
            tableQuery = tableQuery.Where(s => s.Status == filter.DocumentStatus.Value); // Caso 3
        }
        else
        {
            tableQuery = tableQuery.Where(s => s.Status == SaleStatus.Confirmed); // Caso 1 (default)
        }

        // Resumen: SIEMPRE solo sobre ventas Confirmed, independientemente del filtro de
        // la tabla. Reusa los filtros compartidos y suma únicamente las confirmadas.
        var confirmedQuery = sharedQuery.Where(s => s.Status == SaleStatus.Confirmed);

        // Proyección con LEFT JOIN a Customers (CustomerName) y Users (VendorName por
        // CreatedBy, que es una FK conceptual sin navegación). Todo en base de datos.
        var projected =
            from sale in tableQuery
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

        // Resumen agregado sobre las ventas Confirmed (en base de datos), sin importar
        // el filtro de estado de la tabla.
        var summary = await (
            from sale in confirmedQuery
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
            })
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

    // ---- helpers ----

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
