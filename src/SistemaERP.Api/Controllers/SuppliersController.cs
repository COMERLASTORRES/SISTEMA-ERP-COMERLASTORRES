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
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierService _supplierService;
        public SuppliersController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        // GET: api/Suppliers?page=1&pageSize=10
        [HttpGet]
        [Authorize(Policy = PermissionCodes.SuppliersView)]
        public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var all = await _supplierService.GetAllAsync();
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

        // GET: api/Suppliers/{id}
        [HttpGet("{id}")]
        [Authorize(Policy = PermissionCodes.SuppliersView)]
        public async Task<IActionResult> Get(Guid id)
        {
            var supplier = await _supplierService.GetByIdAsync(id);
            if (supplier == null) return NotFound();
            return Ok(supplier);
        }

        // POST: api/Suppliers
        [HttpPost]
        [Authorize(Policy = PermissionCodes.SuppliersCreate)]
        public async Task<IActionResult> Post([FromBody] Supplier model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");
            model.TenantId = tenantId;

            try
            {
                var created = await _supplierService.CreateAsync(model);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/Suppliers/{id}
        [HttpPut("{id}")]
        [Authorize(Policy = PermissionCodes.SuppliersEdit)]
        public async Task<IActionResult> Put(Guid id, [FromBody] Supplier model)
        {
            if (id != model.Id) return BadRequest("ID mismatch");
            var existing = await _supplierService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            // Map fields
            existing.DocumentType = model.DocumentType;
            existing.DocumentNumber = model.DocumentNumber;
            existing.Name = model.Name;
            existing.Email = model.Email;
            existing.Phone = model.Phone;
            existing.Address = model.Address;
            existing.ContactPerson = model.ContactPerson;
            existing.PaymentTermDays = model.PaymentTermDays;
            existing.IsActive = model.IsActive;

            try
            {
                await _supplierService.UpdateAsync(existing);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/Suppliers/{id}
        [HttpDelete("{id}")]
        [Authorize(Policy = PermissionCodes.SuppliersDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var supplier = await _supplierService.GetByIdAsync(id);
            if (supplier == null) return NotFound();
            await _supplierService.DeleteAsync(id);
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
