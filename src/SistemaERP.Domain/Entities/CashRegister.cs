using System;

namespace SistemaERP.Domain.Entities;

/// <summary>
/// Sesión de caja (turno de caja) para un usuario.
///
/// Reglas de negocio (validadas en el Service, documentadas aquí para claridad):
/// 1) Solo puede haber UNA CashRegister en estado Open por UserId a la vez.
///    El Service debe rechazar abrir una nueva caja si el usuario ya tiene una abierta.
/// 2) Una caja Closed NUNCA puede reabrirse. Para continuar operando se abre SIEMPRE
///    una nueva sesión (nuevo correlativo CAJ-00000X), no se reutiliza la cerrada.
/// 3) Mientras NO exista una caja abierta para el usuario, no se pueden registrar
///    ventas al contado (Cash) ni movimientos manuales de caja. La apertura de caja
///    es el pre-requisito para estas operaciones.
/// </summary>
public class CashRegister : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    /// <summary>Correlativo interno "CAJ-000001", único por tenant.</summary>
    public string CashRegisterNumber { get; set; } = string.Empty;

    public Guid UserId { get; set; }
    public Guid? WarehouseId { get; set; }

    public decimal OpeningAmount { get; set; }
    public DateTime OpeningDate { get; set; }

    public decimal? ClosingAmount { get; set; }
    public DateTime? ClosingDate { get; set; }

    public decimal? ExpectedAmount { get; set; }
    public decimal? Difference { get; set; }

    public CashRegisterStatus Status { get; set; } = CashRegisterStatus.Open;

    public string? Notes { get; set; }

    public ICollection<CashMovement> Movements { get; set; } = new List<CashMovement>();
}

public enum CashRegisterStatus
{
    Open,
    Closed
}
