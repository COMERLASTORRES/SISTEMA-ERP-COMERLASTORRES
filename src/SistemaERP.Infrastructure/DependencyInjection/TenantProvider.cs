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

        return Guid.Empty;
    }
}

