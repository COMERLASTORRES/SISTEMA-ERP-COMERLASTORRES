using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SistemaERP.Api.Models;
using SistemaERP.Application.Services;
using SistemaERP.Domain.Entities;
using System;
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

            var token = GenerateToken(user, out var expiration);
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

        private string GenerateToken(User user, out DateTime expiration)
        {
            var jwt = _configuration.GetSection("Jwt");
            var secret = jwt["Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured.");
            var issuer = jwt["Issuer"];
            var audience = jwt["Audience"];
            var expiryHours = double.TryParse(jwt["ExpiryHours"], out var h) ? h : 8;

            expiration = DateTime.UtcNow.AddHours(expiryHours);

            var claims = new[]
            {
                new Claim("tenantId", user.TenantId.ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("role", user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiration,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
