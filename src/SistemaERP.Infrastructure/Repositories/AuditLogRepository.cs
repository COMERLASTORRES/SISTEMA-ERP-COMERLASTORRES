using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Application.DTOs;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Infrastructure.Contexts;

namespace SistemaERP.Infrastructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly SistemaERPDbContext _context;

        public AuditLogRepository(SistemaERPDbContext context)
        {
            _context = context;
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
            var query = _context.AuditLogs
                .Where(al => al.TenantId == tenantId);

            if (from.HasValue)
                query = query.Where(al => al.Timestamp >= from.Value);
            if (to.HasValue)
                query = query.Where(al => al.Timestamp <= to.Value);
            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(al => al.Action == action);
            if (userId.HasValue)
                query = query.Where(al => al.UserId == userId.Value);
            if (entityId.HasValue)
                query = query.Where(al => al.EntityId == entityId.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(al => al.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedAuditLogResult
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AuditLog?> GetByIdAsync(Guid id)
        {
            return await _context.AuditLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(al => al.Id == id);
        }

        public async Task<AuditLog> AddAsync(AuditLog auditLog)
        {
            var entity = await _context.AuditLogs.AddAsync(auditLog);
            return entity.Entity;
        }
    }
}