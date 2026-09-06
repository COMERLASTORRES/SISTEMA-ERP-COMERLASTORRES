using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaERP.Application.DTOs;
using SistemaERP.Application.Services;
using SistemaERP.Domain;
using SistemaERP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly ISaleService _saleService;
        private readonly ISaleDocumentService _saleDocumentService;
        public SalesController(ISaleService saleService, ISaleDocumentService saleDocumentService)
        {
            _saleService = saleService;
            _saleDocumentService = saleDocumentService;
        }

        // GET: api/Sales?status=0&customerId=...&page=1&pageSize=10
        [HttpGet]
        [Authorize(Policy = PermissionCodes.SalesView)]
        public async Task<IActionResult> Get(
            [FromQuery] SaleStatus? status,
            [FromQuery] Guid? customerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var all = (await _saleService.GetAllAsync()).AsEnumerable();

            if (status.HasValue)
                all = all.Where(s => s.Status == status.Value);
            if (customerId.HasValue)
                all = all.Where(s => s.CustomerId == customerId.Value);

            var total = all.Count();
            var items = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = items,
            });
        }

        // GET: api/Sales/{id}
        [HttpGet("{id}")]
        [Authorize(Policy = PermissionCodes.SalesView)]
        public async Task<IActionResult> Get(Guid id)
        {
            var sale = await _saleService.GetByIdAsync(id);
            if (sale == null) return NotFound();
            return Ok(sale);
        }

        // POST: api/Sales (crea en Draft)
        [HttpPost]
        [Authorize(Policy = PermissionCodes.SalesCreate)]
        public async Task<IActionResult> Post([FromBody] CreateSaleDto dto)
        {
            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            var userId = GetUserId();
            if (userId == Guid.Empty) return BadRequest("UserId missing in claim");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (dto.Items.Any(i => i.ProductId == Guid.Empty))
                return BadRequest("Todos los items deben tener un producto válido.");

            try
            {
                var created = await _saleService.CreateDraftAsync(dto, tenantId, userId);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/Sales/{id}
        [HttpPut("{id}")]
        [Authorize(Policy = PermissionCodes.SalesEdit)]
        public async Task<IActionResult> Put(Guid id, [FromBody] UpdateSaleDto dto)
        {
            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            var existing = await _saleService.GetByIdAsync(id);
            if (existing == null) return NotFound("La venta no existe.");

            if (existing.TenantId != tenantId) return Forbid();

            if (id != dto.Id) return BadRequest("ID mismatch");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (dto.Items.Any(i => i.ProductId == Guid.Empty))
                return BadRequest("Todos los items deben tener un producto válido.");

            try
            {
                await _saleService.UpdateDraftAsync(dto, tenantId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/Sales/validate-stock
        // Validación previa de stock para que el frontend pueda chequear antes de confirmar.
        [HttpPost("validate-stock")]
        [Authorize(Policy = PermissionCodes.SalesEdit)]
        public async Task<IActionResult> ValidateStock([FromBody] ValidateStockDto dto)
        {
            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (dto.Items.Any(i => i.ProductId == Guid.Empty))
                return BadRequest("Todos los items deben tener un producto válido.");

            var result = await _saleService.ValidateStockAsync(tenantId, dto.Items);

            return Ok(new StockValidationResponseDto
            {
                IsValid = result.IsValid,
                Errors = result.Errors.Select(e => new StockValidationErrorDto
                {
                    ProductId = e.ProductId,
                    ProductName = e.ProductName,
                    RequestedQuantity = e.RequestedQuantity,
                    AvailableStock = e.AvailableStock,
                }).ToList(),
            });
        }

        // POST: api/Sales/{id}/confirm
        [HttpPost("{id}/confirm")]
        [Authorize(Policy = PermissionCodes.SalesConfirm)]
        public async Task<IActionResult> Confirm(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return BadRequest("UserId missing in claim");

            try
            {
                await _saleService.ConfirmAsync(id, userId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/Sales/{id}/cancel
        [HttpPost("{id}/cancel")]
        [Authorize(Policy = PermissionCodes.SalesCancel)]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return BadRequest("UserId missing in claim");

            try
            {
                await _saleService.CancelAsync(id, userId, dto?.Reason);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/Sales/{id}/register-payment
        [HttpPost("{id}/register-payment")]
        [Authorize(Policy = PermissionCodes.SalesEdit)]
        public async Task<IActionResult> RegisterPayment(Guid id, [FromBody] RegisterPaymentDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return BadRequest("UserId missing in claim");

            var paymentMethod = (PaymentMethod)dto.PaymentMethod;

            try
            {
                await _saleService.RegisterFullPaymentAsync(id, userId, paymentMethod);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/Sales/{id}
        [HttpDelete("{id}")]
        [Authorize(Policy = PermissionCodes.SalesDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            var existing = await _saleService.GetByIdAsync(id);
            if (existing == null) return NotFound("La venta no existe.");

            if (existing.TenantId != tenantId) return Forbid();

            try
            {
                await _saleService.DeleteAsync(id, tenantId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/Sales/{id}/document/pdf
        [HttpGet("{id}/document/pdf")]
        [Authorize(Policy = PermissionCodes.SalesView)]
        public async Task<IActionResult> GetSaleDocumentPdf(Guid id)
        {
            try
            {
                var pdfBytes = await _saleDocumentService.GenerateSaleDocumentPdfAsync(id);
                return File(pdfBytes, "application/pdf", $"comprobante-venta-{id}.pdf");
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
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
