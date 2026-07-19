using Microsoft.EntityFrameworkCore;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Infrastructure.Contexts;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaERP.Infrastructure.Repositories
{
    public class SaleRepository : ISaleRepository
    {
        private readonly SistemaERPDbContext _context;

        public SaleRepository(SistemaERPDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Sale>> GetAllAsync()
        {
            return await _context.Sales
                .AsNoTracking()
                .Include(s => s.Items)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
        }

        public async Task<Sale?> GetByIdAsync(Guid id)
        {
            return await _context.Sales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        // Devuelve el último SaleNumber usado para el tenant, para generar el
        // siguiente correlativo (formato "VEN-000001"). El filtro multi-tenant ya
        // restringe por TenantId, así que basta con proyectar los números del tenant.
        public async Task<string?> GetLastSaleNumberAsync(Guid tenantId)
        {
            return await _context.Sales
                .Where(s => s.TenantId == tenantId)
                .OrderByDescending(s => s.SaleNumber)
                .Select(s => s.SaleNumber)
                .FirstOrDefaultAsync();
        }

        public async Task<Sale> AddAsync(Sale sale)
        {
            var entity = await _context.Sales.AddAsync(sale);
            await _context.SaveChangesAsync();
            return entity.Entity;
        }

        public async Task<Sale> UpdateAsync(Sale sale)
        {
            _context.Entry(sale).State = EntityState.Modified;
            // Los items se actualizan en el Service (SaveChanges con el grafo completo).
            await _context.SaveChangesAsync();
            return sale;
        }

        public async Task DeleteAsync(Guid id)
        {
            var sale = await _context.Sales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (sale != null)
            {
                _context.Sales.Remove(sale);
                await _context.SaveChangesAsync();
            }
        }
    }
}
