using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SistemaERP.Application.DTOs;
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

        // Maps a CreateSupplierDto into a new Supplier entity belonging to the given tenant.
        private Supplier FromCreate(CreateSupplierDto dto, Guid tenantId)
        {
            return new Supplier
            {
                TenantId = tenantId,
                DocumentType = ParseDocumentType(dto.DocumentType),
                DocumentNumber = dto.DocumentNumber,
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                ContactPerson = dto.ContactPerson,
                PaymentTermDays = dto.PaymentTermDays,
                IsActive = dto.IsActive,
            };
        }

        // Maps an UpdateSupplierDto onto an existing Supplier entity.
        private Supplier ApplyUpdate(Supplier supplier, UpdateSupplierDto dto)
        {
            supplier.Name = dto.Name;
            supplier.Email = dto.Email;
            supplier.Phone = dto.Phone;
            supplier.Address = dto.Address;
            supplier.ContactPerson = dto.ContactPerson;
            supplier.PaymentTermDays = dto.PaymentTermDays;
            supplier.IsActive = dto.IsActive;
            return supplier;
        }

        // Convierte el string del tipo de documento en el enum de dominio.
        private static DocumentType ParseDocumentType(string documentType)
        {
            return documentType?.ToUpperInvariant() switch
            {
                "DNI" => DocumentType.DNI,
                "RUC" => DocumentType.RUC,
                "CE" => DocumentType.CE,
                "SINDOCUMENTO" or "" or null => DocumentType.SinDocumento,
                _ => throw new InvalidOperationException($"Unknown document type: {documentType}")
            };
        }

        public async Task<Supplier> CreateAsync(CreateSupplierDto dto, Guid tenantId)
        {
            var supplier = FromCreate(dto, tenantId);
            ValidateDocumentNumber(supplier.DocumentType, supplier.DocumentNumber);
            ValidatePaymentTermDays(supplier.PaymentTermDays);

            var existing = await _repository.GetByDocumentNumberAsync(supplier.DocumentNumber, supplier.DocumentType);
            if (existing != null && existing.TenantId == tenantId)
            {
                _logger.LogWarning("Duplicate DocumentNumber {DocumentNumber} for tenant {TenantId}",
                    supplier.DocumentNumber, tenantId);
                throw new InvalidOperationException("A supplier with this document number already exists for this tenant.");
            }

            return await _repository.AddAsync(supplier);
        }

        public async Task<Supplier> UpdateAsync(UpdateSupplierDto dto)
        {
            var supplier = await _repository.GetByIdAsync(dto.Id);
            if (supplier == null)
            {
                _logger.LogWarning("Supplier with Id {SupplierId} not found for update.", dto.Id);
                throw new InvalidOperationException("Supplier not found.");
            }

            ApplyUpdate(supplier, dto);

            // El DocumentType no se actualiza: solo se validan los campos mutables.
            ValidateDocumentNumber(supplier.DocumentType, supplier.DocumentNumber);
            ValidatePaymentTermDays(supplier.PaymentTermDays);

            return await _repository.UpdateAsync(supplier);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _repository.DeleteAsync(id);
        }

        private void ValidateDocumentNumber(DocumentType documentType, string documentNumber)
        {
            var valid = documentType switch
            {
                DocumentType.DNI => Regex.IsMatch(documentNumber, @"^\d{8}$"),
                DocumentType.RUC => Regex.IsMatch(documentNumber, @"^(10|20)\d{9}$"),
                DocumentType.CE => !string.IsNullOrEmpty(documentNumber) &&
                                   documentNumber.Length <= 12 &&
                                   Regex.IsMatch(documentNumber, @"^[a-zA-Z0-9]+$"),
                DocumentType.SinDocumento => true,
                _ => throw new InvalidOperationException("Unknown document type.")
            };

            if (!valid)
            {
                _logger.LogWarning("Invalid format for {DocumentType}: {DocumentNumber}",
                    documentType, documentNumber);
                throw new InvalidOperationException("Invalid document number format for the specified document type.");
            }
        }

        private void ValidatePaymentTermDays(int paymentTermDays)
        {
            if (paymentTermDays < 0)
            {
                _logger.LogWarning("Negative PaymentTermDays: {PaymentTermDays}", paymentTermDays);
                throw new InvalidOperationException("Payment term days cannot be negative.");
            }
        }
    }
}
