using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SistemaERP.Application.Services;
using SistemaERP.Domain.Entities;
using SistemaERP.Api.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaERP.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CashRegistersController : ControllerBase
    {
        private readonly ICashRegisterService _cashRegisterService;
        public CashRegistersController(ICashRegisterService cashRegisterService)
        {
            _cashRegisterService = cashRegisterService;
        }

        // POST: api/CashRegisters/open
        [HttpPost("open")]
        public async Task<IActionResult> Open([FromBody] OpenCashRegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.OpeningAmount < 0) return BadRequest("El monto de apertura no puede ser negativo.");

            var tenantId = GetTenantId();
            var userId = GetUserId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");
            if (userId == Guid.Empty) return BadRequest("UserId missing in claim");

            try
            {
                var register = await _cashRegisterService.OpenAsync(
                    tenantId, userId, dto.OpeningAmount, dto.WarehouseId, dto.Notes);
                return CreatedAtAction(nameof(GetById), new { id = register.Id }, MapToDto(register));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/CashRegisters/{id}/close
        [HttpPost("{id}/close")]
        public async Task<IActionResult> Close(Guid id, [FromBody] CloseCashRegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.ClosingAmount < 0) return BadRequest("El monto de cierre no puede ser negativo.");

            try
            {
                await _cashRegisterService.CloseAsync(id, dto.ClosingAmount, dto.Notes);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/CashRegisters/{id}/movements (movimientos manuales: retiros, caja chica, etc.)
        [HttpPost("{id}/movements")]
        public async Task<IActionResult> RegisterMovement(Guid id, [FromBody] RegisterCashMovementDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            if (userId == Guid.Empty) return BadRequest("UserId missing in claim");

            try
            {
                await _cashRegisterService.RegisterMovementAsync(
                    id,
                    (CashMovementType)dto.Type,
                    (MovementReason)dto.Reason,
                    (PaymentMethod)dto.PaymentMethod,
                    dto.Amount,
                    dto.Description,
                    dto.SaleId,
                    userId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/CashRegisters/open (caja abierta del usuario actual, o null si no hay)
        [HttpGet("open")]
        public async Task<IActionResult> GetOpen()
        {
            var tenantId = GetTenantId();
            var userId = GetUserId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");
            if (userId == Guid.Empty) return BadRequest("UserId missing in claim");

            var register = await _cashRegisterService.GetOpenCashRegisterForUserAsync(tenantId, userId);
            // JsonResult explícito con 200 para garantizar body + status y evitar
            // cualquier conversión implícita a 204 No Content por parte del framework.
            return new JsonResult(register == null ? null : MapToDto(register))
            {
                StatusCode = StatusCodes.Status200OK,
            };
        }

        // GET: api/CashRegisters (historial paginado)
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var all = (await _cashRegisterService.GetAllAsync(page, pageSize)).AsEnumerable();

            var items = all.Select(MapToDto).ToList();
            return Ok(new
            {
                Total = items.Count,
                Page = page,
                PageSize = pageSize,
                Items = items,
            });
        }

        // GET: api/CashRegisters/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var register = await _cashRegisterService.GetByIdAsync(id);
            if (register == null) return NotFound();
            return Ok(MapToDto(register));
        }

        private CashRegisterResponseDto MapToDto(CashRegister register)
        {
            return new CashRegisterResponseDto
            {
                Id = register.Id,
                TenantId = register.TenantId,
                CashRegisterNumber = register.CashRegisterNumber,
                UserId = register.UserId,
                WarehouseId = register.WarehouseId,
                OpeningAmount = register.OpeningAmount,
                OpeningDate = register.OpeningDate,
                ClosingAmount = register.ClosingAmount,
                ClosingDate = register.ClosingDate,
                ExpectedAmount = register.ExpectedAmount,
                Difference = register.Difference,
                Status = register.Status,
                Notes = register.Notes,
                Movements = register.Movements.Select(m => new CashMovementResponseDto
                {
                    Id = m.Id,
                    Type = m.Type,
                    Reason = m.Reason,
                    PaymentMethod = m.PaymentMethod,
                    Amount = m.Amount,
                    Description = m.Description,
                    SaleId = m.SaleId,
                    CreatedBy = m.CreatedBy,
                    CreatedAt = m.CreatedAt,
                }).ToList(),
            };
        }

        private Guid GetTenantId()
        {
            var claim = User.FindFirst("tenantId");
            if (claim != null && Guid.TryParse(claim.Value, out var id))
                return id;
            return Guid.Empty;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst("userId");
            if (claim != null && Guid.TryParse(claim.Value, out var id))
                return id;
            return Guid.Empty;
        }
    }
}
