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
    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<Purchase> Purchases { get; set; } = null!;
    public DbSet<PurchaseItem> PurchaseItems { get; set; } = null!;

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

        // Tenant is the root entity (the business itself): it is NOT tenant-scoped,
        // so it has no multi-tenant query filter and is queryable globally (e.g. during login).
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Name);

        // User is tenant-scoped (each user belongs to a tenant).
        modelBuilder.Entity<User>().HasQueryFilter(u => u.IsActive && u.TenantId == _tenantProvider.GetTenantId());

        // Email must be unique GLOBALLY (login is by email before the tenant is known),
        // so the index is on Email alone, not composed with TenantId.
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // User -> Tenant relationship.
        modelBuilder.Entity<User>()
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Customer multi-tenant query filter (soft-delete via IsActive)
        modelBuilder.Entity<Customer>().HasQueryFilter(c => c.IsActive && c.TenantId == _tenantProvider.GetTenantId());

        // IsActive defaults to true so inserts without the field do not fail NOT NULL
        modelBuilder.Entity<Customer>()
            .Property(c => c.IsActive)
            .HasDefaultValue(true);

        // DocumentNumber is unique per tenant
        modelBuilder.Entity<Customer>()
            .HasIndex(c => new { c.TenantId, c.DocumentNumber })
            .IsUnique();

        // Supplier multi-tenant query filter (soft-delete via IsActive)
        modelBuilder.Entity<Supplier>().HasQueryFilter(s => s.IsActive && s.TenantId == _tenantProvider.GetTenantId());

        // IsActive defaults to true so inserts without the field do not fail NOT NULL
        modelBuilder.Entity<Supplier>()
            .Property(s => s.IsActive)
            .HasDefaultValue(true);

        // DocumentNumber is unique per tenant
        modelBuilder.Entity<Supplier>()
            .HasIndex(s => new { s.TenantId, s.DocumentNumber })
            .IsUnique();

        // Purchase multi-tenant query filter (the header is tenant-scoped; items are
        // filtered indirectly through their parent Purchase).
        modelBuilder.Entity<Purchase>().HasQueryFilter(p => p.TenantId == _tenantProvider.GetTenantId());

        // PurchaseNumber is unique per tenant.
        modelBuilder.Entity<Purchase>()
            .HasIndex(p => new { p.TenantId, p.PurchaseNumber })
            .IsUnique();

        // Purchase -> Supplier (Restrict: keep purchase history even if the supplier is deactivated).
        modelBuilder.Entity<Purchase>()
            .HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        // Purchase has many PurchaseItems; deleting a Purchase cascades to its items.
        modelBuilder.Entity<Purchase>()
            .HasMany(p => p.Items)
            .WithOne()
            .HasForeignKey(i => i.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // PurchaseItem -> Product (Restrict: don't delete a product that has purchase lines).
        modelBuilder.Entity<PurchaseItem>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(modelBuilder);
    }

    // Override SaveChanges to set audit fields for all AuditableEntity instances
    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
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
    }
}