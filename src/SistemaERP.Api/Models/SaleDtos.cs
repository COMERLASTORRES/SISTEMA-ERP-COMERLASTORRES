using System;
using System.Collections.Generic;

namespace SistemaERP.Api.Models;

public class SaleItemDto
{
    public Guid? ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; } = 0;
    // El IGv por defecto es 18%; el backend lo usa para calcular LineTax si no se envía.
    public decimal TaxPercentage { get; set; } = 18;
}

public class CreateSaleDto
{
    public Guid CustomerId { get; set; }
    public Guid? WarehouseId { get; set; }
    public int VoucherType { get; set; }
    public string? VoucherNumber { get; set; }
    public DateTime SaleDate { get; set; }
    public int Currency { get; set; } = 0; // PEN
    public decimal ExchangeRate { get; set; } = 1;
    public int PaymentType { get; set; }
    public int? PaymentMethod { get; set; }
    public int? CreditDays { get; set; }
    public string? Observations { get; set; }
    public List<SaleItemDto> Items { get; set; } = new();
}

// Para edición reutilizamos la misma forma que creación, pero incluyendo el Id de la cabecera.
public class UpdateSaleDto : CreateSaleDto
{
    public Guid Id { get; set; }
}

// Respuesta de validación de stock previa a la confirmación.
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
