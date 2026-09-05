using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaERP.Application.DTOs;
using SistemaERP.Application.Repositories;
using SistemaERP.Application.Services;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ICustomerRepository customerRepository, ILogger<CustomerService> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all customers.");
        return await _customerRepository.GetAllAsync();
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching customer with Id {CustomerId}.", id);
        return await _customerRepository.GetByIdAsync(id);
    }

    // Maps CreateCustomerDto into a new Customer entity belonging to the given tenant.
    public async Task<Customer> CreateAsync(CreateCustomerDto dto, Guid tenantId)
    {
        var customer = new Customer
        {
            TenantId = tenantId,
            Name = dto.Name,
            DocumentType = dto.DocumentType,
            DocumentNumber = dto.DocumentNumber,
            CustomerType = dto.CustomerType,
            CreditLimit = dto.CreditLimit,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            IsActive = dto.IsActive,
        };

        ValidateDocument(customer);

        var existing = await _customerRepository.GetAllAsync();
        foreach (var c in existing)
        {
            if (c.TenantId == customer.TenantId && string.Equals(c.DocumentNumber, customer.DocumentNumber, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Attempt to create customer with duplicate document {DocumentNumber} for tenant {TenantId}.", customer.DocumentNumber, customer.TenantId);
                throw new InvalidOperationException($"Document number '{customer.DocumentNumber}' already exists for this tenant.");
            }
        }

        _logger.LogInformation("Creating customer {CustomerName} for tenant {TenantId}.", customer.Name, customer.TenantId);
        return await _customerRepository.AddAsync(customer);
    }

    // Applies the DTO fields onto the existing Customer entity.
    public async Task<Customer> UpdateAsync(UpdateCustomerDto dto)
    {
        var customer = await _customerRepository.GetByIdAsync(dto.Id);
        if (customer == null)
        {
            _logger.LogWarning("Customer with Id {CustomerId} not found for update.", dto.Id);
            throw new InvalidOperationException("Customer not found.");
        }

        customer.Name = dto.Name;
        customer.DocumentType = dto.DocumentType;
        customer.DocumentNumber = dto.DocumentNumber;
        customer.CustomerType = dto.CustomerType;
        customer.CreditLimit = dto.CreditLimit;
        customer.Email = dto.Email;
        customer.Phone = dto.Phone;
        customer.Address = dto.Address;
        customer.IsActive = dto.IsActive;

        // Reject if the document number already belongs to another customer in the same tenant.
        var existing = await _customerRepository.GetAllAsync();
        foreach (var c in existing)
        {
            if (c.Id != customer.Id && c.TenantId == customer.TenantId && string.Equals(c.DocumentNumber, customer.DocumentNumber, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Attempt to update customer {CustomerId} to duplicate document {DocumentNumber} for tenant {TenantId}.", customer.Id, customer.DocumentNumber, customer.TenantId);
                throw new InvalidOperationException($"Document number '{customer.DocumentNumber}' already exists for this tenant.");
            }
        }

        ValidateDocument(customer);

        try
        {
            _logger.LogInformation("Updating customer {CustomerId}.", customer.Id);
            return await _customerRepository.UpdateAsync(customer);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Customer {CustomerId} was modified by another process.", customer.Id);
            throw new InvalidOperationException("The customer was modified by another process. Please refresh and try again.");
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting customer {CustomerId}.", id);
            await _customerRepository.DeleteAsync(id);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Customer {CustomerId} was modified by another process and could not be deleted.", id);
            throw new InvalidOperationException("The customer was modified by another process and could not be deleted.");
        }
    }

    private void ValidateDocument(Customer customer)
    {
        if (customer.CustomerType == CustomerType.Corporativo && customer.DocumentType != DocumentType.RUC)
        {
            _logger.LogWarning("Corporate customer {CustomerName} requires RUC document type.", customer.Name);
            throw new InvalidOperationException("Corporate customers require DocumentType RUC.");
        }

        switch (customer.DocumentType)
        {
            case DocumentType.DNI:
                if (!IsDigits(customer.DocumentNumber, 8))
                {
                    throw new InvalidOperationException("DNI must be exactly 8 digits.");
                }
                break;
            case DocumentType.RUC:
                if (string.IsNullOrWhiteSpace(customer.DocumentNumber) ||
                    customer.DocumentNumber.Length != 11 ||
                    !customer.DocumentNumber.All(char.IsDigit) ||
                    !(customer.DocumentNumber.StartsWith("10") || customer.DocumentNumber.StartsWith("20")))
                {
                    throw new InvalidOperationException("RUC must be 11 digits starting with 10 or 20.");
                }
                break;
            case DocumentType.CE:
                if (string.IsNullOrWhiteSpace(customer.DocumentNumber) ||
                    customer.DocumentNumber.Length > 12 ||
                    !customer.DocumentNumber.All(c => char.IsLetterOrDigit(c)))
                {
                    throw new InvalidOperationException("CE must be alphanumeric with a maximum of 12 characters.");
                }
                break;
            case DocumentType.SinDocumento:
                break;
            default:
                throw new InvalidOperationException("Invalid document type.");
        }
    }

    private static bool IsDigits(string value, int length)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Length == length && value.All(char.IsDigit);
    }
}
