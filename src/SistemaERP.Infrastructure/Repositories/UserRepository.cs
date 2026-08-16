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
            // Respeta el filtro global de tenant: solo encuentra usuarios del tenant actual.
            // Usado en operaciones tenant-scoped (ej. UpdateUserAsync).
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByIdIgnoringTenantAsync(Guid id)
        {
            // Ignora el filtro de tenant: busca en TODOS los tenants.
            // Usado SOLO en casos cross-tenant legítimos (ej. reset de contraseña via token).
            return await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        // Carga el usuario junto con sus asignaciones de rol (UserRole). Necesario para
        // resolver los permisos efectivos sin dispersar la lógica de inclusión en los servicios.
        // Se omite el filtro global multi-tenant de UserRole porque el userId ya acota al
        // usuario correcto; de lo contrario, durante el login (donde el TenantProvider aún no
        // resuelve el tenant desde el token) el filtro excluye todos los UserRole y el token
        // se genera sin claims de permiso, provocando 403 en todos los endpoints protegidos.
        // El IgnoreQueryFilters es local a esta consulta y no afecta otras queries.
        public async Task<User?> GetByIdWithRolesAsync(Guid id)
        {
            return await _context.Users
                .IgnoreQueryFilters()
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
