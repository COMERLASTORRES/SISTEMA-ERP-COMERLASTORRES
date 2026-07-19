using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

public interface IPurchaseService
{
    Task<IReadOnlyList<Purchase>> GetAllAsync();
    Task<Purchase?> GetByIdAsync(Guid id);
    Task<Purchase> CreateDraftAsync(Purchase purchase);
    Task<Purchase> UpdateDraftAsync(Purchase purchase);
    Task ConfirmAsync(Guid purchaseId, Guid userId);
    Task CancelAsync(Guid purchaseId, Guid userId, string? reason = null);
    Task DeleteAsync(Guid purchaseId);
}
