using System;
using System.Collections.Generic;

namespace SistemaERP.Domain.Entities;

/// <summary>
/// Cabecera de una Compra a un proveedor.
///
/// Reglas de estado:
/// 1) Solo una compra en estado Draft es editable. Confirmed y Cancelled no se editan.
/// 2) Una compra Confirmed solo puede pasar a Cancelled (no se vuelve a Draft).
/// 3) Una compra Cancelada es terminal.
///
/// VoucherNumber es el número del documento del proveedor (factura/boleta). Hoy es
/// obligatorio y no se valida su formato.
///
/// Filosofía de pagos (idéntica a Sale, reutilizando los mismos enums compartidos):
/// - PaymentType = Cash  -> PaymentMethod es OBLIGATORIO; al confirmar la compra se
///   generará un EGRESO automático en Caja dentro de la misma transacción (Parte 2).
/// - PaymentType = Credit -> CreditDays debe ser > 0 y DueDate = PurchaseDate + CreditDays
///   (calculado en el Service). NO genera movimiento de Caja; queda preparada para una
///   futura entidad PurchasePayment que permitirá pagos parciales (PaymentStatus = Partial)
///   y múltiples pagos.
/// - PaymentStatus hoy se calcula de forma directa (Paid para Cash, Pending para Credit),
///   pero está preparado para la futura tabla de pagos parciales.
/// </summary>
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

    public PaymentType PaymentType { get; set; }
    // Solo aplica si PaymentType == Cash. Obligatorio en ese caso.
    public PaymentMethod? PaymentMethod { get; set; }
    // Solo aplica si PaymentType == Credit. Debe ser > 0.
    public int? CreditDays { get; set; }
    // Calculado como PurchaseDate + CreditDays (en el Service al confirmar/crear).
    public DateTime? DueDate { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

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

    // Auditoría de cancelación (espejo de Sale). Al cancelar un documento Confirmed,
    // el sistema genera automáticamente movimientos inversos de stock (y de caja si aplica)
    // referenciando el documento original — NUNCA se eliminan los movimientos previos, para
    // preservar trazabilidad completa de auditoría. Se implementa en la Parte 2.
    public Guid? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

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
