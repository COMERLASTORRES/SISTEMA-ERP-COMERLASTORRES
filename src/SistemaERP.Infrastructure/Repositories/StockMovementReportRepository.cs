using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Application.Reports.StockMovements;
using SistemaERP.Domain.Entities;
using SistemaERP.Infrastructure.Contexts;

namespace SistemaERP.Infrastructure.Repositories;

public class StockMovementReportRepository : IStockMovementReportRepository
{
    private readonly SistemaERPDbContext _context;

    public StockMovementReportRepository(SistemaERPDbContext context)
    {
        _context = context;
    }

    public IQueryable<StockMovement> Query()
    {
        // Sin tracking (solo lectura analítica). El filtro global multi-tenant de StockMovement
        // ya restringe por tenant automáticamente.
        return _context.StockMovements.AsNoTracking();
    }

    public IQueryable<Product> ProductsQueryable()
    {
        return _context.Products.AsNoTracking();
    }
}