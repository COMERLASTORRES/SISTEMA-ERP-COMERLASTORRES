using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Application.DTOs;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

public class StockValidationError
{
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public int RequestedQuantity { get; set; }
    public int AvailableStock { get; set; }
}

public class StockValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<StockValidationError> Errors { get; set; } = new();
}

public interface ISaleService
{
    Task<IReadOnlyList<Sale>> GetAllAsync();
    Task<Sale?> GetByIdAsync(Guid id);
    Task<Sale> CreateDraftAsync(CreateSaleDto dto, Guid tenantId, Guid userId);
    Task<Sale> UpdateDraftAsync(UpdateSaleDto dto, Guid tenantId);
    Task<StockValidationResult> ValidateStockAsync(Guid tenantId, IEnumerable<ValidateStockItemDto> items);
    Task ConfirmAsync(Guid saleId, Guid userId);
    Task RegisterFullPaymentAsync(Guid saleId, Guid userId, PaymentMethod paymentMethod);
    Task CancelAsync(Guid saleId, Guid userId, string? reason = null);
    Task DeleteAsync(Guid saleId, Guid tenantId);
}
