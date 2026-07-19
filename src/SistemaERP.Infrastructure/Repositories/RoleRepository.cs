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
    public class RoleRepository : IRoleRepository
    {
        private readonly SistemaERPDbContext _context;

        public RoleRepository(SistemaERPDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Role>> GetAllByTenantAsync(Guid tenantId)
        {
            return await _context.Roles
                .AsNoTracking()
                .Where(r => r.TenantId == tenantId)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<Role?> GetByIdAsync(Guid id)
        {
            return await _context.Roles.FindAsync(id);
        }

        public async Task<Role?> GetByIdWithPermissionsAsync(Guid id)
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        // Cuenta usuarios asignados a un rol vía la tabla de unión UserRole. Se usa para
        // impedir eliminar roles que todavía tienen usuarios asociados.
        public async Task<int> GetUserCountAsync(Guid roleId)
        {
            return await _context.UserRoles
                .AsNoTracking()
                .CountAsync(ur => ur.RoleId == roleId);
        }

        public async Task<Role> AddAsync(Role role)
        {
            var entity = await _context.Roles.AddAsync(role);
            await _context.SaveChangesAsync();
            return entity.Entity;
        }

        public async Task<Role> UpdateAsync(Role role)
        {
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
            return role;
        }

        public async Task DeleteAsync(Role role)
        {
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
        }
    }
}
