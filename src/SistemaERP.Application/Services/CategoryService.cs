using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Application.Services;

namespace SistemaERP.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ICategoryRepository categoryRepository, ILogger<CategoryService> logger)
        {
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Category>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all categories.");
            return await _categoryRepository.GetAllAsync();
        }

        public async Task<Category?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Fetching category with Id {CategoryId}.", id);
            return await _categoryRepository.GetByIdAsync(id);
        }

        public async Task<Category> CreateAsync(Category category)
        {
            // Business validation: ensure category name is unique per tenant
            var existing = await _categoryRepository.GetAllAsync();
            foreach (var c in existing)
            {
                if (c.TenantId == category.TenantId && string.Equals(c.Name, category.Name, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Attempt to create category with duplicate name {Name} for tenant {TenantId}.", category.Name, category.TenantId);
                    throw new InvalidOperationException($"Category name '{category.Name}' already exists for this tenant.");
                }
            }

            _logger.LogInformation("Creating category {CategoryName} for tenant {TenantId}.", category.Name, category.TenantId);
            return await _categoryRepository.AddAsync(category);
        }

        public async Task<Category> UpdateAsync(Category category)
        {
            // Business validation: ensure category name is unique per tenant (excluding itself)
            var existing = await _categoryRepository.GetAllAsync();
            foreach (var c in existing)
            {
                if (c.Id != category.Id && c.TenantId == category.TenantId && string.Equals(c.Name, category.Name, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Attempt to update category {CategoryId} to duplicate name {Name} for tenant {TenantId}.", category.Id, category.Name, category.TenantId);
                    throw new InvalidOperationException($"Category name '{category.Name}' already exists for this tenant.");
                }
            }

            _logger.LogInformation("Updating category {CategoryId}.", category.Id);
            return await _categoryRepository.UpdateAsync(category);
        }

        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting category {CategoryId}.", id);
            await _categoryRepository.DeleteAsync(id);
        }
    }
}
