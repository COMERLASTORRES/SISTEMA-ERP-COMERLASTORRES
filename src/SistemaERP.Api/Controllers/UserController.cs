using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaERP.Domain;
using SistemaERP.Api.Models;
using SistemaERP.Application.Services;
using SistemaERP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaERP.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/Users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/Users (lista de usuarios del tenant actual).
        [HttpGet]
        [Authorize(Policy = PermissionCodes.UsersView)]
        public async Task<IActionResult> Get()
        {
            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            var users = await _userService.GetAllByTenantAsync(tenantId);
            var result = users.Select(MapToDto);
            return Ok(result);
        }

        // GET: api/Users/{id} (con sus roles).
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var user = await _userService.GetByIdWithRolesAsync(id);
            if (user == null) return NotFound();
            return Ok(MapToDto(user));
        }

        // POST: api/Users (crear usuario dentro del tenant actual).
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            try
            {
                var created = await _userService.CreateUserAsync(tenantId, dto.Email, dto.Password, dto.FullName);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, MapToDto(created));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/Users/{id} (editar nombre y estado activo).
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] UpdateUserDto dto)
        {
            if (id != dto.Id) return BadRequest("ID mismatch");
            try
            {
                await _userService.UpdateUserAsync(id, dto.FullName, dto.IsActive);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/Users/{id}/roles (reemplaza el set completo de roles).
        [HttpPut("{id}/roles")]
        [Authorize(Policy = PermissionCodes.UsersEdit)]
        public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignUserRolesDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await _userService.AssignRolesAsync(id, dto.RoleIds.Select(Guid.Parse));
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (FormatException)
            {
                return BadRequest("Uno o más RoleIds no tienen formato Guid válido.");
            }
        }

        private static UserDto MapToDto(User user) => new()
        {
            Id = user.Id,
            TenantId = user.TenantId,
            Email = user.Email,
            FullName = user.FullName,
            IsActive = user.IsActive,
            Roles = user.UserRoles?
                .Select(ur => ur.Role)
                .Where(r => r != null)
                .Select(r => new RoleSummaryDto
                {
                    Id = r!.Id,
                    Name = r.Name,
                    IsSystemRole = r.IsSystemRole,
                })
                .ToList() ?? new List<RoleSummaryDto>(),
        };

        private Guid GetTenantId()
        {
            var claim = User.FindFirst("tenantId");
            if (claim != null && Guid.TryParse(claim.Value, out var id))
                return id;
            return Guid.Empty;
        }
    }
}
