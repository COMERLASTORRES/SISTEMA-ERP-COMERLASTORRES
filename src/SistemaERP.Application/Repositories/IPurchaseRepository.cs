using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Repositories;

public interface IPurchaseRepository
{
    Task<IReadOnlyList<Purchase>> GetAllAsync();
    Task<Purchase?> GetByIdAsync(Guid id);
    Task<string?> GetLastPurchaseNumberAsync(Guid tenantId);
    Task<Purchase> AddAsync(Purchase purchase);
    Task<Purchase> UpdateAsync(Purchase purchase);
    Task DeleteAsync(Guid id);
}
