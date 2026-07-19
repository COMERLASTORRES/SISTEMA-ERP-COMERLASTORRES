using System.Collections.Generic;
using System.Threading.Tasks;
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
    Task<Sale> CreateDraftAsync(Sale sale);
    Task<Sale> UpdateDraftAsync(Sale sale);
    Task<StockValidationResult> ValidateStockAsync(Guid tenantId, IEnumerable<SaleItem> items);
    Task ConfirmAsync(Guid saleId, Guid userId);
    Task CancelAsync(Guid saleId);
    Task DeleteAsync(Guid saleId);
}
