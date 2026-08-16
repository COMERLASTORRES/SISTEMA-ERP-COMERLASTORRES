using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Application.DTOs;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

public interface IPurchaseService
{
    Task<IReadOnlyList<Purchase>> GetAllAsync();
    Task<Purchase?> GetByIdAsync(Guid id);
    Task<Purchase> CreateDraftAsync(CreatePurchaseDto dto, Guid tenantId, Guid userId);
    Task<Purchase> UpdateDraftAsync(UpdatePurchaseDto dto, Guid tenantId);
    Task ConfirmAsync(Guid purchaseId, Guid userId);
    Task CancelAsync(Guid purchaseId, Guid userId, string? reason = null);
    Task RegisterFullPaymentAsync(Guid purchaseId, Guid userId, PaymentMethod paymentMethod);
    Task DeleteAsync(Guid purchaseId, Guid tenantId);
}
