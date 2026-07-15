using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Infrastructure.Contexts;

namespace SistemaERP.Infrastructure.Repositories
{
    public class StockMovementRepository : IStockMovementRepository
    {
        private readonly SistemaERPDbContext _context;

        public StockMovementRepository(SistemaERPDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<StockMovement>> GetAllAsync()
        {
            return await _context.StockMovements.AsNoTracking().ToListAsync();
        }

        public async Task<IReadOnlyList<StockMovement>> GetByProductIdAsync(Guid productId)
        {
            return await _context.StockMovements
                .AsNoTracking()
                .Where(m => m.ProductId == productId)
                .ToListAsync();
        }

        public async Task<StockMovement?> GetByIdAsync(Guid id)
        {
            return await _context.StockMovements.FindAsync(id);
        }

        public async Task<StockMovement> AddAsync(StockMovement stockMovement)
        {
            var entity = await _context.StockMovements.AddAsync(stockMovement);
            await _context.SaveChangesAsync();
            return entity.Entity;
        }
    }
}
