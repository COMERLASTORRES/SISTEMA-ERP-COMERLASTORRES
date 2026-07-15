using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaERP.Infrastructure.Contexts;
using SistemaERP.Infrastructure.Repositories;
using SistemaERP.Application.Services;
using SistemaERP.Application.Repositories;

namespace SistemaERP.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
    {
        services.AddHttpContextAccessor(); // needed for ITenantProvider
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddDbContext<SistemaERPDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        // Apply migrations on build if needed
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductService, ProductService>();
        services.AddDbContextInitializer();

        return services;
    }
}
