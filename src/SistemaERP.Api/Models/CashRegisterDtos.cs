using System;
using System.Collections.Generic;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Api.Models;

public class OpenCashRegisterDto
{
    public decimal OpeningAmount { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? Notes { get; set; }
}

public class CloseCashRegisterDto
{
    public decimal ClosingAmount { get; set; }
    public string? Notes { get; set; }
}

public class RegisterCashMovementDto
{
    public int Type { get; set; } // CashMovementType: 0=Income, 1=Expense
    public int Reason { get; set; } // MovementReason
    public int PaymentMethod { get; set; } // PaymentMethod
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? SaleId { get; set; }
}

// Respuestas (no exponen la entidad directamente).
public class CashMovementResponseDto
{
    public Guid Id { get; set; }
    public CashMovementType Type { get; set; }
    public MovementReason Reason { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? SaleId { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CashRegisterResponseDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string CashRegisterNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid? WarehouseId { get; set; }
    public decimal OpeningAmount { get; set; }
    public DateTime OpeningDate { get; set; }
    public decimal? ClosingAmount { get; set; }
    public DateTime? ClosingDate { get; set; }
    public decimal? ExpectedAmount { get; set; }
    public decimal? Difference { get; set; }
    public CashRegisterStatus Status { get; set; }
    public string? Notes { get; set; }
    public List<CashMovementResponseDto> Movements { get; set; } = new();
}
