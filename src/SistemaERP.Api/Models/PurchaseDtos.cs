using System;
using System.Collections.Generic;

namespace SistemaERP.Api.Models;

public class PurchaseItemDto
{
    public Guid? ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal DiscountPercentage { get; set; } = 0;
}

public class CreatePurchaseDto
{
    public Guid SupplierId { get; set; }
    public Guid? WarehouseId { get; set; }
    public int VoucherType { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public int Currency { get; set; } = 0; // PEN
    public decimal ExchangeRate { get; set; } = 1;
    public string? Observations { get; set; }
    public List<PurchaseItemDto> Items { get; set; } = new();
}

// Para edición reutilizamos la misma forma que creación, pero incluyendo el Id de la cabecera.
public class UpdatePurchaseDto : CreatePurchaseDto
{
    public Guid Id { get; set; }
}
