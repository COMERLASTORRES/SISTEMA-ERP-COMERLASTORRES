using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaERP.Infrastructure.Contexts;

namespace SistemaERP.Infrastructure.DependencyInjection;

public static class DbContextInitializer
{
    public static IServiceCollection AddDbContextInitializer(this IServiceCollection services)
    {
        return services.AddTransient<DbInitializer>();
    }
}

public class DbInitializer
{
    private readonly IServiceProvider _serviceProvider;

    public DbInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SistemaERPDbContext>();

        // Apply pending migrations
        await context.Database.MigrateAsync();

        // Seed del catálogo base de permisos (idempotente por Code).
        await PermissionSeed.SeedAsync(context);
    }
}