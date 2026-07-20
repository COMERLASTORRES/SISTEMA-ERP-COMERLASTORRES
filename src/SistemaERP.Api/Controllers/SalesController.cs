using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaERP.Application.Services;
using SistemaERP.Domain;
using SistemaERP.Domain.Entities;
using SistemaERP.Api.Models;
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
        public SalesController(ISaleService saleService)
        {
            _saleService = saleService;
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
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (dto.Items.Any(i => !i.ProductId.HasValue || i.ProductId == Guid.Empty))
                return BadRequest("Todos los items deben tener un producto válido.");

            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            var sale = MapToEntity(dto, tenantId);
            sale.CreatedBy = GetUserId();

            try
            {
                var created = await _saleService.CreateDraftAsync(sale);
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
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != dto.Id) return BadRequest("ID mismatch");

            if (dto.Items.Any(i => !i.ProductId.HasValue || i.ProductId == Guid.Empty))
                return BadRequest("Todos los items deben tener un producto válido.");

            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            var sale = MapToEntity(dto, tenantId);
            sale.Id = id;

            try
            {
                await _saleService.UpdateDraftAsync(sale);
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
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (dto.Items.Any(i => !i.ProductId.HasValue || i.ProductId == Guid.Empty))
                return BadRequest("Todos los items deben tener un producto válido.");

            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            var items = dto.Items.Select(i => new SaleItem
            {
                ProductId = i.ProductId ?? Guid.Empty,
                Quantity = i.Quantity,
            }).ToList();

            var result = await _saleService.ValidateStockAsync(tenantId, items);

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

            try
            {
                await _saleService.RegisterFullPaymentAsync(id, userId, dto.PaymentMethod);
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
            try
            {
                await _saleService.DeleteAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private Sale MapToEntity(CreateSaleDto dto, Guid tenantId)
        {
            return new Sale
            {
                TenantId = tenantId,
                CustomerId = dto.CustomerId,
                WarehouseId = dto.WarehouseId,
                VoucherType = (VoucherType)dto.VoucherType,
                VoucherNumber = dto.VoucherNumber,
                SaleDate = dto.SaleDate,
                Currency = (Currency)dto.Currency,
                ExchangeRate = dto.ExchangeRate,
                PaymentType = (PaymentType)dto.PaymentType,
                PaymentMethod = dto.PaymentMethod.HasValue ? (PaymentMethod?)dto.PaymentMethod.Value : null,
                CreditDays = dto.CreditDays,
                Observations = dto.Observations,
                Items = dto.Items.Select(i => new SaleItem
                {
                    ProductId = i.ProductId ?? Guid.Empty,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    DiscountPercentage = i.DiscountPercentage,
                    TaxPercentage = i.TaxPercentage,
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

    public class ValidateStockItemDto
    {
        public Guid? ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class ValidateStockDto
    {
        public List<ValidateStockItemDto> Items { get; set; } = new();
    }
}
