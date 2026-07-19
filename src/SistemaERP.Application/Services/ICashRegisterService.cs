using System;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

public interface ICashRegisterService
{
    Task<CashRegister> OpenAsync(Guid tenantId, Guid userId, decimal openingAmount, Guid? warehouseId = null, string? notes = null);
    Task CloseAsync(Guid cashRegisterId, decimal closingAmount, string? notes = null);
    Task RegisterMovementAsync(
        Guid cashRegisterId,
        CashMovementType type,
        MovementReason reason,
        PaymentMethod paymentMethod,
        decimal amount,
        string description,
        Guid? saleId = null,
        Guid? userId = null);
    Task<CashRegister?> GetOpenCashRegisterForUserAsync(Guid tenantId, Guid userId);
    Task<IReadOnlyList<CashRegister>> GetAllAsync(int page = 1, int pageSize = 10);
    Task<CashRegister?> GetByIdAsync(Guid id);
}
