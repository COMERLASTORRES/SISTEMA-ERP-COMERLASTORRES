using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaERP.Application.DTOs;
using SistemaERP.Application.Services;
using SistemaERP.Domain;
using System;
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
        public async Task<IActionResult> Post([FromBody] CreateProductDto dto)
        {
            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var created = await _productService.CreateAsync(dto, tenantId);
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
        public async Task<IActionResult> Put(Guid id, [FromBody] UpdateProductDto dto)
        {
            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            var existing = await _productService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            if (id != dto.Id) return BadRequest("ID mismatch");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _productService.UpdateAsync(dto);
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