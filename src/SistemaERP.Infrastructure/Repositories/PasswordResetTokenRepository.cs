using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Infrastructure.Contexts;

namespace SistemaERP.Infrastructure.Repositories
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly SistemaERPDbContext _context;

        public PasswordResetTokenRepository(SistemaERPDbContext context)
        {
            _context = context;
        }

        public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash)
        {
            return await _context.PasswordResetTokens
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(prt => prt.TokenHash == tokenHash);
        }

        public async Task<IReadOnlyList<PasswordResetToken>> GetByUserIdAsync(Guid userId)
        {
            return await _context.PasswordResetTokens
                .AsNoTracking()
                .Where(prt => prt.UserId == userId)
                .OrderByDescending(prt => prt.CreatedAt)
                .ToListAsync();
        }

        public async Task<PasswordResetToken> AddAsync(PasswordResetToken resetToken)
        {
            var entity = await _context.PasswordResetTokens.AddAsync(resetToken);
            return entity.Entity;
        }

        public async Task<PasswordResetToken> UpdateAsync(PasswordResetToken resetToken)
        {
            _context.PasswordResetTokens.Update(resetToken);
            await _context.SaveChangesAsync();
            return resetToken;
        }

        public async Task DeleteExpiredAsync(DateTime before)
        {
            var expired = await _context.PasswordResetTokens
                .Where(prt => prt.ExpiresAt < before)
                .ToListAsync();

            if (expired.Any())
            {
                _context.PasswordResetTokens.RemoveRange(expired);
                await _context.SaveChangesAsync();
            }
        }
    }
}