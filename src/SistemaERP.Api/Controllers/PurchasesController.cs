using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaERP.Application.Services;
using SistemaERP.Domain;
using SistemaERP.Domain.Entities;
using SistemaERP.Api.Models;
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
        public PurchasesController(IPurchaseService purchaseService)
        {
            _purchaseService = purchaseService;
        }

        // GET: api/Purchases?status=0&supplierId=...&page=1&pageSize=10
        [HttpGet]
        [Authorize(Policy = PermissionCodes.PurchasesView)]
        public async Task<IActionResult> Get(
            [FromQuery] PurchaseStatus? status,
            [FromQuery] Guid? supplierId,
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
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (dto.Items.Any(i => !i.ProductId.HasValue || i.ProductId == Guid.Empty))
                return BadRequest("Todos los items deben tener un producto válido.");

            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            var purchase = MapToEntity(dto, tenantId);
            purchase.CreatedBy = GetUserId();

            try
            {
                var created = await _purchaseService.CreateDraftAsync(purchase);
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
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != dto.Id) return BadRequest("ID mismatch");

            if (dto.Items.Any(i => !i.ProductId.HasValue || i.ProductId == Guid.Empty))
                return BadRequest("Todos los items deben tener un producto válido.");

            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            var purchase = MapToEntity(dto, tenantId);
            purchase.Id = id;

            try
            {
                await _purchaseService.UpdateDraftAsync(purchase);
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

        // DELETE: api/Purchases/{id}
        [HttpDelete("{id}")]
        [Authorize(Policy = PermissionCodes.PurchasesDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _purchaseService.DeleteAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private Purchase MapToEntity(CreatePurchaseDto dto, Guid tenantId)
        {
            return new Purchase
            {
                TenantId = tenantId,
                SupplierId = dto.SupplierId,
                WarehouseId = dto.WarehouseId,
                VoucherType = (VoucherType)dto.VoucherType,
                VoucherNumber = dto.VoucherNumber,
                PurchaseDate = dto.PurchaseDate,
                Currency = (Currency)dto.Currency,
                ExchangeRate = dto.ExchangeRate,
                PaymentType = (PaymentType)dto.PaymentType,
                PaymentMethod = dto.PaymentMethod.HasValue ? (PaymentMethod)dto.PaymentMethod.Value : null,
                CreditDays = dto.CreditDays,
                Observations = dto.Observations,
                Items = dto.Items.Select(i => new PurchaseItem
                {
                    ProductId = i.ProductId ?? Guid.Empty,
                    Quantity = i.Quantity,
                    UnitCost = i.UnitCost,
                    DiscountPercentage = i.DiscountPercentage,
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
