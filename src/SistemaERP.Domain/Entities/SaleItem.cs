using System;

namespace SistemaERP.Domain.Entities;

/// <summary>
/// Detalle de una Venta. Actúa como un snapshot histórico del precio: los valores
/// (UnitPrice, DiscountPercentage, TaxPercentage y los totales de línea) se calculan y
/// se persisten en el momento de la venta. Esto es deliberado: cambios futuros en el
/// precio del Producto NUNCA deben alterar el historial de ventas ya registradas.
/// </summary>
public class SaleItem : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; } = 0;
    public decimal TaxPercentage { get; set; } = 18;

    public decimal LineSubtotal { get; set; }
    public decimal LineTax { get; set; }
    public decimal LineTotal { get; set; }
}
