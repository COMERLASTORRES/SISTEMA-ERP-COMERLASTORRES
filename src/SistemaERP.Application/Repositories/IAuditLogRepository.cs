using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Application.DTOs;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Repositories;

public class PagedAuditLogResult
{
    public IReadOnlyList<AuditLog> Items { get; set; } = Array.Empty<AuditLog>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public interface IAuditLogRepository
{
    Task<PagedAuditLogResult> GetPagedAsync(
        Guid tenantId,
        DateTime? from,
        DateTime? to,
        string? action,
        Guid? userId,
        Guid? entityId,
        int page,
        int pageSize);

    Task<AuditLog?> GetByIdAsync(Guid id);
    Task<AuditLog> AddAsync(AuditLog auditLog);
}