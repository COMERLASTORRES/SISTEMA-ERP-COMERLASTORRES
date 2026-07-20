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
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IStockMovementService, StockMovementService>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<ICashRegisterRepository, CashRegisterRepository>();
        services.AddScoped<ICashRegisterService, CashRegisterService>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Reports (servicios especializados por dominio; fachada en ReportsController).
        services.AddScoped<SistemaERP.Application.Reports.Sales.ISalesReportRepository, SistemaERP.Infrastructure.Repositories.SalesReportRepository>();
        services.AddScoped<SistemaERP.Application.Reports.Sales.ISalesReportService, SistemaERP.Application.Reports.Sales.SalesReportService>();

        services.AddDbContextInitializer();

        return services;
    }
}
