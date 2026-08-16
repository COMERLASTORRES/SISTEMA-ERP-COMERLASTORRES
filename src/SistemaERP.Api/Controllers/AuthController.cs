using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SistemaERP.Api.Models;
using SistemaERP.Application.Repositories;
using SistemaERP.Application.Services;
using SistemaERP.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.IO;

namespace SistemaERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserRepository _userRepository;
        private readonly IAuditService _auditService;
        private readonly IConfiguration _configuration;

        public AuthController(IUserService userService, IUserRepository userRepository, IAuditService auditService, IConfiguration configuration)
        {
            _userService = userService;
            _userRepository = userRepository;
            _auditService = auditService;
            _configuration = configuration;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            Request.EnableBuffering();
            var rawBody = await new StreamReader(Request.Body).ReadToEndAsync();
            Request.Body.Position = 0;

            Console.WriteLine("ContentType: " + Request.ContentType);
            Console.WriteLine("ContentLength: " + Request.ContentLength);
            Console.WriteLine("RawBody:\n" + rawBody);

            foreach (var kvp in ModelState)
            {
                foreach (var error in kvp.Value.Errors)
                {
                    Console.WriteLine($"ModelState Error - Field: {kvp.Key}");
                    Console.WriteLine($"Message: {error.ErrorMessage}");
                    Console.WriteLine($"Exception: {error.Exception?.Message ?? "None"}");
                    if (error.Exception != null)
                        Console.WriteLine($"Stack: {error.Exception.StackTrace}");
                }
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    RawBody = rawBody,
                    ContentType = Request.ContentType,
                    ContentLength = Request.ContentLength,
                    ModelState = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => new
                        {
                            Field = kvp.Key,
                            Message = e.ErrorMessage,
                            Exception = e.Exception?.Message
                        }).ToArray()
                    ),
                    Errors = ModelState.SelectMany(kvp => kvp.Value.Errors.Select(e => new
                    {
                        Field = kvp.Key,
                        Error = e.ErrorMessage,
                        Exception = e.Exception?.Message
                    }))
                });
            }

            try
            {
                var user = await _userService.RegisterAsync(model.TenantName, model.Email, model.Password, model.FullName);
                return Created("", new { user.Id, user.Email, user.FullName, user.TenantId });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/auth/login
        [HttpPost("login")]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userService.ValidateCredentialsAsync(model.Email, model.Password);
            if (user == null)
            {
                // Log failed login attempt
                var clientIpLoginFailed = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                await _auditService.LogAsync(
                    Guid.Empty, // No tenant context yet
                    "LoginFailed",
                    "User",
                    null,
                    new { email = model.Email },
                    ipAddress: clientIpLoginFailed,
                    userId: null);

                return Unauthorized("Invalid email or password.");
            }

            var (token, expiration) = await GenerateTokenAsync(user);
            var refreshTokenResult = await _userService.IssueRefreshTokenAsync(user.Id, GetRefreshTokenExpiryDays());
            var tenant = await _userService.GetTenantAsync(user.TenantId);

            // Log successful login
            var clientIpLoginSuccess = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            await _auditService.LogAsync(
                user.TenantId,
                "LoginSuccess",
                "User",
                user.Id,
                new { user.Email, user.Role },
                ipAddress: clientIpLoginSuccess,
                userId: user.Id);

            return Ok(new LoginResponse
            {
                Token = token,
                Expiration = expiration,
                RefreshToken = refreshTokenResult.RefreshToken,
                RefreshTokenExpiration = refreshTokenResult.ExpiresAt,
                UserId = user.Id.ToString(),
                Email = user.Email,
                Role = user.Role,
                TenantId = user.TenantId.ToString(),
                TenantName = tenant?.Name ?? string.Empty
            });
        }

        // POST: api/auth/refresh
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (string.IsNullOrWhiteSpace(model.RefreshToken)) return BadRequest("Refresh token is required.");

            var refreshResult = await _userService.RotateRefreshTokenAsync(model.RefreshToken, GetRefreshTokenExpiryDays());
            if (refreshResult == null)
            {
                var clientIpRefreshFailed = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                await _auditService.LogAsync(
                    Guid.Empty,
                    "RefreshTokenFailed",
                    "User",
                    null,
                    new { reason = "Invalid or expired token" },
                    ipAddress: clientIpRefreshFailed,
                    userId: null);
                return Unauthorized("Invalid or expired refresh token.");
            }

            // Obtener el usuario desde el refresh token rotado
            var user = await _userRepository.GetByIdWithRolesAsync(refreshResult.UserId);
            if (user == null) return Unauthorized("User not found.");

            var (token, expiration) = await GenerateTokenAsync(user);

            // Log successful token refresh
            var clientIpRefreshSuccess = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            await _auditService.LogAsync(
                user.TenantId,
                "RefreshTokenRotated",
                "User",
                user.Id,
                new { user.Email },
                ipAddress: clientIpRefreshSuccess,
                userId: user.Id);

            return Ok(new RefreshResponse
            {
                Token = token,
                Expiration = expiration,
                RefreshToken = refreshResult.RefreshToken,
                RefreshTokenExpiration = refreshResult.ExpiresAt
            });
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.RefreshToken))
            {
                return Ok(new { message = "Logout successful (no refresh token provided)." });
            }

            // Try to get userId from the refresh token before revoking
            var clientIpLogout = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            await _userService.RevokeRefreshTokenAsync(model.RefreshToken);

            // Log logout
            await _auditService.LogAsync(
                Guid.Empty, // We don't have tenant context from just the refresh token here
                "Logout",
                "User",
                null,
                new { },
                ipAddress: clientIpLogout,
                userId: null);

            return Ok(new { message = "Logout successful. Refresh token revoked." });
        }

        // POST: api/auth/forgot-password
        [HttpPost("forgot-password")]
        [EnableRateLimiting("ForgotPasswordPolicy")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (string.IsNullOrWhiteSpace(model.Email)) return BadRequest("Email es requerido.");

            var result = await _userService.RequestPasswordResetAsync(model.Email);

            // Log forgot password request (same response for existing/non-existing emails - no enumeration)
            var clientIpForgot = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            await _auditService.LogAsync(
                Guid.Empty, // No tenant context for this public endpoint
                "PasswordResetRequested",
                "User",
                null,
                new { email = model.Email },
                ipAddress: clientIpForgot,
                userId: null);

            // Siempre respondemos 200 OK con el mismo formato (no revela si el email existe)
            // En DEV: incluye token y link para testing
            // En PROD: solo Success=true y mensaje genérico
            var isDevelopment = _configuration["ASPNETCORE_ENVIRONMENT"] == "Development";

            var response = new ForgotPasswordResponse
            {
                Success = true,
                Message = "Si el email existe, recibirá instrucciones para restablecer su contraseña."
            };

            if (isDevelopment && result.ResetToken != null)
            {
                response.ResetToken = result.ResetToken;
                response.ResetLink = result.ResetLink;
                response.ExpiresAt = result.ExpiresAt;
            }

            return Ok(response);
        }

        // POST: api/auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (string.IsNullOrWhiteSpace(model.Token)) return BadRequest("Token es requerido.");
            if (string.IsNullOrWhiteSpace(model.NewPassword)) return BadRequest("Nueva contraseña es requerida.");

            var result = await _userService.ResetPasswordAsync(model.Token, model.NewPassword);

            if (!result.Success)
            {
                var clientIpResetFailed = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                await _auditService.LogAsync(
                    Guid.Empty,
                    "PasswordResetFailed",
                    "User",
                    null,
                    new { reason = result.ErrorMessage },
                    ipAddress: clientIpResetFailed,
                    userId: null);

                return BadRequest(new ResetPasswordResponse
                {
                    Success = false,
                    Message = result.ErrorMessage
                });
            }

            // Log successful password reset
            var clientIpResetSuccess = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            await _auditService.LogAsync(
                Guid.Empty, // The service logs with proper tenantId internally
                "PasswordResetCompleted",
                "User",
                null,
                new { },
                ipAddress: clientIpResetSuccess,
                userId: null);

            return Ok(new ResetPasswordResponse
            {
                Success = true,
                Message = "Contraseña actualizada correctamente. Por favor, inicie sesión con su nueva contraseña."
            });
        }

        // GET: api/auth/my-permissions
        [HttpGet("my-permissions")]
        public async Task<IActionResult> MyPermissions()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return BadRequest("UserId missing in claim");

            var permissions = await _userService.GetUserPermissionsAsync(userId);
            return Ok(permissions.Select(p => new {
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

        private int GetRefreshTokenExpiryDays()
        {
            var jwt = _configuration.GetSection("Jwt");
            return int.TryParse(jwt["RefreshTokenExpiryDays"], out var days) ? days : 7;
        }

        private async Task<(string Token, DateTime Expiration)> GenerateTokenAsync(User user)
        {
            var jwt = _configuration.GetSection("Jwt");
            var secret = jwt["Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured.");
            var issuer = jwt["Issuer"];
            var audience = jwt["Audience"];
            var expiryHours = double.TryParse(jwt["ExpiryHours"], out var h) ? h : 1;

            var expiration = DateTime.UtcNow.AddHours(expiryHours);

            var tenant = await _userService.GetTenantAsync(user.TenantId);
            var tenantName = tenant?.Name ?? string.Empty;

            var claims = new List<Claim>
            {
                new Claim("tenantId", user.TenantId.ToString()),
                new Claim("tenantName", tenantName),
                new Claim("userId", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("role", user.Role),
            };

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