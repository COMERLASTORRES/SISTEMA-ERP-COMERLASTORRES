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

        // Filtros aplicados sobre IQueryable<Sale> con operadores traducibles a SQL
        // (la lógica imperativa no es traducible por EF Core). Se filtra ANTES de proyectar.
        var salesQuery = _repository.Query();
        if (filter.DateFrom.HasValue)
            salesQuery = salesQuery.Where(s => s.SaleDate >= filter.DateFrom.Value);
        if (filter.DateTo.HasValue)
            salesQuery = salesQuery.Where(s => s.SaleDate <= filter.DateTo.Value);
        if (filter.CustomerId.HasValue)
            salesQuery = salesQuery.Where(s => s.CustomerId == filter.CustomerId.Value);
        if (filter.VendorId.HasValue)
            salesQuery = salesQuery.Where(s => s.CreatedBy == filter.VendorId.Value);
        if (filter.WarehouseId.HasValue)
            salesQuery = salesQuery.Where(s => s.WarehouseId == filter.WarehouseId.Value);
        if (filter.PaymentType.HasValue)
            salesQuery = salesQuery.Where(s => s.PaymentType == filter.PaymentType.Value);
        if (filter.PaymentStatus.HasValue)
            salesQuery = salesQuery.Where(s => s.PaymentStatus == filter.PaymentStatus.Value);
        if (filter.DocumentStatus.HasValue)
            salesQuery = salesQuery.Where(s => s.Status == filter.DocumentStatus.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            salesQuery = salesQuery.Where(s => s.SaleNumber.Contains(search));
        }

        // Proyección con LEFT JOIN a Customers (CustomerName) y Users (VendorName por
        // CreatedBy, que es una FK conceptual sin navegación). Todo en base de datos.
        var projected =
            from sale in salesQuery
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

        // Resumen agregado sobre el conjunto filtrado completo (en base de datos).
        var summary = await projected
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
