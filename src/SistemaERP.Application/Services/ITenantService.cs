using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

public interface ITenantService
{
    Task<IReadOnlyList<Tenant>> GetAllAsync();
    Task<Tenant?> GetByIdAsync(Guid id);
    Task<Tenant> CreateAsync(Tenant tenant);
    Task<string?> GenerateNextPurchaseNumberAsync(Guid tenantId);
    Task<string?> GenerateNextSaleNumberAsync(Guid tenantId);
}
