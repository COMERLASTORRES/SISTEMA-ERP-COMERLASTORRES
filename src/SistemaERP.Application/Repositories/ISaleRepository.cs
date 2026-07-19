using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Repositories;

public interface ISaleRepository
{
    Task<IReadOnlyList<Sale>> GetAllAsync();
    Task<Sale?> GetByIdAsync(Guid id);
    Task<string?> GetLastSaleNumberAsync(Guid tenantId);
    Task<Sale> AddAsync(Sale sale);
    Task<Sale> UpdateAsync(Sale sale);
    Task DeleteAsync(Guid id);
}
