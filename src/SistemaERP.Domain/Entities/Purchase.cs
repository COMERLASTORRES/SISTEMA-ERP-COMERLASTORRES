using System;
using System.Collections.Generic;

namespace SistemaERP.Domain.Entities;

public class Purchase : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    // Numeración interna autogenerada tipo "PUR-000001", correlativa por tenant.
    // Se genera en el Service (no en la entidad), consultando el último PurchaseNumber del tenant.
    public string PurchaseNumber { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }
    public Guid? WarehouseId { get; set; }

    public VoucherType VoucherType { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }

    public Currency Currency { get; set; } = Currency.PEN;
    public decimal ExchangeRate { get; set; } = 1;

    public string? Observations { get; set; }

    // Calculados a partir de los items en el Service.
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    public PurchaseStatus Status { get; set; } = PurchaseStatus.Draft;

    // FK conceptual a User (sin restricción real por ahora).
    public Guid? CreatedBy { get; set; }
    public Guid? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();

    // REGLA DE NEGOCIO: una compra en estado Confirmed no se puede editar; solo se permite
    // cambiar su Status a Cancelled. Solo las compras en Draft son editables.
    // (Validación aplicada en el Service en la Parte 2.)
}

public enum VoucherType
{
    Boleta,
    Factura,
    NotaCredito,
    Otro
}

public enum Currency
{
    PEN,
    USD
}

public enum PurchaseStatus
{
    Draft,
    Confirmed,
    Cancelled
}
