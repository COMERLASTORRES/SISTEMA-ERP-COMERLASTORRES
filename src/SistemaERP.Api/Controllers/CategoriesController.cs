using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: api/Categories?page=1&pageSize=10
        [HttpGet]
        [Authorize(Policy = PermissionCodes.CategoriesView)]
        public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var all = await _categoryService.GetAllAsync();
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

        // GET: api/Categories/{id}
        [HttpGet("{id}")]
        [Authorize(Policy = PermissionCodes.CategoriesView)]
        public async Task<IActionResult> Get(Guid id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null) return NotFound();
            return Ok(category);
        }

        // POST: api/Categories
        [HttpPost]
        [Authorize(Policy = PermissionCodes.CategoriesCreate)]
        public async Task<IActionResult> Post([FromBody] Category model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");
            model.TenantId = tenantId;

            try
            {
                var created = await _categoryService.CreateAsync(model);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/Categories/{id}
        [HttpPut("{id}")]
        [Authorize(Policy = PermissionCodes.CategoriesEdit)]
        public async Task<IActionResult> Put(Guid id, [FromBody] Category model)
        {
            if (id != model.Id) return BadRequest("ID mismatch");
            var existing = await _categoryService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            // Map fields
            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.IsActive = model.IsActive;

            try
            {
                await _categoryService.UpdateAsync(existing);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/Categories/{id}
        [HttpDelete("{id}")]
        [Authorize(Policy = PermissionCodes.CategoriesDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null) return NotFound();
            await _categoryService.DeleteAsync(id);
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
