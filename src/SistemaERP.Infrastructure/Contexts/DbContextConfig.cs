using Microsoft.EntityFrameworkCore;
using SistemaERP.Infrastructure.Contexts;

namespace SistemaERP.Infrastructure.Contexts;

public static class DbContextConfig
{
    public static DbContextOptions<SistemaERPDbContext> CreateOptions(string connectionString)
    {
        return new DbContextOptionsBuilder<SistemaERPDbContext>()
            .UseNpgsql(connectionString)
            .EnableSensitiveDataLogging()
            .Options;
    }
}