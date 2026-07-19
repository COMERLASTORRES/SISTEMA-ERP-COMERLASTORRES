using System;

namespace SistemaERP.Domain.Entities;

/// <summary>
/// Detalle de un movimiento de caja. Es inmutable (historial), al igual que StockMovement:
/// una vez registrado no se edita ni se borra.
///
/// Preparado para el futuro módulo de Cuentas por Cobrar / Pagar:
/// - Los cobros de ventas a crédito generarán CashMovement con Reason = CustomerPayment.
/// - Los pagos a proveedores generarán CashMovement con Reason = SupplierPayment.
/// El campo SaleId (opcional, FK a Sale) vincula el movimiento a una venta cuando aplica
/// (ej. cobro de una venta), y es NULL para movimientos manuales de caja.
///
/// CreatedAt se hereda de AuditableEntity y registra cuándo se creó el movimiento.
/// </summary>
public class CashMovement : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CashRegisterId { get; set; }

    public CashMovementType Type { get; set; }
    public MovementReason Reason { get; set; }

    /// <summary>Reutiliza el enum PaymentMethod definido en Sale.cs (no se duplica).</summary>
    public PaymentMethod PaymentMethod { get; set; }

    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;

    public Guid? SaleId { get; set; }

    public Guid? CreatedBy { get; set; }
}

public enum CashMovementType
{
    Income,
    Expense
}

public enum MovementReason
{
    Sale,
    CustomerPayment,
    SupplierPayment,
    CashWithdrawal,
    PettyCash,
    Other
}
