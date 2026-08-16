using System.ComponentModel.DataAnnotations;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.DTOs;

/// <summary>
/// Validación cross-field para Purchase: si PaymentType es Credit, CreditDays debe ser > 0.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PurchasePaymentTypeValidationAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var dto = validationContext.ObjectInstance;

        var paymentTypeProperty = dto.GetType().GetProperty("PaymentType");
        var creditDaysProperty = dto.GetType().GetProperty("CreditDays");

        if (paymentTypeProperty == null || creditDaysProperty == null)
            return ValidationResult.Success;

        var paymentType = (int)paymentTypeProperty.GetValue(dto)!;
        var creditDays = creditDaysProperty.GetValue(dto) as int?;

        // PaymentType: 0 = Cash, 1 = Credit
        if (paymentType == 1 && (!creditDays.HasValue || creditDays.Value <= 0))
        {
            return new ValidationResult("Para compras a crédito (PaymentType=Credit), CreditDays debe ser mayor a 0.");
        }

        return ValidationResult.Success;
    }
}

[PurchasePaymentTypeValidation]
public class PurchaseItemDto
{
    [Required(ErrorMessage = "Product ID is required.")]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public int Quantity { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Unit cost must be greater than zero.")]
    public decimal UnitCost { get; set; }

    [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100.")]
    public decimal DiscountPercentage { get; set; } = 0;
}

[PurchasePaymentTypeValidation]
public class CreatePurchaseDto
{
    [Required(ErrorMessage = "Supplier ID is required.")]
    public Guid SupplierId { get; set; }

    public Guid? WarehouseId { get; set; }

    [Required(ErrorMessage = "Voucher type is required.")]
    [Range(0, 3, ErrorMessage = "Invalid voucher type.")]
    public int VoucherType { get; set; }

    [Required(ErrorMessage = "Voucher number is required.")]
    [MaxLength(50, ErrorMessage = "Voucher number cannot exceed 50 characters.")]
    public string VoucherNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Purchase date is required.")]
    public DateTime PurchaseDate { get; set; }

    [Range(0, 1, ErrorMessage = "Invalid currency.")]
    public int Currency { get; set; } = 0; // PEN

    [Range(0.0001, double.MaxValue, ErrorMessage = "Exchange rate must be positive.")]
    public decimal ExchangeRate { get; set; } = 1;

    [Required(ErrorMessage = "Payment type is required.")]
    [Range(0, 1, ErrorMessage = "Invalid payment type.")]
    public int PaymentType { get; set; }

    public int? PaymentMethod { get; set; }

    [Range(1, 365, ErrorMessage = "Credit days must be between 1 and 365.")]
    public int? CreditDays { get; set; }

    [MaxLength(500, ErrorMessage = "Observations cannot exceed 500 characters.")]
    public string? Observations { get; set; }

    [Required(ErrorMessage = "At least one item is required.")]
    [MaxItems(100, ErrorMessage = "Maximum 100 items allowed per purchase.")]
    public List<PurchaseItemDto> Items { get; set; } = new();
}

public class UpdatePurchaseDto : CreatePurchaseDto
{
    [Required(ErrorMessage = "Purchase ID is required.")]
    public Guid Id { get; set; }
}