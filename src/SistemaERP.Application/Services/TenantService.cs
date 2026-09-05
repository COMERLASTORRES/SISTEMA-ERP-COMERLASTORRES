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
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly ISaleRepository _saleRepository;
        private readonly ILogger<TenantService> _logger;

        public TenantService(
            ITenantRepository tenantRepository,
            IPurchaseRepository purchaseRepository,
            ISaleRepository saleRepository,
            ILogger<TenantService> logger)
        {
            _tenantRepository = tenantRepository;
            _purchaseRepository = purchaseRepository;
            _saleRepository = saleRepository;
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

        // Genera el siguiente correlativo de compra para el tenant: "PUR-000001", "PUR-000002", ...
        public async Task<string?> GenerateNextPurchaseNumberAsync(Guid tenantId)
        {
            var last = await _purchaseRepository.GetLastPurchaseNumberAsync(tenantId);
            return IncrementCorrelative(last, "PUR");
        }

        // Genera el siguiente correlativo de venta para el tenant: "VEN-000001", "VEN-000002", ...
        public async Task<string?> GenerateNextSaleNumberAsync(Guid tenantId)
        {
            var last = await _saleRepository.GetLastSaleNumberAsync(tenantId);
            return IncrementCorrelative(last, "VEN");
        }

        // Suma 1 al sufijo numérico del último correlativo del prefijo dado, o empieza en 1.
        private static string IncrementCorrelative(string? last, string prefix)
        {
            const int width = 6;
            var next = 1;
            if (!string.IsNullOrEmpty(last) && last.StartsWith($"{prefix}-"))
            {
                var numericPart = last.Substring(prefix.Length + 1);
                if (int.TryParse(numericPart, out var n))
                    next = n + 1;
            }
            return $"{prefix}-{next.ToString().PadLeft(width, '0')}";
        }
    }
}
