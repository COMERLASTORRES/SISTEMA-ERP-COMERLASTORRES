using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;
        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        // GET: api/Roles (lista de roles del tenant, con conteo de permisos).
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            var roles = await _roleService.GetAllByTenantAsync(tenantId);
            var result = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                TenantId = r.TenantId,
                Name = r.Name,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole,
                PermissionCount = r.RolePermissions?.Count ?? 0,
            });

            return Ok(result);
        }

        // GET: api/Roles/{id} (detalle con permisos incluidos).
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var role = await _roleService.GetByIdAsync(id);
            if (role == null) return NotFound();

            var result = new RoleDetailDto
            {
                Id = role.Id,
                TenantId = role.TenantId,
                Name = role.Name,
                Description = role.Description,
                IsSystemRole = role.IsSystemRole,
                Permissions = (role.RolePermissions ?? Enumerable.Empty<RolePermission>())
                    .Select(rp => rp.Permission)
                    .Where(p => p != null)
                    .Select(p => new PermissionDto
                    {
                        Id = p!.Id,
                        Code = p.Code,
                        Module = p.Module,
                        Description = p.Description,
                    })
                    .ToList(),
            };

            return Ok(result);
        }

        // POST: api/Roles (crear).
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateRoleDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var tenantId = GetTenantId();
            if (tenantId == Guid.Empty) return BadRequest("TenantId missing in claim");

            try
            {
                var created = await _roleService.CreateAsync(tenantId, dto.Name, dto.Description);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, MapToDto(created));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/Roles/{id} (editar). Bloquea la edición completa de roles de sistema:
        // el service ya lanza si se intenta cambiar el nombre, así que delegamos la regla.
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] UpdateRoleDto dto)
        {
            if (id != dto.Id) return BadRequest("ID mismatch");
            try
            {
                await _roleService.UpdateAsync(id, dto.Name, dto.Description);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/Roles/{id} (validación en RoleService: no si es sistema ni tiene usuarios).
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _roleService.DeleteAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/Roles/{id}/permissions (reemplaza el set completo de permisos).
        [HttpPost("{id}/permissions")]
        public async Task<IActionResult> AssignPermissions(Guid id, [FromBody] AssignRolePermissionsDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await _roleService.AssignPermissionsAsync(id, dto.PermissionIds.Select(Guid.Parse));
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (FormatException)
            {
                return BadRequest("Uno o más PermissionIds no tienen formato Guid válido.");
            }
        }

        private static RoleDto MapToDto(Role role) => new()
        {
            Id = role.Id,
            TenantId = role.TenantId,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            PermissionCount = role.RolePermissions?.Count ?? 0,
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
