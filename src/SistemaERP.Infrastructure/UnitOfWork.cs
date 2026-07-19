using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Application.Services;
using SistemaERP.Infrastructure.Contexts;

namespace SistemaERP.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly SistemaERPDbContext _context;

    public UnitOfWork(SistemaERPDbContext context)
    {
        _context = context;
    }

    public Task BeginTransactionAsync()
    {
        return _context.Database.BeginTransactionAsync();
    }

    public Task CommitAsync()
    {
        return _context.Database.CommitTransactionAsync();
    }

    public Task RollbackAsync()
    {
        return _context.Database.RollbackTransactionAsync();
    }
}
