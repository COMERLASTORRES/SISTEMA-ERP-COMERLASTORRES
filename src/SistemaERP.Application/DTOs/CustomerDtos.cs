using System;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.DTOs;

public class CustomerDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public CustomerType CustomerType { get; set; }
    public decimal CreditLimit { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class CreateCustomerDto
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "El nombre es obligatorio.")]
    [System.ComponentModel.DataAnnotations.MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "El tipo de documento es obligatorio.")]
    public DocumentType DocumentType { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "El número de documento es obligatorio.")]
    [System.ComponentModel.DataAnnotations.MaxLength(50, ErrorMessage = "El número de documento no puede exceder 50 caracteres.")]
    public string DocumentNumber { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "El tipo de cliente es obligatorio.")]
    public CustomerType CustomerType { get; set; }

    [System.ComponentModel.DataAnnotations.Range(0, 999999999.99, ErrorMessage = "El límite de crédito debe estar entre 0 y 999,999,999.99.")]
    public decimal CreditLimit { get; set; }

    [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "El formato de email no es válido.")]
    public string Email { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Phone(ErrorMessage = "El formato de teléfono no es válido.")]
    public string Phone { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(500, ErrorMessage = "La dirección no puede exceder 500 caracteres.")]
    public string Address { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UpdateCustomerDto
{
    public Guid Id { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "El nombre es obligatorio.")]
    [System.ComponentModel.DataAnnotations.MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "El tipo de documento es obligatorio.")]
    public DocumentType DocumentType { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(50, ErrorMessage = "El número de documento no puede exceder 50 caracteres.")]
    public string DocumentNumber { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "El tipo de cliente es obligatorio.")]
    public CustomerType CustomerType { get; set; }

    [System.ComponentModel.DataAnnotations.Range(0, 999999999.99, ErrorMessage = "El límite de crédito debe estar entre 0 y 999,999,999.99.")]
    public decimal CreditLimit { get; set; }

    [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "El formato de email no es válido.")]
    public string Email { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Phone(ErrorMessage = "El formato de teléfono no es válido.")]
    public string Phone { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(500, ErrorMessage = "La dirección no puede exceder 500 caracteres.")]
    public string Address { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
