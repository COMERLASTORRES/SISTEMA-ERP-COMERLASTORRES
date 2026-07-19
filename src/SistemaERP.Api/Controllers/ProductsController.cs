using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
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
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // GET: api/Products?page=1&pageSize=10
        [HttpGet]
        [Authorize(Policy = PermissionCodes.ProductsView)]
        public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var all = await _productService.GetAllAsync();
            var total = all.Count;
            var items = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = items
            };

            return Ok(result);
        }

        // GET: api/Products/{id}
        [HttpGet("{id}")]
        [Authorize(Policy = PermissionCodes.ProductsView)]
        public async Task<IActionResult> Get(Guid id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        // POST: api/Products
        [HttpPost]
        [Authorize(Policy = PermissionCodes.ProductsCreate)]
        public async Task<IActionResult> Post([FromBody] Product model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");
            model.TenantId = tenantId;

            try
            {
                var created = await _productService.CreateAsync(model);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/Products/{id}
        [HttpPut("{id}")]
        [Authorize(Policy = PermissionCodes.ProductsEdit)]
        public async Task<IActionResult> Put(Guid id, [FromBody] Product model)
        {
            if (id != model.Id) return BadRequest("ID mismatch");
            var existing = await _productService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            // Map fields
            existing.Code = model.Code;
            existing.Name = model.Name;
            existing.Barcode = model.Barcode;
            existing.PurchasePrice = model.PurchasePrice;
            existing.SalePrice = model.SalePrice;
            existing.Stock = model.Stock;
            existing.StockMinimum = model.StockMinimum;
            existing.IsActive = model.IsActive;

            try
            {
                await _productService.UpdateAsync(existing);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/Products/{id}
        [HttpDelete("{id}")]
        [Authorize(Policy = PermissionCodes.ProductsDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null) return NotFound();
            await _productService.DeleteAsync(id);
            return NoContent();
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