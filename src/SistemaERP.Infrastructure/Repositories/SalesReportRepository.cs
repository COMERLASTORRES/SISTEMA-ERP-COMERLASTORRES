using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Application.Reports.Sales;
using SistemaERP.Domain.Entities;
using SistemaERP.Infrastructure.Contexts;

namespace SistemaERP.Infrastructure.Repositories;

public class SalesReportRepository : ISalesReportRepository
{
    private readonly SistemaERPDbContext _context;

    public SalesReportRepository(SistemaERPDbContext context)
    {
        _context = context;
    }

    public IQueryable<Sale> Query()
    {
        // Sin tracking (solo lectura analítica). El filtro global multi-tenant de Sale ya
        // restringe por tenant automáticamente.
        return _context.Sales.AsNoTracking();
    }

    public IQueryable<Customer> CustomersQueryable()
    {
        return _context.Customers.AsNoTracking();
    }

    public IQueryable<User> UsersQueryable()
    {
        return _context.Users.AsNoTracking();
    }
}
