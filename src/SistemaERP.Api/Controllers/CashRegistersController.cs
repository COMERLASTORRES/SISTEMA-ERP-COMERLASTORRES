using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SistemaERP.Application.Services;
using SistemaERP.Domain;
using SistemaERP.Domain.Entities;
using SistemaERP.Api.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaERP.Api.Controllers
{
    [ApiController]
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
        [Authorize(Policy = PermissionCodes.CashRegisterOpen)]
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
        [Authorize(Policy = PermissionCodes.CashRegisterClose)]
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
        [Authorize(Policy = PermissionCodes.CashRegisterMovement)]
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
        [Authorize(Policy = PermissionCodes.CashRegisterView)]
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
        [Authorize(Policy = PermissionCodes.CashRegisterView)]
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
        [Authorize(Policy = PermissionCodes.CashRegisterView)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var register = await _cashRegisterService.GetByIdAsync(id);
            if (register == null) return NotFound();
            return Ok(MapToDto(register));
        }

        // GET: api/CashRegisters/{id}/movements/export/excel
        [HttpGet("{id}/movements/export/excel")]
        [Authorize(Policy = PermissionCodes.CashRegisterView)]
        public async Task<IActionResult> ExportMovementsExcel(Guid id)
        {
            var register = await _cashRegisterService.GetByIdAsync(id);
            if (register == null) return NotFound();
            var bytes = GenerateCashMovementsExcel(register.Movements);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"movimientos-caja-{register.CashRegisterNumber}.xlsx");
        }

        private static byte[] GenerateCashMovementsExcel(IEnumerable<CashMovement> movements)
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Movs. Caja");

            // ---- Encabezados ----
            var headers = new[] { "Fecha", "Tipo", "Motivo", "Método de Pago", "Monto", "Descripción" };
            for (var i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            }

            // ---- Filas ----
            var items = movements.ToList();
            for (var row = 0; row < items.Count; row++)
            {
                var item = items[row];
                var r = row + 2;

                var typeLabel = item.Type switch
                {
                    CashMovementType.Income => "Ingreso",
                    CashMovementType.Expense => "Egreso",
                    _ => "—"
                };

                var reasonLabel = item.Reason switch
                {
                    MovementReason.Sale => "Venta",
                    MovementReason.CustomerPayment => "Cobro a Cliente",
                    MovementReason.SupplierPayment => "Pago a Proveedor",
                    MovementReason.CashWithdrawal => "Retiro de Efectivo",
                    MovementReason.PettyCash => "Caja Chica",
                    MovementReason.Other => "Otro",
                    _ => "—"
                };

                var paymentMethodLabel = item.PaymentMethod switch
                {
                    PaymentMethod.Cash => "Efectivo",
                    PaymentMethod.Card => "Tarjeta",
                    PaymentMethod.Transfer => "Transferencia",
                    PaymentMethod.YapePlin => "Yape/Plin",
                    PaymentMethod.Other => "Otro",
                    _ => "—"
                };

                worksheet.Cell(r, 1).Value = item.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(r, 2).Value = typeLabel;
                worksheet.Cell(r, 3).Value = reasonLabel;
                worksheet.Cell(r, 4).Value = paymentMethodLabel;
                worksheet.Cell(r, 5).Value = item.Amount;
                worksheet.Cell(r, 6).Value = item.Description ?? string.Empty;

                worksheet.Cell(r, 5).Style.NumberFormat.Format = "#,##0.00";
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
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
