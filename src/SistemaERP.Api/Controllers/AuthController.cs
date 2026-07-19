using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SistemaERP.Api.Models;
using SistemaERP.Application.Services;
using SistemaERP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SistemaERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        public AuthController(IUserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
        }

        // POST: api/auth/register
        // Creates the Tenant and its initial admin User in one operation.
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var user = await _userService.RegisterAsync(model.TenantName, model.Email, model.Password, model.FullName);
                return Created(string.Empty, user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/auth/login
        // Validates credentials and returns a JWT valid for 8 hours.
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userService.ValidateCredentialsAsync(model.Email, model.Password);
            if (user == null) return Unauthorized("Invalid email or password.");

            var (token, expiration) = await GenerateTokenAsync(user);
            return Ok(new LoginResponse
            {
                Token = token,
                Expiration = expiration,
                UserId = user.Id.ToString(),
                Email = user.Email,
                Role = user.Role,
                TenantId = user.TenantId.ToString()
            });
        }

        // GET: api/auth/my-permissions
        // Devuelve los permisos efectivos del usuario autenticado (unión de los permisos de
        // sus roles). Útil para que el frontend decida qué mostrar/ocultar en la UI.
        [HttpGet("my-permissions")]
        public async Task<IActionResult> MyPermissions()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return BadRequest("UserId missing in claim");

            var permissions = await _userService.GetUserPermissionsAsync(userId);
            return Ok(permissions.Select(p => new
            {
                p.Id,
                p.Code,
                p.Module,
                p.Description,
            }));
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst("userId");
            if (claim != null && Guid.TryParse(claim.Value, out var id))
                return id;
            return Guid.Empty;
        }

        private async Task<(string Token, DateTime Expiration)> GenerateTokenAsync(User user)
        {
            var jwt = _configuration.GetSection("Jwt");
            var secret = jwt["Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured.");
            var issuer = jwt["Issuer"];
            var audience = jwt["Audience"];
            var expiryHours = double.TryParse(jwt["ExpiryHours"], out var h) ? h : 8;

            var expiration = DateTime.UtcNow.AddHours(expiryHours);

            // Claims base (compatibilidad): tenant, userId, email y role simple.
            var claims = new List<Claim>
            {
                new Claim("tenantId", user.TenantId.ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("role", user.Role),
            };

            // Un claim "permission" por cada permiso efectivo del usuario, para que ASP.NET
            // Core valide políticas de permiso sin consultar la base de datos en cada request.
            var permissions = await _userService.GetUserPermissionsAsync(user.Id);
            foreach (var permission in permissions)
            {
                claims.Add(new Claim(SistemaERP.Api.Authorization.PermissionPolicyProvider.PermissionClaimType, permission.Code));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiration,
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiration);
        }
    }
}
