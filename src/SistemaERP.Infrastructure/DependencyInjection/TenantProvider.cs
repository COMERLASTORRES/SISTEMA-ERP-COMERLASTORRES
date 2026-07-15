using Microsoft.Extensions.Hosting;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace SistemaERP.Infrastructure.DependencyInjection;

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return Guid.Empty;
        var claim = httpContext.User.FindFirst("tenantId");
        if (claim != null && Guid.TryParse(claim.Value, out var guid))
            return guid;

        // Temporary fallback for development environment when tenantId claim is missing.
        // This should be removed once JWT authentication with proper tenantId claim is implemented.
        var env = httpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Hosting.IHostEnvironment)) as Microsoft.Extensions.Hosting.IHostEnvironment;
        if (env != null && env.IsDevelopment())
        {
            return Guid.Parse("11111111-1111-1111-1111-111111111111");
        }

        return Guid.Empty;
    }
}

