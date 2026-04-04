using Microsoft.EntityFrameworkCore;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Mcp.Data.Entities;

namespace SquadCommerce.Mcp.Tests.Tools;

/// <summary>
/// Creates InMemory SquadCommerceDbContext instances pre-seeded with test data.
/// Each call returns an isolated database to prevent test cross-contamination.
/// </summary>
internal static class DbContextTestHelper
{
    public static SquadCommerceDbContext CreateContext(string? dbName = null)
    {
        dbName ??= Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SquadCommerceDbContext(options);
    }

    public static SquadCommerceDbContext CreateSeededContext(string? dbName = null)
    {
        var context = CreateContext(dbName);
        SeedSuppliers(context);
        SeedShipments(context);
        SeedSocialSentiment(context);
        SeedStoreLayouts(context);
        context.SaveChanges();
        return context;
    }

    public static void SeedSuppliers(SquadCommerceDbContext context)
    {
        context.Suppliers.AddRange(
            new SupplierEntity
            {
                SupplierId = "SUP-001", Name = "EcoFarms Co", Category = "Cocoa",
                Country = "Ghana", Certification = "FairTrade",
                CertificationExpiry = DateTimeOffset.UtcNow.AddDays(180),
                Status = "Compliant", WatchlistNotes = null
            },
            new SupplierEntity
            {
                SupplierId = "SUP-002", Name = "GreenLeaf Ltd", Category = "Coffee",
                Country = "Colombia", Certification = "Organic",
                CertificationExpiry = DateTimeOffset.UtcNow.AddDays(30),
                Status = "AtRisk", WatchlistNotes = "Certification expiring soon"
            },
            new SupplierEntity
            {
                SupplierId = "SUP-003", Name = "DarkRoast Inc", Category = "Coffee",
                Country = "Brazil", Certification = "FairTrade",
                CertificationExpiry = DateTimeOffset.UtcNow.AddDays(-10),
                Status = "NonCompliant", WatchlistNotes = "Certification expired"
            },
            new SupplierEntity
            {
                SupplierId = "SUP-004", Name = "PureCocoa Partners", Category = "Cocoa",
                Country = "Ivory Coast", Certification = "FairTrade",
                CertificationExpiry = DateTimeOffset.UtcNow.AddDays(365),
                Status = "Compliant", WatchlistNotes = null
            },
            new SupplierEntity
            {
                SupplierId = "SUP-005", Name = "SilkThread Apparel", Category = "Apparel",
                Country = "Vietnam", Certification = "Organic",
                CertificationExpiry = DateTimeOffset.UtcNow.AddDays(90),
                Status = "Compliant", WatchlistNotes = null
            }
        );
    }

    public static void SeedShipments(SquadCommerceDbContext context)
    {
        context.Shipments.AddRange(
            new ShipmentEntity
            {
                ShipmentId = "SHP-001", Sku = "SKU-2001", ProductName = "Organic Coffee Beans",
                OriginStoreId = "SEA-001", DestStoreId = "PDX-002",
                Status = "InTransit", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(2),
                DelayDays = 0, DelayReason = null, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new ShipmentEntity
            {
                ShipmentId = "SHP-002", Sku = "SKU-2001", ProductName = "Organic Coffee Beans",
                OriginStoreId = "NYC-006", DestStoreId = "BOS-007",
                Status = "Delayed", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(5),
                DelayDays = 3, DelayReason = "Weather delay", CreatedAt = DateTimeOffset.UtcNow.AddDays(-3)
            },
            new ShipmentEntity
            {
                ShipmentId = "SHP-003", Sku = "SKU-3001", ProductName = "Artisan Chocolate Bar",
                OriginStoreId = "LAX-004", DestStoreId = "SFO-003",
                Status = "Delivered", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(-1),
                DelayDays = 0, DelayReason = null, CreatedAt = DateTimeOffset.UtcNow.AddDays(-5)
            }
        );
    }

    public static void SeedSocialSentiment(SquadCommerceDbContext context)
    {
        context.SocialSentiment.AddRange(
            new SocialSentimentEntity
            {
                Sku = "SKU-3001", ProductName = "Artisan Chocolate Bar",
                Platform = "TikTok", SentimentScore = 0.92, Velocity = 4.5,
                Region = "Northeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-2)
            },
            new SocialSentimentEntity
            {
                Sku = "SKU-3001", ProductName = "Artisan Chocolate Bar",
                Platform = "Instagram", SentimentScore = 0.85, Velocity = 3.2,
                Region = "Northeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-4)
            },
            new SocialSentimentEntity
            {
                Sku = "SKU-3001", ProductName = "Artisan Chocolate Bar",
                Platform = "Twitter", SentimentScore = 0.65, Velocity = 0.8,
                Region = "Southeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-6)
            },
            new SocialSentimentEntity
            {
                Sku = "SKU-4001", ProductName = "Premium T-Shirt",
                Platform = "TikTok", SentimentScore = 0.45, Velocity = -0.5,
                Region = "Northeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-1)
            }
        );
    }

    public static void SeedStoreLayouts(SquadCommerceDbContext context)
    {
        context.StoreLayouts.AddRange(
            new StoreLayoutEntity
            {
                StoreId = "SEA-001", StoreName = "Downtown Flagship",
                Section = "Electronics", SquareFootage = 2500, ShelfCount = 12,
                AvgHourlyTraffic = 150.0, OptimalPlacement = "Front"
            },
            new StoreLayoutEntity
            {
                StoreId = "SEA-001", StoreName = "Downtown Flagship",
                Section = "Grocery", SquareFootage = 4000, ShelfCount = 20,
                AvgHourlyTraffic = 200.0, OptimalPlacement = "Middle"
            },
            new StoreLayoutEntity
            {
                StoreId = "SEA-001", StoreName = "Downtown Flagship",
                Section = "Home", SquareFootage = 1800, ShelfCount = 8,
                AvgHourlyTraffic = 50.0, OptimalPlacement = "Back"
            },
            new StoreLayoutEntity
            {
                StoreId = "PDX-002", StoreName = "Suburban Mall",
                Section = "Electronics", SquareFootage = 1500, ShelfCount = 6,
                AvgHourlyTraffic = 80.0, OptimalPlacement = "Front"
            }
        );
    }
}
