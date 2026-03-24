using Microsoft.EntityFrameworkCore;
using SquadCommerce.Mcp.Data.Entities;

namespace SquadCommerce.Mcp.Data;

/// <summary>
/// EF Core DbContext for Squad-Commerce data.
/// Manages Inventory and Pricing entities with SQLite backing.
/// </summary>
public sealed class SquadCommerceDbContext : DbContext
{
    public SquadCommerceDbContext(DbContextOptions<SquadCommerceDbContext> options)
        : base(options)
    {
    }

    public DbSet<InventoryEntity> Inventory => Set<InventoryEntity>();
    public DbSet<PricingEntity> Pricing => Set<PricingEntity>();
    public DbSet<AuditEntryEntity> AuditEntries => Set<AuditEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure composite primary key for Inventory
        modelBuilder.Entity<InventoryEntity>(entity =>
        {
            entity.ToTable("Inventory");
            entity.HasKey(e => new { e.StoreId, e.Sku });
            
            entity.Property(e => e.StoreId).HasMaxLength(20).IsRequired();
            entity.Property(e => e.StoreName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Sku).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ProductName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.QuantityOnHand).IsRequired();
            entity.Property(e => e.ReorderThreshold).IsRequired();
            entity.Property(e => e.LastRestocked).IsRequired();
        });

        // Configure composite primary key for Pricing
        modelBuilder.Entity<PricingEntity>(entity =>
        {
            entity.ToTable("Pricing");
            entity.HasKey(e => new { e.StoreId, e.Sku });
            
            entity.Property(e => e.StoreId).HasMaxLength(20).IsRequired();
            entity.Property(e => e.StoreName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Sku).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ProductName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CurrentPrice).HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.Cost).HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.MarginPercent).HasPrecision(5, 2).IsRequired();
            entity.Property(e => e.LastUpdated).IsRequired();
        });

        // Configure primary key for AuditEntries
        modelBuilder.Entity<AuditEntryEntity>(entity =>
        {
            entity.ToTable("AuditEntries");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasMaxLength(50).IsRequired();
            entity.Property(e => e.SessionId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.AgentName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Protocol).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.DurationMs).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Details).HasMaxLength(1000);
            entity.Property(e => e.TraceId).HasMaxLength(50);
            entity.Property(e => e.DecisionOutcome).HasMaxLength(200);
            entity.Property(e => e.AffectedSkusCsv).HasMaxLength(500);
            entity.Property(e => e.AffectedStoresCsv).HasMaxLength(500);

            // Index on SessionId for fast retrieval
            entity.HasIndex(e => e.SessionId);
            // Index on Timestamp for recent entries queries
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
