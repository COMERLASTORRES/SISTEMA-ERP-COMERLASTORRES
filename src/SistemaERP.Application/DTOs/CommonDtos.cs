using System.ComponentModel.DataAnnotations;

namespace SistemaERP.Application.DTOs;

/// <summary>
/// Atributo de validación para asegurar que la lista de items no exceda el máximo permitido.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class MaxItemsAttribute : ValidationAttribute
{
    private readonly int _maxItems;

    public MaxItemsAttribute(int maxItems)
    {
        _maxItems = maxItems;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is System.Collections.ICollection collection)
        {
            if (collection.Count > _maxItems)
            {
                return new ValidationResult($"La lista no puede exceder {_maxItems} elementos.");
            }
        }
        return ValidationResult.Success;
    }
}

/// <summary>
/// DTO para cancelación (común para ventas y compras).
/// </summary>
public class CancelDto
{
    [MaxLength(500, ErrorMessage = "Cancellation reason cannot exceed 500 characters.")]
    public string? Reason { get; set; }
}

/// <summary>
/// DTO para registro de pago (común para ventas y compras).
/// </summary>
public class RegisterPaymentDto
{
    [Required(ErrorMessage = "Payment method is required.")]
    [Range(0, 4, ErrorMessage = "Invalid payment method.")]
    public int PaymentMethod { get; set; }
}