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
    public DbSet<Sale> Sales { get; set; } = null!;
    public DbSet<SaleItem> SaleItems { get; set; } = null!;
    public DbSet<CashRegister> CashRegisters { get; set; } = null!;
    public DbSet<CashMovement> CashMovements { get; set; } = null!;

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

        // Sale multi-tenant query filter (the header is tenant-scoped; items are
        // filtered indirectly through their parent Sale).
        modelBuilder.Entity<Sale>().HasQueryFilter(s => s.TenantId == _tenantProvider.GetTenantId());

        // SaleNumber is unique per tenant.
        modelBuilder.Entity<Sale>()
            .HasIndex(s => new { s.TenantId, s.SaleNumber })
            .IsUnique();

        // Simple indexes for the most common lookups/filters.
        modelBuilder.Entity<Sale>().HasIndex(s => s.SaleNumber);
        modelBuilder.Entity<Sale>().HasIndex(s => s.SaleDate);
        modelBuilder.Entity<Sale>().HasIndex(s => s.CustomerId);
        modelBuilder.Entity<Sale>().HasIndex(s => s.Status);
        modelBuilder.Entity<Sale>().HasIndex(s => s.PaymentStatus);
        modelBuilder.Entity<Sale>().HasIndex(s => s.TenantId);

        // Sale -> Customer (Restrict: keep sale history even if the customer is deactivated).
        modelBuilder.Entity<Sale>()
            .HasOne<Customer>()
            .WithMany()
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sale has many SaleItems; deleting a Sale cascades to its items.
        modelBuilder.Entity<Sale>()
            .HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Monetary precision for the header totals.
        modelBuilder.Entity<Sale>()
            .Property(s => s.Subtotal).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Sale>()
            .Property(s => s.Tax).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Sale>()
            .Property(s => s.Total).HasColumnType("decimal(18,2)");

        // SaleItem -> Product (Restrict: don't delete a product that has sale lines).
        modelBuilder.Entity<SaleItem>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Monetary precision for the item (historical price snapshot). All values are
        // frozen at sale time, so they use the same fixed scale as the header totals.
        modelBuilder.Entity<SaleItem>()
            .Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<SaleItem>()
            .Property(i => i.DiscountPercentage).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<SaleItem>()
            .Property(i => i.TaxPercentage).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<SaleItem>()
            .Property(i => i.LineSubtotal).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<SaleItem>()
            .Property(i => i.LineTax).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<SaleItem>()
            .Property(i => i.LineTotal).HasColumnType("decimal(18,2)");

        // CashRegister multi-tenant query filter (the header is tenant-scoped; movements
        // are filtered indirectly through their parent CashRegister).
        modelBuilder.Entity<CashRegister>().HasQueryFilter(c => c.TenantId == _tenantProvider.GetTenantId());

        // CashRegisterNumber is unique per tenant.
        modelBuilder.Entity<CashRegister>()
            .HasIndex(c => new { c.TenantId, c.CashRegisterNumber })
            .IsUnique();

        // Simple indexes for the most common lookups/filters.
        modelBuilder.Entity<CashRegister>().HasIndex(c => c.UserId);
        modelBuilder.Entity<CashRegister>().HasIndex(c => c.Status);
        modelBuilder.Entity<CashRegister>().HasIndex(c => c.OpeningDate);

        // Monetary precision for the cash register amounts.
        modelBuilder.Entity<CashRegister>()
            .Property(c => c.OpeningAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CashRegister>()
            .Property(c => c.ClosingAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CashRegister>()
            .Property(c => c.ExpectedAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CashRegister>()
            .Property(c => c.Difference).HasColumnType("decimal(18,2)");

        // CashMovement -> CashRegister (Cascade: deleting a register removes its movements).
        // Se usa la navegación explícita CashRegister.Movements para que EF reutilice la
        // FK CashRegisterId existente en lugar de crear una FK sombra duplicada.
        modelBuilder.Entity<CashMovement>()
            .HasOne<CashRegister>()
            .WithMany(cr => cr.Movements)
            .HasForeignKey(m => m.CashRegisterId)
            .OnDelete(DeleteBehavior.Cascade);

        // CashMovement -> Sale (Restrict, nullable: a movement may not be linked to a sale).
        modelBuilder.Entity<CashMovement>()
            .HasOne<Sale>()
            .WithMany()
            .HasForeignKey(m => m.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        // CashMovement -> Purchase (Restrict, nullable: a movement may not be linked to a
        // purchase). Espejo de la relación con Sale para trazabilidad simétrica.
        modelBuilder.Entity<CashMovement>()
            .HasOne<Purchase>()
            .WithMany()
            .HasForeignKey(m => m.PurchaseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Monetary precision for the cash movement amount.
        modelBuilder.Entity<CashMovement>()
            .Property(m => m.Amount).HasColumnType("decimal(18,2)");

        // Partial unique index: at most one movement per (CashRegister, Sale, Type) when
        // SaleId is present (PostgreSQL partial index via HasFilter). Impide duplicar dos
        // movimientos del MISMO Type para la misma venta en la misma caja (ej. dos Income),
        // pero permite el movimiento de reversión (Type opuesto) para la misma venta.
        modelBuilder.Entity<CashMovement>()
            .HasIndex(m => new { m.CashRegisterId, m.SaleId, m.Type })
            .IsUnique()
            .HasFilter("\"SaleId\" IS NOT NULL");

        // Partial unique index espejo para Purchase: a lo sumo un movimiento por
        // (CashRegister, Purchase, Type) cuando PurchaseId está presente. Misma lógica:
        // protege contra duplicados del mismo Type pero permite la reversión (Type opuesto).
        modelBuilder.Entity<CashMovement>()
            .HasIndex(m => new { m.CashRegisterId, m.PurchaseId, m.Type })
            .IsUnique()
            .HasFilter("\"PurchaseId\" IS NOT NULL");

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