using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Infrastructure.Contexts;

namespace SistemaERP.Infrastructure.Repositories
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly SistemaERPDbContext _context;

        public PermissionRepository(SistemaERPDbContext context)
        {
            _context = context;
        }

        // Los permisos son globales (sin filtro multi-tenant), así que se consultan sin
        // restricción de tenant. Se bypasea el filtro por si acaso para lecturas directas.
        public async Task<IReadOnlyList<Permission>> GetAllAsync()
        {
            return await _context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Code)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Permission>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            var idSet = ids as HashSet<Guid> ?? new HashSet<Guid>(ids);
            return await _context.Permissions
                .AsNoTracking()
                .Where(p => idSet.Contains(p.Id))
                .ToListAsync();
        }

        public async Task AddRangeAsync(IEnumerable<Permission> permissions)
        {
            await _context.Permissions.AddRangeAsync(permissions);
            await _context.SaveChangesAsync();
        }
    }
}
