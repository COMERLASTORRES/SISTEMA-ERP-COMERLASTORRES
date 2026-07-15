using Microsoft.EntityFrameworkCore;
using SistemaERP.Domain.Entities;
using SistemaERP.Infrastructure.DependencyInjection;

namespace SistemaERP.Infrastructure.Contexts;

public class SistemaERPDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public SistemaERPDbContext(DbContextOptions<SistemaERPDbContext> options, ITenantProvider tenantProvider) : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Product> Products { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply global query filter for multi‑tenant isolation and soft‑delete
        modelBuilder.Entity<Product>().HasQueryFilter(p => p.IsActive && p.TenantId == _tenantProvider.GetTenantId());

        // Configure indexes for performance (Tenant + Code is unique per tenant)
        modelBuilder.Entity<Product>()
            .HasIndex(p => new { p.TenantId, p.Code })
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }

    // Override SaveChanges to set audit fields for all AuditableEntity instances
    public override int SaveChanges()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is AuditableEntity &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (AuditableEntity)entry.Entity;
            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChanges();
    }
}