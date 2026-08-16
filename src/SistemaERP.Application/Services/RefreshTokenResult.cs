using System;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

/// <summary>
/// Resultado de operaciones con refresh tokens.
/// </summary>
public class RefreshTokenResult
{
    public string RefreshToken { get; set; } = string.Empty; // Token plano (solo al crear/rotar)
    public string TokenHash { get; set; } = string.Empty;    // Hash para almacenar/validar
    public DateTime ExpiresAt { get; set; }
    public Guid TokenId { get; set; }
    public Guid UserId { get; set; } // Usuario al que pertenece el token

    public static RefreshTokenResult Create(string plainToken, string tokenHash, DateTime expiresAt, Guid tokenId, Guid userId)
    {
        return new RefreshTokenResult
        {
            RefreshToken = plainToken,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            TokenId = tokenId,
            UserId = userId
        };
    }

    public static RefreshTokenResult FromEntity(RefreshToken entity, string? plainToken = null)
    {
        return new RefreshTokenResult
        {
            RefreshToken = plainToken ?? string.Empty,
            TokenHash = entity.TokenHash,
            ExpiresAt = entity.ExpiresAt,
            TokenId = entity.Id,
            UserId = entity.UserId
        };
    }
}