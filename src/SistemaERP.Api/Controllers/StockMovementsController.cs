using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SistemaERP.Application.Services;
using SistemaERP.Domain.Entities;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockMovementsController : ControllerBase
    {
        private readonly IStockMovementService _stockMovementService;
        public StockMovementsController(IStockMovementService stockMovementService)
        {
            _stockMovementService = stockMovementService;
        }

        // GET: api/StockMovements?productId=...&page=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] Guid? productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var all = productId.HasValue
                ? await _stockMovementService.GetByProductIdAsync(productId.Value)
                : await _stockMovementService.GetAllAsync();

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

        // GET: api/StockMovements/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var movement = await _stockMovementService.GetByIdAsync(id);
            if (movement == null) return NotFound();
            return Ok(movement);
        }

        // POST: api/StockMovements
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] StockMovement model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");
            model.TenantId = tenantId;

            try
            {
                var created = await _stockMovementService.CreateAsync(model);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Stock movements are immutable history: no PUT or DELETE endpoints.

        private Guid GetTenantId()
        {
            var claim = User.FindFirst("tenantId");
            if (claim != null && Guid.TryParse(claim.Value, out var id))
                return id;

            if (HttpContext.RequestServices.GetService(typeof(IHostEnvironment)) is IHostEnvironment env && env.IsDevelopment())
            {
                return Guid.Parse("11111111-1111-1111-1111-111111111111");
            }

            return Guid.Empty;
        }
    }
}
