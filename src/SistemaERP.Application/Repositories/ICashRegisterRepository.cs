using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Repositories;

public interface ICashRegisterRepository
{
    Task<CashRegister?> GetOpenByUserIdAsync(Guid tenantId, Guid userId);
    Task<string?> GetLastCashRegisterNumberAsync(Guid tenantId);
    Task<CashRegister?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<CashRegister>> GetAllAsync(int page = 1, int pageSize = 10);
    Task<CashRegister> AddAsync(CashRegister cashRegister);
    Task<CashRegister> UpdateAsync(CashRegister cashRegister);
    Task<CashMovement> AddMovementAsync(CashMovement movement);
}
