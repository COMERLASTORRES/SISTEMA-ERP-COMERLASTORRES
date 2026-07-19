using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _repository;
        private readonly ILogger<SupplierService> _logger;

        public SupplierService(ISupplierRepository repository, ILogger<SupplierService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Supplier>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Supplier?> GetByIdAsync(Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<Supplier> CreateAsync(Supplier supplier)
        {
            ValidateDocumentNumber(supplier);
            ValidatePaymentTermDays(supplier);

            var existing = await _repository.GetByDocumentNumberAsync(supplier.DocumentNumber, supplier.DocumentType);
            if (existing != null && existing.TenantId == supplier.TenantId)
            {
                _logger.LogWarning("Duplicate DocumentNumber {DocumentNumber} for tenant {TenantId}",
                    supplier.DocumentNumber, supplier.TenantId);
                throw new InvalidOperationException("A supplier with this document number already exists for this tenant.");
            }

            return await _repository.AddAsync(supplier);
        }

        public async Task<Supplier> UpdateAsync(Supplier supplier)
        {
            ValidateDocumentNumber(supplier);
            ValidatePaymentTermDays(supplier);

            var existing = await _repository.GetByDocumentNumberAsync(supplier.DocumentNumber, supplier.DocumentType);
            if (existing != null && existing.TenantId == supplier.TenantId && existing.Id != supplier.Id)
            {
                _logger.LogWarning("Duplicate DocumentNumber {DocumentNumber} for tenant {TenantId}",
                    supplier.DocumentNumber, supplier.TenantId);
                throw new InvalidOperationException("A supplier with this document number already exists for this tenant.");
            }

            return await _repository.UpdateAsync(supplier);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _repository.DeleteAsync(id);
        }

        private void ValidateDocumentNumber(Supplier supplier)
        {
            var valid = supplier.DocumentType switch
            {
                DocumentType.DNI => Regex.IsMatch(supplier.DocumentNumber, @"^\d{8}$"),
                DocumentType.RUC => Regex.IsMatch(supplier.DocumentNumber, @"^(10|20)\d{9}$"),
                DocumentType.CE => !string.IsNullOrEmpty(supplier.DocumentNumber) &&
                                   supplier.DocumentNumber.Length <= 12 &&
                                   Regex.IsMatch(supplier.DocumentNumber, @"^[a-zA-Z0-9]+$"),
                DocumentType.SinDocumento => true,
                _ => throw new InvalidOperationException("Unknown document type.")
            };

            if (!valid)
            {
                _logger.LogWarning("Invalid format for {DocumentType}: {DocumentNumber}",
                    supplier.DocumentType, supplier.DocumentNumber);
                throw new InvalidOperationException("Invalid document number format for the specified document type.");
            }
        }

        private void ValidatePaymentTermDays(Supplier supplier)
        {
            if (supplier.PaymentTermDays < 0)
            {
                _logger.LogWarning("Negative PaymentTermDays: {PaymentTermDays}", supplier.PaymentTermDays);
                throw new InvalidOperationException("Payment term days cannot be negative.");
            }
        }
    }
}