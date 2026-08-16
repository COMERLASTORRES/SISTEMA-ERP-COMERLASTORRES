using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash);
    Task<IReadOnlyList<PasswordResetToken>> GetByUserIdAsync(Guid userId);
    Task<PasswordResetToken> AddAsync(PasswordResetToken resetToken);
    Task<PasswordResetToken> UpdateAsync(PasswordResetToken resetToken);
    Task DeleteExpiredAsync(DateTime before);
}