using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SistemaERP.Application.Repositories;
using SistemaERP.Application.Services;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services
{
    public class UserService : IUserService
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;

        public UserService(ITenantRepository tenantRepository, IUserRepository userRepository, ILogger<UserService> logger)
        {
            _tenantRepository = tenantRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<User> RegisterAsync(string tenantName, string email, string password, string fullName)
        {
            if (string.IsNullOrWhiteSpace(tenantName))
            {
                _logger.LogWarning("Attempt to register with empty tenant name.");
                throw new InvalidOperationException("Tenant name is required.");
            }

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Attempt to register with empty email or password.");
                throw new InvalidOperationException("Email and password are required.");
            }

            var existing = await _userRepository.GetByEmailAsync(email);
            if (existing != null)
            {
                _logger.LogWarning("Attempt to register with already-used email {Email}.", email);
                throw new InvalidOperationException("A user with this email already exists.");
            }

            var tenant = await _tenantRepository.AddAsync(new Tenant { Name = tenantName });

            var user = new User
            {
                TenantId = tenant.Id,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = fullName,
                Role = "Admin",
                IsActive = true
            };

            _logger.LogInformation("Registering admin user {Email} for tenant {TenantId}.", email, tenant.Id);
            return await _userRepository.AddAsync(user);
        }

        public async Task<User?> ValidateCredentialsAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Login failed for email {Email}: user not found or inactive.", email);
                return null;
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed for email {Email}: invalid password.", email);
                return null;
            }

            return user;
        }
    }
}
