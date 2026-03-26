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
    public DbSet<ShipmentEntity> Shipments => Set<ShipmentEntity>();
    public DbSet<SocialSentimentEntity> SocialSentiment => Set<SocialSentimentEntity>();
    public DbSet<SupplierEntity> Suppliers => Set<SupplierEntity>();
    public DbSet<StoreLayoutEntity> StoreLayouts => Set<StoreLayoutEntity>();

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

        // Configure primary key for Shipments
        modelBuilder.Entity<ShipmentEntity>(entity =>
        {
            entity.ToTable("Shipments");
            entity.HasKey(e => e.ShipmentId);

            entity.Property(e => e.ShipmentId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Sku).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ProductName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.OriginStoreId).HasMaxLength(20).IsRequired();
            entity.Property(e => e.DestStoreId).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.EstimatedArrival).IsRequired();
            entity.Property(e => e.DelayDays).IsRequired();
            entity.Property(e => e.DelayReason).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => e.Status);
        });

        // Configure primary key for SocialSentiment (auto-increment Id)
        modelBuilder.Entity<SocialSentimentEntity>(entity =>
        {
            entity.ToTable("SocialSentiment");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Sku).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ProductName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Platform).HasMaxLength(50).IsRequired();
            entity.Property(e => e.SentimentScore).IsRequired();
            entity.Property(e => e.Velocity).IsRequired();
            entity.Property(e => e.Region).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DetectedAt).IsRequired();

            entity.HasIndex(e => e.Sku);
        });

        // Configure primary key for Suppliers
        modelBuilder.Entity<SupplierEntity>(entity =>
        {
            entity.ToTable("Suppliers");
            entity.HasKey(e => e.SupplierId);

            entity.Property(e => e.SupplierId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Country).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Certification).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CertificationExpiry).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.WatchlistNotes).HasMaxLength(500);

            entity.HasIndex(e => e.Status);
        });

        // Configure primary key for StoreLayouts (auto-increment Id)
        modelBuilder.Entity<StoreLayoutEntity>(entity =>
        {
            entity.ToTable("StoreLayouts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.StoreId).HasMaxLength(20).IsRequired();
            entity.Property(e => e.StoreName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Section).HasMaxLength(50).IsRequired();
            entity.Property(e => e.SquareFootage).IsRequired();
            entity.Property(e => e.ShelfCount).IsRequired();
            entity.Property(e => e.AvgHourlyTraffic).IsRequired();
            entity.Property(e => e.OptimalPlacement).HasMaxLength(20).IsRequired();

            entity.HasIndex(e => e.StoreId);
        });
    }
}
