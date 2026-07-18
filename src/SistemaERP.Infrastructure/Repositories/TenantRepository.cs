using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Infrastructure.Contexts;

namespace SistemaERP.Infrastructure.Repositories
{
    public class TenantRepository : ITenantRepository
    {
        private readonly SistemaERPDbContext _context;

        public TenantRepository(SistemaERPDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Tenant>> GetAllAsync()
        {
            return await _context.Tenants.AsNoTracking().ToListAsync();
        }

        public async Task<Tenant?> GetByIdAsync(Guid id)
        {
            return await _context.Tenants.FindAsync(id);
        }

        public async Task<Tenant> AddAsync(Tenant tenant)
        {
            var entity = await _context.Tenants.AddAsync(tenant);
            await _context.SaveChangesAsync();
            return entity.Entity;
        }
    }
}
