using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Application.Repositories;

namespace SistemaERP.Application.Services;

public interface IAuditService
{
    Task LogAsync(
        Guid tenantId,
        string action,
        string? entityType = null,
        Guid? entityId = null,
        object? details = null,
        string? ipAddress = null,
        Guid? userId = null);

    Task<PagedAuditLogResult> GetPagedAsync(
        Guid tenantId,
        DateTime? from,
        DateTime? to,
        string? action,
        Guid? userId,
        Guid? entityId,
        int page,
        int pageSize);
}