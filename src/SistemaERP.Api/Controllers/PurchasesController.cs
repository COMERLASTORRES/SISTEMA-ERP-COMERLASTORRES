using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaERP.Application.DTOs;
using SistemaERP.Application.Services;
using SistemaERP.Domain;
using SistemaERP.Domain.Entities;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PurchasesController : ControllerBase
    {
        private readonly IPurchaseService _purchaseService;
        // TEMP: IPurchaseDocumentService not yet implemented
        // private readonly IPurchaseDocumentService _purchaseDocumentService;
        public PurchasesController(IPurchaseService purchaseService)
        {
            _purchaseService = purchaseService;
        }

        // GET: api/Purchases?status=0&supplierId=...&paymentType=1&page=1&pageSize=10
        [HttpGet]
        [Authorize(Policy = PermissionCodes.PurchasesView)]
        public async Task<IActionResult> Get(
            [FromQuery] PurchaseStatus? status,
            [FromQuery] Guid? supplierId,
            [FromQuery] PaymentType? paymentType,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var all = (await _purchaseService.GetAllAsync()).AsEnumerable();

            if (status.HasValue)
                all = all.Where(p => p.Status == status.Value);
            if (supplierId.HasValue)
                all = all.Where(p => p.SupplierId == supplierId.Value);
            if (paymentType.HasValue)
                all = all.Where(p => p.PaymentType == paymentType.Value);

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

        // GET: api/Purchases/{id}
        [HttpGet("{id}")]
        [Authorize(Policy = PermissionCodes.PurchasesView)]
        public async Task<IActionResult> Get(Guid id)
        {
            var purchase = await _purchaseService.GetByIdAsync(id);
            if (purchase == null) return NotFound();
            return Ok(purchase);
        }

        // POST: api/Purchases (crea en Draft)
        [HttpPost]
        [Authorize(Policy = PermissionCodes.PurchasesCreate)]
        public async Task<IActionResult> Post([FromBody] CreatePurchaseDto dto)
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
                var created = await _purchaseService.CreateDraftAsync(dto, tenantId, userId);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/Purchases/{id}
        [HttpPut("{id}")]
        [Authorize(Policy = PermissionCodes.PurchasesEdit)]
        public async Task<IActionResult> Put(Guid id, [FromBody] UpdatePurchaseDto dto)
        {
            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            var existing = await _purchaseService.GetByIdAsync(id);
            if (existing == null) return NotFound("La compra no existe.");

            if (existing.TenantId != tenantId) return Forbid();

            if (id != dto.Id) return BadRequest("ID mismatch");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (dto.Items.Any(i => i.ProductId == Guid.Empty))
                return BadRequest("Todos los items deben tener un producto válido.");

            try
            {
                await _purchaseService.UpdateDraftAsync(dto, tenantId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/Purchases/{id}/confirm
        [HttpPost("{id}/confirm")]
        [Authorize(Policy = PermissionCodes.PurchasesConfirm)]
        public async Task<IActionResult> Confirm(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return BadRequest("UserId missing in claim");

            try
            {
                await _purchaseService.ConfirmAsync(id, userId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/Purchases/{id}/cancel
        [HttpPost("{id}/cancel")]
        [Authorize(Policy = PermissionCodes.PurchasesCancel)]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return BadRequest("UserId missing in claim");

            try
            {
                await _purchaseService.CancelAsync(id, userId, dto?.Reason);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/Purchases/{id}/register-payment
        [HttpPost("{id}/register-payment")]
        [Authorize(Policy = PermissionCodes.PurchasesRegisterPayment)]
        public async Task<IActionResult> RegisterPayment(Guid id, [FromBody] RegisterPaymentDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return BadRequest("UserId missing in claim");

            var paymentMethod = (PaymentMethod)dto.PaymentMethod;

            try
            {
                await _purchaseService.RegisterFullPaymentAsync(id, userId, paymentMethod);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/Purchases/{id}
        [HttpDelete("{id}")]
        [Authorize(Policy = PermissionCodes.PurchasesDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            var existing = await _purchaseService.GetByIdAsync(id);
            if (existing == null) return NotFound("La compra no existe.");

            if (existing.TenantId != tenantId) return Forbid();

            try
            {
                await _purchaseService.DeleteAsync(id, tenantId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // TEMP: GetPurchaseDocumentPdf not yet implemented
        // [HttpGet("{id}/document/pdf")]
        // [Authorize(Policy = PermissionCodes.PurchasesView)]
        // public async Task<IActionResult> GetPurchaseDocumentPdf(Guid id)
        // {
        //     try
        //     {
        //         var pdfBytes = await _purchaseDocumentService.GeneratePurchaseDocumentPdfAsync(id);
        //         return File(pdfBytes, "application/pdf", $"comprobante-compra-{id}.pdf");
        //     }
        //     catch (InvalidOperationException ex)
        //     {
        //         return NotFound(ex.Message);
        //     }
        // }

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