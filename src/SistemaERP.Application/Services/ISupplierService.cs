using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services
{
    public interface ISupplierService
    {
        Task<IReadOnlyList<Supplier>> GetAllAsync();
        Task<Supplier?> GetByIdAsync(Guid id);
        Task<Supplier> CreateAsync(Supplier supplier);
        Task<Supplier> UpdateAsync(Supplier supplier);
        Task DeleteAsync(Guid id);
    }
}