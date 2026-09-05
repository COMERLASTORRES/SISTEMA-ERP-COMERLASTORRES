using System;

namespace SistemaERP.Application.DTOs;

public class SupplierDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public int PaymentTermDays { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class CreateSupplierDto
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "El nombre es obligatorio.")]
    [System.ComponentModel.DataAnnotations.MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "El tipo de documento es obligatorio.")]
    public string DocumentType { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "El número de documento es obligatorio.")]
    [System.ComponentModel.DataAnnotations.MaxLength(50, ErrorMessage = "El número de documento no puede exceder 50 caracteres.")]
    public string DocumentNumber { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "El formato de email no es válido.")]
    public string Email { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Phone(ErrorMessage = "El formato de teléfono no es válido.")]
    public string Phone { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(500, ErrorMessage = "La dirección no puede exceder 500 caracteres.")]
    public string Address { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(100, ErrorMessage = "El contacto no puede exceder 100 caracteres.")]
    public string ContactPerson { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(0, 365, ErrorMessage = "Los términos de pago deben estar entre 0 y 365 días.")]
    public int PaymentTermDays { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}

public class UpdateSupplierDto
{
    public Guid Id { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "El nombre es obligatorio.")]
    [System.ComponentModel.DataAnnotations.MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(50, ErrorMessage = "El número de documento no puede exceder 50 caracteres.")]
    public string DocumentNumber { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "El formato de email no es válido.")]
    public string Email { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Phone(ErrorMessage = "El formato de teléfono no es válido.")]
    public string Phone { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(500, ErrorMessage = "La dirección no puede exceder 500 caracteres.")]
    public string Address { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(100, ErrorMessage = "El contacto no puede exceder 100 caracteres.")]
    public string ContactPerson { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(0, 365, ErrorMessage = "Los términos de pago deben estar entre 0 y 365 días.")]
    public int PaymentTermDays { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}