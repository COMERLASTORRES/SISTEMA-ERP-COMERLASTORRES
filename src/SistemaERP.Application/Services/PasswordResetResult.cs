using System;

namespace SistemaERP.Application.Services;

/// <summary>
/// Resultado de operaciones de reseteo de contraseña.
/// </summary>
public class PasswordResetResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResetToken { get; set; } // Solo en DEV (token plano)
    public string? ResetLink { get; set; }  // Solo en DEV (link completo)
    public DateTime? ExpiresAt { get; set; }

    public static PasswordResetResult Ok(string? resetToken = null, string? resetLink = null, DateTime? expiresAt = null)
    {
        return new PasswordResetResult
        {
            Success = true,
            ResetToken = resetToken,
            ResetLink = resetLink,
            ExpiresAt = expiresAt
        };
    }

    public static PasswordResetResult Fail(string errorMessage)
    {
        return new PasswordResetResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}