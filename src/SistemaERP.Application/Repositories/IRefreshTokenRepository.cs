using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task<IReadOnlyList<RefreshToken>> GetByUserIdAsync(Guid userId);
    Task<RefreshToken?> GetActiveByUserIdAsync(Guid userId);
    Task<RefreshToken> AddAsync(RefreshToken refreshToken);
    Task<RefreshToken> UpdateAsync(RefreshToken refreshToken);
    Task DeleteExpiredAsync(DateTime before);
}