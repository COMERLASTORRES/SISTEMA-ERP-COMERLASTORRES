using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Repositories;

public interface ISupplierRepository
{
    Task<IReadOnlyList<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(Guid id);
    Task<Supplier?> GetByDocumentNumberAsync(string documentNumber, DocumentType documentType);
    Task<IReadOnlyList<Supplier>> GetByDocumentNumberAsync(string documentNumber);
    Task<Supplier> AddAsync(Supplier supplier);
    Task<Supplier> UpdateAsync(Supplier supplier);
    Task DeleteAsync(Guid id);
}