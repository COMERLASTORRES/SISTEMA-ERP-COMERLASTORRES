using Microsoft.EntityFrameworkCore;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Infrastructure.Contexts;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaERP.Infrastructure.Repositories
{
    public class CashRegisterRepository : ICashRegisterRepository
    {
        private readonly SistemaERPDbContext _context;

        public CashRegisterRepository(SistemaERPDbContext context)
        {
            _context = context;
        }

        // Busca la caja abierta (Open) del usuario para el tenant. El filtro multi-tenant
        // ya restringe por TenantId; se agrega el filtro por UserId y Status=Open.
        public async Task<CashRegister?> GetOpenByUserIdAsync(Guid tenantId, Guid userId)
        {
            return await _context.CashRegisters
                .Include(c => c.Movements)
                .FirstOrDefaultAsync(c =>
                    c.TenantId == tenantId &&
                    c.UserId == userId &&
                    c.Status == CashRegisterStatus.Open);
        }

        // Devuelve el último CashRegisterNumber usado para el tenant, para generar el
        // siguiente correlativo (formato "CAJ-000001"). El filtro multi-tenant ya
        // restringe por TenantId. No se incluyen los movimientos aquí (solo el número).
        public async Task<string?> GetLastCashRegisterNumberAsync(Guid tenantId)
        {
            return await _context.CashRegisters
                .Where(c => c.TenantId == tenantId)
                .OrderByDescending(c => c.CashRegisterNumber)
                .Select(c => c.CashRegisterNumber)
                .FirstOrDefaultAsync();
        }

        public async Task<CashRegister?> GetByIdAsync(Guid id)
        {
            return await _context.CashRegisters
                .Include(c => c.Movements)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IReadOnlyList<CashRegister>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _context.CashRegisters
                .AsNoTracking()
                .Include(c => c.Movements)
                .OrderByDescending(c => c.OpeningDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<CashRegister> AddAsync(CashRegister cashRegister)
        {
            var entity = await _context.CashRegisters.AddAsync(cashRegister);
            await _context.SaveChangesAsync();
            return entity.Entity;
        }

        public async Task<CashRegister> UpdateAsync(CashRegister cashRegister)
        {
            _context.Entry(cashRegister).State = EntityState.Modified;
            // Los movimientos se registran en el Service (SaveChanges con el grafo completo).
            await _context.SaveChangesAsync();
            return cashRegister;
        }
    }
}
