using Microsoft.EntityFrameworkCore;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Infrastructure.Contexts;
using System.Threading.Tasks;

namespace SistemaERP.Infrastructure.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly SistemaERPDbContext _context;

        public SupplierRepository(SistemaERPDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Supplier>> GetAllAsync()
        {
            return await _context.Suppliers.AsNoTracking().ToListAsync();
        }

        public async Task<Supplier?> GetByIdAsync(Guid id)
        {
            return await _context.Suppliers.FindAsync(id);
        }

        public async Task<Supplier?> GetByDocumentNumberAsync(string documentNumber, DocumentType documentType)
        {
            return await _context.Suppliers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.DocumentNumber == documentNumber && s.DocumentType == documentType);
        }

        public async Task<IReadOnlyList<Supplier>> GetByDocumentNumberAsync(string documentNumber)
        {
            return await _context.Suppliers
                .IgnoreQueryFilters()
                .Where(s => s.DocumentNumber == documentNumber)
                .ToListAsync();
        }

        public async Task<Supplier> AddAsync(Supplier supplier)
        {
            var entity = await _context.Suppliers.AddAsync(supplier);
            await _context.SaveChangesAsync();
            return entity.Entity;
        }

        public async Task<Supplier> UpdateAsync(Supplier supplier)
        {
            _context.Entry(supplier).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return supplier;
        }

        public async Task DeleteAsync(Guid id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
            }
        }
    }
}