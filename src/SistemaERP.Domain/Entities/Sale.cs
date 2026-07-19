using System;
using System.Collections.Generic;

namespace SistemaERP.Domain.Entities;

/// <summary>
/// Cabecera de una Venta.
///
/// Reglas de estado (coinciden con el patrón de Compras):
/// 1) Solo una venta en estado Draft es editable. Confirmed y Cancelled no se editan.
/// 2) Una venta Confirmed solo puede pasar a Cancelled (no se vuelve a Draft).
/// 3) Una venta Cancelada es terminal.
///
/// VoucherNumber queda preparado para una futura integración con SUNAT (emisión
/// electrónica de boletas/facturas). Hoy es opcional y no se valida su formato.
///
/// PaymentStatus / PaymentType son campos simples hoy, pero están preparados para una
/// futura tabla de Payments que permita pagos parciales (Partial) y múltiples métodos de
/// pago por venta. Por ahora se calculan de forma directa según el PaymentType.
/// </summary>
public class Sale : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    // Numeración interna correlativa "VEN-000001", única por tenant.
    public string SaleNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public Guid? WarehouseId { get; set; }

    public VoucherType VoucherType { get; set; }
    public string? VoucherNumber { get; set; }
    public DateTime SaleDate { get; set; }

    public Currency Currency { get; set; } = Currency.PEN;
    public decimal ExchangeRate { get; set; } = 1;

    public PaymentType PaymentType { get; set; }
    // Solo aplica si PaymentType == Cash.
    public PaymentMethod? PaymentMethod { get; set; }
    // Solo aplica si PaymentType == Credit.
    public int? CreditDays { get; set; }
    // Calculado como SaleDate + CreditDays.
    public DateTime? DueDate { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    public SaleStatus Status { get; set; } = SaleStatus.Draft;

    public Guid? CreatedBy { get; set; }
    public Guid? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    // Auditoría de cancelación (espejo de Purchase). Al cancelar un documento Confirmed,
    // el sistema genera automáticamente movimientos inversos de stock (y de caja si aplica)
    // referenciando el documento original — NUNCA se eliminan los movimientos previos, para
    // preservar trazabilidad completa de auditoría. Se implementa en la Parte 2.
    public Guid? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public string? Observations { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();

    /// <summary>
    /// Transición de dominio para el cobro TOTAL de una venta a crédito. Cambia
    /// PaymentStatus a Paid. Encapsula la regla de negocio para que la futura
    /// implementación de pagos parciales (Cuentas por Cobrar con entidad SalePayment)
    /// no disperse lógica en los servicios.
    ///
    /// Nota: esta versión simplificada solo cubre el cobro total. El módulo completo
    /// de Cuentas por Cobrar incorporará una entidad SalePayment que permitirá pagos
    /// parciales, múltiples métodos de pago por venta, múltiples pagos sobre una misma
    /// venta, reversión individual de pagos e historial completo. La arquitectura actual
    /// (PaymentStatus + esta transición) queda preparada para esa evolución sin cambios
    /// estructurales en la entidad Sale.
    /// </summary>
    public void RegisterFullPayment()
    {
        PaymentStatus = PaymentStatus.Paid;
    }
}

public enum PaymentType
{
    Cash,
    Credit
}

public enum PaymentMethod
{
    Cash,
    Card,
    Transfer,
    YapePlin,
    Other
}

public enum PaymentStatus
{
    Pending,
    Partial,
    Paid
}

public enum SaleStatus
{
    Draft,
    Confirmed,
    Cancelled
}
