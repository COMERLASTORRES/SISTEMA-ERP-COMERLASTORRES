using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaERP.Application.Reports;
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
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IPurchaseDocumentService, PurchaseDocumentService>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<ISaleDocumentService, SaleDocumentService>();
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
        services.AddScoped<SistemaERP.Application.Reports.Purchases.IPurchaseReportRepository, SistemaERP.Infrastructure.Repositories.PurchaseReportRepository>();
        services.AddScoped<SistemaERP.Application.Reports.Purchases.IPurchaseReportService, SistemaERP.Application.Reports.Purchases.PurchaseReportService>();
        services.AddScoped<SistemaERP.Application.Reports.Inventory.IInventoryReportRepository, SistemaERP.Infrastructure.Repositories.InventoryReportRepository>();
        services.AddScoped<SistemaERP.Application.Reports.Inventory.IInventoryReportService, SistemaERP.Application.Reports.Inventory.InventoryReportService>();
        services.AddScoped<SistemaERP.Application.Reports.Cash.ICashReportRepository, SistemaERP.Infrastructure.Repositories.CashReportRepository>();
        services.AddScoped<SistemaERP.Application.Reports.Cash.ICashReportService, SistemaERP.Application.Reports.Cash.CashReportService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();

        services.AddDbContextInitializer();

        return services;
    }
}
