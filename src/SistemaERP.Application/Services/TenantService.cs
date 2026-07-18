using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services
{
    public class TenantService : ITenantService
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly ILogger<TenantService> _logger;

        public TenantService(ITenantRepository tenantRepository, ILogger<TenantService> logger)
        {
            _tenantRepository = tenantRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Tenant>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all tenants.");
            return await _tenantRepository.GetAllAsync();
        }

        public async Task<Tenant?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Fetching tenant with Id {TenantId}.", id);
            return await _tenantRepository.GetByIdAsync(id);
        }

        public async Task<Tenant> CreateAsync(Tenant tenant)
        {
            if (string.IsNullOrWhiteSpace(tenant.Name))
            {
                _logger.LogWarning("Attempt to create tenant with empty name.");
                throw new InvalidOperationException("Tenant name is required.");
            }

            _logger.LogInformation("Creating tenant {TenantName}.", tenant.Name);
            return await _tenantRepository.AddAsync(tenant);
        }
    }
}
