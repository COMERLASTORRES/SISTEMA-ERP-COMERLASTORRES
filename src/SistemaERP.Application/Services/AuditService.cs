using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SistemaERP.Application.Repositories;
using SistemaERP.Application.Services;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services
{
    public class AuditService : IAuditService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(
            IAuditLogRepository auditLogRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _auditLogRepository = auditLogRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            Guid tenantId,
            string action,
            string? entityType = null,
            Guid? entityId = null,
            object? details = null,
            string? ipAddress = null,
            Guid? userId = null)
        {
            if (tenantId == Guid.Empty)
            {
                // Sin tenant context (ej. login antes de emitir token), no loguear
                return;
            }

            userId ??= GetCurrentUserId();
            ipAddress ??= GetClientIpAddress();

            var auditLog = new AuditLog
            {
                TenantId = tenantId,
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details != null ? JsonSerializer.Serialize(details) : null,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            await _auditLogRepository.AddAsync(auditLog);
        }

        public async Task<PagedAuditLogResult> GetPagedAsync(
            Guid tenantId,
            DateTime? from,
            DateTime? to,
            string? action,
            Guid? userId,
            Guid? entityId,
            int page,
            int pageSize)
        {
            if (tenantId == Guid.Empty)
            {
                return new PagedAuditLogResult
                {
                    Items = Array.Empty<AuditLog>(),
                    TotalCount = 0,
                    Page = page,
                    PageSize = pageSize
                };
            }

            return await _auditLogRepository.GetPagedAsync(
                tenantId, from, to, action, userId, entityId, page, pageSize);
        }

        private Guid? GetCurrentUserId()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.User == null) return null;

            var claim = context.User.FindFirst("userId");
            if (claim != null && Guid.TryParse(claim.Value, out var id))
                return id;

            return null;
        }

        private string? GetClientIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.Connection?.RemoteIpAddress?.ToString();
        }
    }
}