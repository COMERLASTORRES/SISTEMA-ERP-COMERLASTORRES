using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Infrastructure.Contexts;

namespace SistemaERP.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly SistemaERPDbContext _context;

        public UserRepository(SistemaERPDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<User>> GetAllAsync()
        {
            return await _context.Users.AsNoTracking().ToListAsync();
        }

        public async Task<IReadOnlyList<User>> GetAllByTenantAsync(Guid tenantId)
        {
            return await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.TenantId == tenantId)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        // Carga el usuario junto con sus asignaciones de rol (UserRole). Necesario para
        // resolver los permisos efectivos sin dispersar la lógica de inclusión en los servicios.
        public async Task<User?> GetByIdWithRolesAsync(Guid id)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        // Login/registration look up a user by email across ALL tenants, before any
        // tenant context exists, so the multi-tenant query filter must be bypassed.
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> AddAsync(User user)
        {
            var entity = await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return entity.Entity;
        }

        public async Task<User> UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }
    }
}
