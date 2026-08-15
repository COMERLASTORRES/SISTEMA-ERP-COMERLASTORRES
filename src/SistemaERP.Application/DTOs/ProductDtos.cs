using System.ComponentModel.DataAnnotations;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.DTOs;

/// <summary>
/// Atributo de validación para asegurar que SalePrice >= PurchasePrice.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SalePriceGreaterThanPurchasePriceAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var dto = validationContext.ObjectInstance;

        var purchasePriceProperty = dto.GetType().GetProperty("PurchasePrice");
        var salePriceProperty = dto.GetType().GetProperty("SalePrice");

        if (purchasePriceProperty == null || salePriceProperty == null)
        {
            return new ValidationResult("PurchasePrice and SalePrice properties not found for cross-field validation.");
        }

        var purchasePrice = (decimal)purchasePriceProperty.GetValue(dto)!;
        var salePrice = (decimal)salePriceProperty.GetValue(dto)!;

        if (salePrice < purchasePrice)
        {
            return new ValidationResult("Sale price must be greater than or equal to purchase price.");
        }

        return ValidationResult.Success;
    }
}

[SalePriceGreaterThanPurchasePrice]
public class CreateProductDto
{
    [Required(ErrorMessage = "Product code is required.")]
    [MaxLength(50, ErrorMessage = "Product code cannot exceed 50 characters.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product name is required.")]
    [MaxLength(200, ErrorMessage = "Product name cannot exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "Barcode cannot exceed 50 characters.")]
    public string? Barcode { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Purchase price must be non-negative.")]
    public decimal PurchasePrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Sale price must be non-negative.")]
    public decimal SalePrice { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock must be non-negative.")]
    public int Stock { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Minimum stock must be non-negative.")]
    public int StockMinimum { get; set; }

    public Guid? CategoryId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateProductDto : CreateProductDto
{
    [Required(ErrorMessage = "Product ID is required.")]
    public Guid Id { get; set; }
}