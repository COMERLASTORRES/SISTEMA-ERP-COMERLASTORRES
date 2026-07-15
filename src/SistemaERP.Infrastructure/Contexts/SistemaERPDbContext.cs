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
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<StockMovement> StockMovements { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply global query filter for multi‑tenant isolation and soft‑delete
        modelBuilder.Entity<Product>().HasQueryFilter(p => p.IsActive && p.TenantId == _tenantProvider.GetTenantId());

        // Configure indexes for performance (Tenant + Code is unique per tenant)
        modelBuilder.Entity<Product>()
            .HasIndex(p => new { p.TenantId, p.Code })
            .IsUnique();

        // Category multi‑tenant query filter
        modelBuilder.Entity<Category>().HasQueryFilter(c => c.IsActive && c.TenantId == _tenantProvider.GetTenantId());

        // Category name is unique per tenant
        modelBuilder.Entity<Category>()
            .HasIndex(c => new { c.TenantId, c.Name })
            .IsUnique();

        // Optional Product -> Category relationship
        modelBuilder.Entity<Product>()
            .HasOne<Category>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // StockMovement multi-tenant query filter (immutable history, no soft-delete)
        modelBuilder.Entity<StockMovement>().HasQueryFilter(m => m.TenantId == _tenantProvider.GetTenantId());

        // Composite index for tenant + product lookups
        modelBuilder.Entity<StockMovement>()
            .HasIndex(m => new { m.TenantId, m.ProductId });

        // StockMovement references Product; restrict deletion to preserve history
        modelBuilder.Entity<StockMovement>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

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