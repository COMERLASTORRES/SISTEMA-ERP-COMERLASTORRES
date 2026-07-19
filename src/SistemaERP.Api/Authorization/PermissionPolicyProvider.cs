using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace SistemaERP.Api.Authorization;

/// <summary>
/// Proveedor de políticas de autorización dinámico: resuelve ON-DEMAND cualquier nombre
/// de política como un requerimiento de claim "permission" con ese mismo valor. Esto
/// permite usar [Authorize(Policy = "sales.create")] sin registrar decenas de políticas
/// a mano. Las políticas conocidas (ej. "Authenticated") se delegan al proveedor por
/// defecto para no romper la configuración estándar de ASP.NET Core.
///
/// El claim "permission" se emite en el JWT durante el login (uno por cada permiso
/// efectivo del usuario), de modo que la validación no consulta la base de datos.
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    // Nombre del claim que porta los permisos en el JWT.
    public const string PermissionClaimType = "permission";

    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    // La política por defecto (sin [Authorize(Policy=...)] explícito) la resuelve el fallback.
    public Task<AuthorizationPolicy?> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    // Políticas de fallback (ej. las definidas en AddAuthorization) las resuelve el fallback.
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Si el fallback ya conoce la política (configuración estándar), la usa.
        var fallbackPolicy = _fallback.GetPolicyAsync(policyName);
        if (fallbackPolicy.Result != null)
            return fallbackPolicy;

        // En caso contrario, tratamos el nombre como un código de permiso y exigimos el claim.
        var policy = new AuthorizationPolicyBuilder()
            .RequireClaim(PermissionClaimType, policyName)
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
