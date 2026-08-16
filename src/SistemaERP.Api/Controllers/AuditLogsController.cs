using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaERP.Application.Services;
using SistemaERP.Domain;

namespace SistemaERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = PermissionCodes.AuditLogsView)]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditLogsController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        // GET: api/audit-logs
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? action,
            [FromQuery] Guid? userId,
            [FromQuery] Guid? entityId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 200) pageSize = 200;

            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            var result = await _auditService.GetPagedAsync(
                tenantId, from, to, action, userId, entityId, page, pageSize);

            return Ok(new
            {
                items = result.Items.Select(al => new
                {
                    al.Id,
                    al.TenantId,
                    al.UserId,
                    al.Action,
                    al.EntityType,
                    al.EntityId,
                    al.Details,
                    al.IpAddress,
                    al.Timestamp
                }),
                totalCount = result.TotalCount,
                page = result.Page,
                pageSize = result.PageSize,
                totalPages = result.TotalPages
            });
        }

        private Guid GetTenantId()
        {
            var claim = User.FindFirst("tenantId");
            if (claim != null && Guid.TryParse(claim.Value, out var id))
                return id;
            return Guid.Empty;
        }
    }
}