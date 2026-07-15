using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Repositories;

public interface IStockMovementRepository
{
    Task<IReadOnlyList<StockMovement>> GetAllAsync();
    Task<IReadOnlyList<StockMovement>> GetByProductIdAsync(Guid productId);
    Task<StockMovement?> GetByIdAsync(Guid id);
    Task<StockMovement> AddAsync(StockMovement stockMovement);
}
