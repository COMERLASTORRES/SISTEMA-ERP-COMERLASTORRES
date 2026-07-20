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
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        // GET: api/Customers?page=1&pageSize=10
        [HttpGet]
        [Authorize(Policy = PermissionCodes.CustomersView)]
        public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var all = await _customerService.GetAllAsync();
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

        // GET: api/Customers/{id}
        [HttpGet("{id}")]
        [Authorize(Policy = PermissionCodes.CustomersView)]
        public async Task<IActionResult> Get(Guid id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null) return NotFound();
            return Ok(customer);
        }

        // POST: api/Customers
        [HttpPost]
        [Authorize(Policy = PermissionCodes.CustomersCreate)]
        public async Task<IActionResult> Post([FromBody] Customer model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");
            model.TenantId = tenantId;

            try
            {
                var created = await _customerService.CreateAsync(model);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/Customers/{id}
        [HttpPut("{id}")]
        [Authorize(Policy = PermissionCodes.CustomersEdit)]
        public async Task<IActionResult> Put(Guid id, [FromBody] Customer model)
        {
            if (id != model.Id) return BadRequest("ID mismatch");
            var existing = await _customerService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            // Map fields
            existing.DocumentType = model.DocumentType;
            existing.DocumentNumber = model.DocumentNumber;
            existing.Name = model.Name;
            existing.Email = model.Email;
            existing.Phone = model.Phone;
            existing.Address = model.Address;
            existing.CustomerType = model.CustomerType;
            existing.CreditLimit = model.CreditLimit;
            existing.IsActive = model.IsActive;

            try
            {
                await _customerService.UpdateAsync(existing);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/Customers/{id}
        [HttpDelete("{id}")]
        [Authorize(Policy = PermissionCodes.CustomersDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null) return NotFound();
            await _customerService.DeleteAsync(id);
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
