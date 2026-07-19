using Microsoft.EntityFrameworkCore;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Infrastructure.Contexts;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaERP.Infrastructure.Repositories
{
    public class PurchaseRepository : IPurchaseRepository
    {
        private readonly SistemaERPDbContext _context;

        public PurchaseRepository(SistemaERPDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Purchase>> GetAllAsync()
        {
            return await _context.Purchases
                .AsNoTracking()
                .Include(p => p.Items)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();
        }

        public async Task<Purchase?> GetByIdAsync(Guid id)
        {
            return await _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // Devuelve el último PurchaseNumber usado para el tenant, para generar el
        // siguiente correlativo (formato "PUR-000001"). El filtro multi-tenant ya
        // restringe por TenantId, así que basta con proyectar los números del tenant.
        public async Task<string?> GetLastPurchaseNumberAsync(Guid tenantId)
        {
            return await _context.Purchases
                .Where(p => p.TenantId == tenantId)
                .OrderByDescending(p => p.PurchaseNumber)
                .Select(p => p.PurchaseNumber)
                .FirstOrDefaultAsync();
        }

        public async Task<Purchase> AddAsync(Purchase purchase)
        {
            var entity = await _context.Purchases.AddAsync(purchase);
            await _context.SaveChangesAsync();
            return entity.Entity;
        }

        public async Task<Purchase> UpdateAsync(Purchase purchase)
        {
            _context.Entry(purchase).State = EntityState.Modified;
            // Los items se actualizan en el Service (SaveChanges con el grafo completo).
            await _context.SaveChangesAsync();
            return purchase;
        }

        public async Task DeleteAsync(Guid id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (purchase != null)
            {
                _context.Purchases.Remove(purchase);
                await _context.SaveChangesAsync();
            }
        }
    }
}
