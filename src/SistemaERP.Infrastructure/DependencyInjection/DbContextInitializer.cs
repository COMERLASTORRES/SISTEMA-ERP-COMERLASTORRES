using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaERP.Application.Services;
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

        // Sincroniza el rol de sistema "Admin" de cada tenant con los permisos nuevos
        // del catálogo, sin eliminar los existentes. Garantiza que un administrador
        // nunca pierda acceso al añadirse un módulo/acción (ej. users.*, roles.*).
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        await userService.SyncSystemAdminRolesAsync();
    }
}