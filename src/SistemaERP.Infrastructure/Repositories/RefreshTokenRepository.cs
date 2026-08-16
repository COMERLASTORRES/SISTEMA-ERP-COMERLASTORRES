using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Infrastructure.Contexts;

namespace SistemaERP.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly SistemaERPDbContext _context;

        public RefreshTokenRepository(SistemaERPDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
        {
            return await _context.RefreshTokens
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
        }

        public async Task<IReadOnlyList<RefreshToken>> GetByUserIdAsync(Guid userId)
        {
            return await _context.RefreshTokens
                .AsNoTracking()
                .Where(rt => rt.UserId == userId)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }

        public async Task<RefreshToken?> GetActiveByUserIdAsync(Guid userId)
        {
            return await _context.RefreshTokens
                .AsNoTracking()
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .OrderByDescending(rt => rt.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<RefreshToken> AddAsync(RefreshToken refreshToken)
        {
            var entity = await _context.RefreshTokens.AddAsync(refreshToken);
            return entity.Entity;
        }

        public async Task<RefreshToken> UpdateAsync(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken;
        }

        public async Task DeleteExpiredAsync(DateTime before)
        {
            var expired = await _context.RefreshTokens
                .Where(rt => rt.ExpiresAt < before)
                .ToListAsync();

            if (expired.Any())
            {
                _context.RefreshTokens.RemoveRange(expired);
                await _context.SaveChangesAsync();
            }
        }
    }
}