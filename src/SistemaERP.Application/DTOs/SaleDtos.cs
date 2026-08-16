using System.ComponentModel.DataAnnotations;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.DTOs;

/// <summary>
/// Validación cross-field para Sale: si PaymentType es Credit, CreditDays debe ser > 0.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SalePaymentTypeValidationAttribute : ValidationAttribute
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
            return new ValidationResult("Para ventas a crédito (PaymentType=Credit), CreditDays debe ser mayor a 0.");
        }

        return ValidationResult.Success;
    }
}

[SalePaymentTypeValidation]
public class SaleItemDto
{
    [Required(ErrorMessage = "Product ID is required.")]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public int Quantity { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than zero.")]
    public decimal UnitPrice { get; set; }

    [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100.")]
    public decimal DiscountPercentage { get; set; } = 0;

    [Range(0, 100, ErrorMessage = "Tax percentage must be between 0 and 100.")]
    public decimal TaxPercentage { get; set; } = 18;
}

[SalePaymentTypeValidation]
public class CreateSaleDto
{
    [Required(ErrorMessage = "Customer ID is required.")]
    public Guid CustomerId { get; set; }

    public Guid? WarehouseId { get; set; }

    [Required(ErrorMessage = "Voucher type is required.")]
    [Range(0, 3, ErrorMessage = "Invalid voucher type.")]
    public int VoucherType { get; set; }

    [MaxLength(50, ErrorMessage = "Voucher number cannot exceed 50 characters.")]
    public string? VoucherNumber { get; set; }

    [Required(ErrorMessage = "Sale date is required.")]
    public DateTime SaleDate { get; set; }

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
    [MaxItems(100, ErrorMessage = "Maximum 100 items allowed per sale.")]
    public List<SaleItemDto> Items { get; set; } = new();
}

public class UpdateSaleDto : CreateSaleDto
{
    [Required(ErrorMessage = "Sale ID is required.")]
    public Guid Id { get; set; }
}

// DTOs para validación de stock
public class ValidateStockItemDto
{
    [Required(ErrorMessage = "Product ID is required.")]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public int Quantity { get; set; }
}

public class ValidateStockDto
{
    [Required(ErrorMessage = "At least one item is required.")]
    [MaxItems(100, ErrorMessage = "Maximum 100 items allowed.")]
    public List<ValidateStockItemDto> Items { get; set; } = new();
}

// DTOs para respuesta
public class StockValidationErrorDto
{
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public int RequestedQuantity { get; set; }
    public int AvailableStock { get; set; }
}

public class StockValidationResponseDto
{
    public bool IsValid { get; set; }
    public List<StockValidationErrorDto> Errors { get; set; } = new();
}