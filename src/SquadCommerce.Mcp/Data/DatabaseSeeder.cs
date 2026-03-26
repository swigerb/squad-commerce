using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SquadCommerce.Mcp.Data.Entities;

namespace SquadCommerce.Mcp.Data;

/// <summary>
/// Seeds the database with demo data.
/// Idempotent - only seeds if database is empty.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly SquadCommerceDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(SquadCommerceDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Seeds the database with demo inventory and pricing data.
    /// Only seeds if the database is empty (idempotent).
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Check if already seeded
        if (await _context.Inventory.AnyAsync(cancellationToken) || await _context.Pricing.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database already seeded - skipping");
            return;
        }

        _logger.LogInformation("Seeding database with demo data...");

        // ────────────────────────────────────────────────────────────
        // Store and product master data
        // ────────────────────────────────────────────────────────────
        var stores = new (string Id, string Name)[]
        {
            ("SEA-001", "Downtown Flagship"),
            ("PDX-002", "Suburban Mall"),
            ("SFO-003", "Airport Terminal"),
            ("LAX-004", "University District"),
            ("DEN-005", "Waterfront Plaza"),
            ("NYC-006", "Times Square Flagship"),
            ("BOS-007", "Back Bay Mall"),
            ("PHI-008", "Center City Plaza"),
            ("MIA-009", "Miami Flagship"),
            ("TPA-010", "Tampa Gateway"),
            ("ORL-011", "Orlando Resort District"),
            ("ATL-012", "Peachtree Center"),
        };

        var products = new (string Sku, string Name, decimal BasePrice, decimal Cost)[]
        {
            ("SKU-1001", "Wireless Mouse",                 29.99m, 15.00m),
            ("SKU-1002", "USB-C Cable 6ft",                12.99m,  4.50m),
            ("SKU-1003", "Laptop Stand",                   49.99m, 25.00m),
            ("SKU-1004", "Webcam 1080p",                   79.99m, 40.00m),
            ("SKU-1005", "Mechanical Keyboard",           119.99m, 65.00m),
            ("SKU-1006", "Noise-Cancelling Headphones",   199.99m,110.00m),
            ("SKU-1007", "External SSD 1TB",               89.99m, 50.00m),
            ("SKU-1008", "Monitor 27-inch",               349.99m,200.00m),
            ("SKU-2001", "Organic Fair Trade Coffee",      14.99m,  6.50m),
            ("SKU-2002", "Dark Chocolate Bar 72% Cocoa",    6.99m,  2.80m),
            ("SKU-2003", "Cocoa Powder Premium",           11.99m,  4.50m),
            ("SKU-2004", "Hot Chocolate Mix",               8.99m,  3.20m),
            ("SKU-3001", "Classic Straight Denim",         59.99m, 22.00m),
            ("SKU-3002", "Classic Boot-Cut Denim",         64.99m, 24.00m),
            ("SKU-3003", "Denim Jacket Classic",           89.99m, 35.00m),
            ("SKU-3004", "Canvas Belt",                    24.99m,  8.00m),
        };

        // Deterministic seed for reproducible demo data
        var rng = new Random(42);

        // ────────────────────────────────────────────────────────────
        // Seed inventory data (12 stores × 16 SKUs = 192 records)
        // ────────────────────────────────────────────────────────────
        var inventoryData = new List<InventoryEntity>();
        foreach (var store in stores)
        {
            foreach (var product in products)
            {
                var qty = rng.Next(10, 201);
                var reorder = rng.Next(5, 51);
                var daysSinceRestock = rng.Next(1, 14);
                inventoryData.Add(new InventoryEntity
                {
                    StoreId = store.Id,
                    StoreName = store.Name,
                    Sku = product.Sku,
                    ProductName = product.Name,
                    QuantityOnHand = qty,
                    ReorderThreshold = reorder,
                    LastRestocked = DateTimeOffset.UtcNow.AddDays(-daysSinceRestock),
                });
            }
        }

        // ────────────────────────────────────────────────────────────
        // Seed pricing data (12 stores × 16 SKUs = 192 records)
        // ±10 % variation per store
        // ────────────────────────────────────────────────────────────
        var pricingData = new List<PricingEntity>();
        foreach (var store in stores)
        {
            foreach (var product in products)
            {
                // ±10% price variation
                var factor = 0.90m + (decimal)(rng.NextDouble() * 0.20);
                var price = Math.Round(product.BasePrice * factor, 2);
                var margin = Math.Round((price - product.Cost) / price * 100m, 1);
                var daysSinceUpdate = rng.Next(1, 14);
                pricingData.Add(new PricingEntity
                {
                    StoreId = store.Id,
                    StoreName = store.Name,
                    Sku = product.Sku,
                    ProductName = product.Name,
                    CurrentPrice = price,
                    Cost = product.Cost,
                    MarginPercent = margin,
                    LastUpdated = DateTimeOffset.UtcNow.AddDays(-daysSinceUpdate),
                });
            }
        }

        await _context.Inventory.AddRangeAsync(inventoryData, cancellationToken);
        await _context.Pricing.AddRangeAsync(pricingData, cancellationToken);

        // ────────────────────────────────────────────────────────────
        // Seed shipment records (~15)
        // ────────────────────────────────────────────────────────────
        var shipmentData = new List<ShipmentEntity>
        {
            // DELAYED: Organic Coffee heading to ATL-012, tropical storm
            new() { ShipmentId = "SHP-001", Sku = "SKU-2001", ProductName = "Organic Fair Trade Coffee", OriginStoreId = "MIA-009", DestStoreId = "ATL-012", Status = "Delayed", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(3), DelayDays = 3, DelayReason = "Tropical Storm", CreatedAt = DateTimeOffset.UtcNow.AddDays(-2) },
            // InTransit
            new() { ShipmentId = "SHP-002", Sku = "SKU-1001", ProductName = "Wireless Mouse", OriginStoreId = "SEA-001", DestStoreId = "PDX-002", Status = "InTransit", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(1), DelayDays = 0, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
            new() { ShipmentId = "SHP-003", Sku = "SKU-3001", ProductName = "Classic Straight Denim", OriginStoreId = "NYC-006", DestStoreId = "BOS-007", Status = "InTransit", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(2), DelayDays = 0, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
            new() { ShipmentId = "SHP-004", Sku = "SKU-2002", ProductName = "Dark Chocolate Bar 72% Cocoa", OriginStoreId = "DEN-005", DestStoreId = "SFO-003", Status = "InTransit", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(3), DelayDays = 0, CreatedAt = DateTimeOffset.UtcNow.AddDays(-2) },
            new() { ShipmentId = "SHP-005", Sku = "SKU-1005", ProductName = "Mechanical Keyboard", OriginStoreId = "LAX-004", DestStoreId = "MIA-009", Status = "InTransit", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(4), DelayDays = 0, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
            new() { ShipmentId = "SHP-006", Sku = "SKU-3003", ProductName = "Denim Jacket Classic", OriginStoreId = "PHI-008", DestStoreId = "NYC-006", Status = "InTransit", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(1), DelayDays = 0, CreatedAt = DateTimeOffset.UtcNow },
            // Delayed
            new() { ShipmentId = "SHP-007", Sku = "SKU-1008", ProductName = "Monitor 27-inch", OriginStoreId = "SFO-003", DestStoreId = "LAX-004", Status = "Delayed", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(5), DelayDays = 2, DelayReason = "Customs Hold", CreatedAt = DateTimeOffset.UtcNow.AddDays(-4) },
            new() { ShipmentId = "SHP-008", Sku = "SKU-2003", ProductName = "Cocoa Powder Premium", OriginStoreId = "ORL-011", DestStoreId = "TPA-010", Status = "Delayed", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(2), DelayDays = 1, DelayReason = "Warehouse Backlog", CreatedAt = DateTimeOffset.UtcNow.AddDays(-3) },
            // Delivered
            new() { ShipmentId = "SHP-009", Sku = "SKU-1002", ProductName = "USB-C Cable 6ft", OriginStoreId = "SEA-001", DestStoreId = "DEN-005", Status = "Delivered", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(-1), DelayDays = 0, CreatedAt = DateTimeOffset.UtcNow.AddDays(-5) },
            new() { ShipmentId = "SHP-010", Sku = "SKU-3002", ProductName = "Classic Boot-Cut Denim", OriginStoreId = "NYC-006", DestStoreId = "PHI-008", Status = "Delivered", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(-2), DelayDays = 0, CreatedAt = DateTimeOffset.UtcNow.AddDays(-6) },
            new() { ShipmentId = "SHP-011", Sku = "SKU-2004", ProductName = "Hot Chocolate Mix", OriginStoreId = "BOS-007", DestStoreId = "NYC-006", Status = "Delivered", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(-1), DelayDays = 0, CreatedAt = DateTimeOffset.UtcNow.AddDays(-4) },
            new() { ShipmentId = "SHP-012", Sku = "SKU-1006", ProductName = "Noise-Cancelling Headphones", OriginStoreId = "PDX-002", DestStoreId = "SEA-001", Status = "Delivered", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(-3), DelayDays = 0, CreatedAt = DateTimeOffset.UtcNow.AddDays(-7) },
            new() { ShipmentId = "SHP-013", Sku = "SKU-1004", ProductName = "Webcam 1080p", OriginStoreId = "DEN-005", DestStoreId = "ATL-012", Status = "InTransit", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(2), DelayDays = 0, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
            new() { ShipmentId = "SHP-014", Sku = "SKU-3004", ProductName = "Canvas Belt", OriginStoreId = "MIA-009", DestStoreId = "ORL-011", Status = "Delivered", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(-2), DelayDays = 0, CreatedAt = DateTimeOffset.UtcNow.AddDays(-5) },
            new() { ShipmentId = "SHP-015", Sku = "SKU-2001", ProductName = "Organic Fair Trade Coffee", OriginStoreId = "TPA-010", DestStoreId = "MIA-009", Status = "InTransit", EstimatedArrival = DateTimeOffset.UtcNow.AddDays(1), DelayDays = 0, CreatedAt = DateTimeOffset.UtcNow },
        };

        await _context.Shipments.AddRangeAsync(shipmentData, cancellationToken);

        // ────────────────────────────────────────────────────────────
        // Seed social sentiment records (~20)
        // ────────────────────────────────────────────────────────────
        var sentimentData = new List<SocialSentimentEntity>
        {
            // SKU-3001 trending hard on TikTok — Northeast
            new() { Sku = "SKU-3001", ProductName = "Classic Straight Denim", Platform = "TikTok", SentimentScore = 0.92, Velocity = 4.2, Region = "Northeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-6) },
            new() { Sku = "SKU-3001", ProductName = "Classic Straight Denim", Platform = "Instagram", SentimentScore = 0.85, Velocity = 2.8, Region = "Northeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-5) },
            new() { Sku = "SKU-3001", ProductName = "Classic Straight Denim", Platform = "Twitter", SentimentScore = 0.78, Velocity = 1.5, Region = "Southeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-8) },
            // Denim Jacket moderate interest
            new() { Sku = "SKU-3003", ProductName = "Denim Jacket Classic", Platform = "TikTok", SentimentScore = 0.74, Velocity = 1.9, Region = "Northeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-12) },
            new() { Sku = "SKU-3003", ProductName = "Denim Jacket Classic", Platform = "Instagram", SentimentScore = 0.70, Velocity = 1.4, Region = "West", DetectedAt = DateTimeOffset.UtcNow.AddHours(-10) },
            // Noise-Cancelling Headphones steady interest
            new() { Sku = "SKU-1006", ProductName = "Noise-Cancelling Headphones", Platform = "TikTok", SentimentScore = 0.80, Velocity = 2.1, Region = "West", DetectedAt = DateTimeOffset.UtcNow.AddHours(-4) },
            new() { Sku = "SKU-1006", ProductName = "Noise-Cancelling Headphones", Platform = "Twitter", SentimentScore = 0.65, Velocity = 0.9, Region = "Northeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-7) },
            // Coffee products — moderate
            new() { Sku = "SKU-2001", ProductName = "Organic Fair Trade Coffee", Platform = "Instagram", SentimentScore = 0.72, Velocity = 1.3, Region = "Southeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-9) },
            new() { Sku = "SKU-2001", ProductName = "Organic Fair Trade Coffee", Platform = "Twitter", SentimentScore = 0.68, Velocity = 0.8, Region = "West", DetectedAt = DateTimeOffset.UtcNow.AddHours(-11) },
            // Chocolate trending mildly
            new() { Sku = "SKU-2002", ProductName = "Dark Chocolate Bar 72% Cocoa", Platform = "TikTok", SentimentScore = 0.76, Velocity = 2.0, Region = "Northeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-3) },
            new() { Sku = "SKU-2002", ProductName = "Dark Chocolate Bar 72% Cocoa", Platform = "Instagram", SentimentScore = 0.71, Velocity = 1.1, Region = "Southeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-6) },
            // Electronics baseline
            new() { Sku = "SKU-1001", ProductName = "Wireless Mouse", Platform = "Twitter", SentimentScore = 0.55, Velocity = 0.4, Region = "West", DetectedAt = DateTimeOffset.UtcNow.AddHours(-14) },
            new() { Sku = "SKU-1005", ProductName = "Mechanical Keyboard", Platform = "TikTok", SentimentScore = 0.82, Velocity = 2.5, Region = "West", DetectedAt = DateTimeOffset.UtcNow.AddHours(-2) },
            new() { Sku = "SKU-1005", ProductName = "Mechanical Keyboard", Platform = "Instagram", SentimentScore = 0.77, Velocity = 1.8, Region = "Northeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-5) },
            // Boot-Cut Denim
            new() { Sku = "SKU-3002", ProductName = "Classic Boot-Cut Denim", Platform = "TikTok", SentimentScore = 0.60, Velocity = 0.7, Region = "Southeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-16) },
            new() { Sku = "SKU-3002", ProductName = "Classic Boot-Cut Denim", Platform = "Instagram", SentimentScore = 0.58, Velocity = 0.5, Region = "West", DetectedAt = DateTimeOffset.UtcNow.AddHours(-15) },
            // Canvas Belt low
            new() { Sku = "SKU-3004", ProductName = "Canvas Belt", Platform = "Instagram", SentimentScore = 0.45, Velocity = 0.3, Region = "Northeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-20) },
            // Hot Chocolate seasonal
            new() { Sku = "SKU-2004", ProductName = "Hot Chocolate Mix", Platform = "TikTok", SentimentScore = 0.88, Velocity = 3.1, Region = "Northeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-1) },
            new() { Sku = "SKU-2004", ProductName = "Hot Chocolate Mix", Platform = "Instagram", SentimentScore = 0.83, Velocity = 2.4, Region = "Southeast", DetectedAt = DateTimeOffset.UtcNow.AddHours(-3) },
            // Monitor steady
            new() { Sku = "SKU-1008", ProductName = "Monitor 27-inch", Platform = "Twitter", SentimentScore = 0.62, Velocity = 0.6, Region = "West", DetectedAt = DateTimeOffset.UtcNow.AddHours(-18) },
        };

        await _context.SocialSentiment.AddRangeAsync(sentimentData, cancellationToken);

        // ────────────────────────────────────────────────────────────
        // Seed supplier records (~12)
        // ────────────────────────────────────────────────────────────
        var supplierData = new List<SupplierEntity>
        {
            // Cocoa suppliers (4)
            new() { SupplierId = "SUP-C01", Name = "Abidjan Cocoa Co-op", Category = "Cocoa", Country = "Ivory Coast", Certification = "FairTrade", CertificationExpiry = DateTimeOffset.UtcNow.AddMonths(18), Status = "Compliant" },
            new() { SupplierId = "SUP-C02", Name = "Ghana Premium Beans", Category = "Cocoa", Country = "Ghana", Certification = "FairTrade", CertificationExpiry = DateTimeOffset.UtcNow.AddDays(45), Status = "AtRisk", WatchlistNotes = "FairTrade certification expiring in 45 days; renewal audit pending" },
            new() { SupplierId = "SUP-C03", Name = "Cameroon Harvest Ltd", Category = "Cocoa", Country = "Cameroon", Certification = "None", CertificationExpiry = DateTimeOffset.UtcNow.AddDays(-30), Status = "NonCompliant", WatchlistNotes = "Failed last audit — child labor concerns reported by NGO watchdog" },
            new() { SupplierId = "SUP-C04", Name = "Ecuador Organic Cacao", Category = "Cocoa", Country = "Ecuador", Certification = "Organic", CertificationExpiry = DateTimeOffset.UtcNow.AddMonths(12), Status = "Compliant" },

            // Coffee suppliers (4)
            new() { SupplierId = "SUP-K01", Name = "Colombian Highland Growers", Category = "Coffee", Country = "Colombia", Certification = "FairTrade", CertificationExpiry = DateTimeOffset.UtcNow.AddMonths(24), Status = "Compliant" },
            new() { SupplierId = "SUP-K02", Name = "Ethiopian Yirgacheffe Union", Category = "Coffee", Country = "Ethiopia", Certification = "RainforestAlliance", CertificationExpiry = DateTimeOffset.UtcNow.AddMonths(8), Status = "Compliant" },
            new() { SupplierId = "SUP-K03", Name = "Sumatra Direct Trade", Category = "Coffee", Country = "Indonesia", Certification = "None", CertificationExpiry = DateTimeOffset.UtcNow.AddDays(-60), Status = "AtRisk", WatchlistNotes = "No certification; deforestation allegations under review" },
            new() { SupplierId = "SUP-K04", Name = "Guatemala Antigua Farms", Category = "Coffee", Country = "Guatemala", Certification = "Organic", CertificationExpiry = DateTimeOffset.UtcNow.AddMonths(6), Status = "Compliant" },

            // Apparel suppliers (4)
            new() { SupplierId = "SUP-A01", Name = "Carolina Cotton Mills", Category = "Apparel", Country = "United States", Certification = "None", CertificationExpiry = DateTimeOffset.UtcNow.AddMonths(36), Status = "Compliant" },
            new() { SupplierId = "SUP-A02", Name = "Bangladesh Textile Group", Category = "Apparel", Country = "Bangladesh", Certification = "FairTrade", CertificationExpiry = DateTimeOffset.UtcNow.AddDays(30), Status = "AtRisk", WatchlistNotes = "Fire safety inspection overdue; FairTrade renewal at risk" },
            new() { SupplierId = "SUP-A03", Name = "Vietnam Denim Works", Category = "Apparel", Country = "Vietnam", Certification = "None", CertificationExpiry = DateTimeOffset.UtcNow.AddMonths(12), Status = "Compliant" },
            new() { SupplierId = "SUP-A04", Name = "Turkish Weave Co", Category = "Apparel", Country = "Turkey", Certification = "RainforestAlliance", CertificationExpiry = DateTimeOffset.UtcNow.AddMonths(10), Status = "Compliant" },
        };

        await _context.Suppliers.AddRangeAsync(supplierData, cancellationToken);

        // ────────────────────────────────────────────────────────────
        // Seed store layout records (~30)
        // ────────────────────────────────────────────────────────────
        var layoutData = new List<StoreLayoutEntity>
        {
            // MIA-009 Miami Flagship — 5 sections
            new() { StoreId = "MIA-009", StoreName = "Miami Flagship", Section = "Electronics", SquareFootage = 3200, ShelfCount = 40, AvgHourlyTraffic = 120.5, OptimalPlacement = "Front" },
            new() { StoreId = "MIA-009", StoreName = "Miami Flagship", Section = "Grocery", SquareFootage = 4500, ShelfCount = 60, AvgHourlyTraffic = 185.0, OptimalPlacement = "Middle" },
            new() { StoreId = "MIA-009", StoreName = "Miami Flagship", Section = "Apparel", SquareFootage = 3800, ShelfCount = 45, AvgHourlyTraffic = 150.3, OptimalPlacement = "Front" },
            new() { StoreId = "MIA-009", StoreName = "Miami Flagship", Section = "Home", SquareFootage = 2800, ShelfCount = 35, AvgHourlyTraffic = 90.2, OptimalPlacement = "Back" },
            new() { StoreId = "MIA-009", StoreName = "Miami Flagship", Section = "Outdoor", SquareFootage = 2200, ShelfCount = 25, AvgHourlyTraffic = 75.8, OptimalPlacement = "EndCap" },

            // TPA-010 Tampa Gateway — foot traffic reference
            new() { StoreId = "TPA-010", StoreName = "Tampa Gateway", Section = "Electronics", SquareFootage = 2400, ShelfCount = 30, AvgHourlyTraffic = 88.4, OptimalPlacement = "Front" },
            new() { StoreId = "TPA-010", StoreName = "Tampa Gateway", Section = "Grocery", SquareFootage = 3500, ShelfCount = 50, AvgHourlyTraffic = 145.2, OptimalPlacement = "Middle" },
            new() { StoreId = "TPA-010", StoreName = "Tampa Gateway", Section = "Apparel", SquareFootage = 2800, ShelfCount = 35, AvgHourlyTraffic = 95.6, OptimalPlacement = "Middle" },
            new() { StoreId = "TPA-010", StoreName = "Tampa Gateway", Section = "Home", SquareFootage = 2000, ShelfCount = 28, AvgHourlyTraffic = 65.1, OptimalPlacement = "Back" },

            // ORL-011 Orlando Resort District — foot traffic reference
            new() { StoreId = "ORL-011", StoreName = "Orlando Resort District", Section = "Electronics", SquareFootage = 2600, ShelfCount = 32, AvgHourlyTraffic = 110.0, OptimalPlacement = "Front" },
            new() { StoreId = "ORL-011", StoreName = "Orlando Resort District", Section = "Grocery", SquareFootage = 3000, ShelfCount = 42, AvgHourlyTraffic = 130.5, OptimalPlacement = "Middle" },
            new() { StoreId = "ORL-011", StoreName = "Orlando Resort District", Section = "Apparel", SquareFootage = 3400, ShelfCount = 40, AvgHourlyTraffic = 140.8, OptimalPlacement = "Front" },
            new() { StoreId = "ORL-011", StoreName = "Orlando Resort District", Section = "Home", SquareFootage = 1800, ShelfCount = 22, AvgHourlyTraffic = 55.3, OptimalPlacement = "Back" },

            // ATL-012 Peachtree Center
            new() { StoreId = "ATL-012", StoreName = "Peachtree Center", Section = "Electronics", SquareFootage = 2800, ShelfCount = 36, AvgHourlyTraffic = 105.2, OptimalPlacement = "Front" },
            new() { StoreId = "ATL-012", StoreName = "Peachtree Center", Section = "Grocery", SquareFootage = 3800, ShelfCount = 55, AvgHourlyTraffic = 160.0, OptimalPlacement = "Middle" },
            new() { StoreId = "ATL-012", StoreName = "Peachtree Center", Section = "Apparel", SquareFootage = 3000, ShelfCount = 38, AvgHourlyTraffic = 118.4, OptimalPlacement = "EndCap" },

            // NYC-006 Times Square Flagship
            new() { StoreId = "NYC-006", StoreName = "Times Square Flagship", Section = "Electronics", SquareFootage = 4000, ShelfCount = 50, AvgHourlyTraffic = 220.0, OptimalPlacement = "Front" },
            new() { StoreId = "NYC-006", StoreName = "Times Square Flagship", Section = "Grocery", SquareFootage = 3200, ShelfCount = 45, AvgHourlyTraffic = 175.5, OptimalPlacement = "Middle" },
            new() { StoreId = "NYC-006", StoreName = "Times Square Flagship", Section = "Apparel", SquareFootage = 4500, ShelfCount = 55, AvgHourlyTraffic = 200.3, OptimalPlacement = "Front" },

            // BOS-007 Back Bay Mall
            new() { StoreId = "BOS-007", StoreName = "Back Bay Mall", Section = "Electronics", SquareFootage = 2200, ShelfCount = 28, AvgHourlyTraffic = 78.5, OptimalPlacement = "EndCap" },
            new() { StoreId = "BOS-007", StoreName = "Back Bay Mall", Section = "Grocery", SquareFootage = 2800, ShelfCount = 38, AvgHourlyTraffic = 112.0, OptimalPlacement = "Middle" },
            new() { StoreId = "BOS-007", StoreName = "Back Bay Mall", Section = "Apparel", SquareFootage = 2600, ShelfCount = 32, AvgHourlyTraffic = 92.7, OptimalPlacement = "Front" },

            // PHI-008 Center City Plaza
            new() { StoreId = "PHI-008", StoreName = "Center City Plaza", Section = "Electronics", SquareFootage = 2500, ShelfCount = 30, AvgHourlyTraffic = 85.0, OptimalPlacement = "Front" },
            new() { StoreId = "PHI-008", StoreName = "Center City Plaza", Section = "Grocery", SquareFootage = 3000, ShelfCount = 40, AvgHourlyTraffic = 125.3, OptimalPlacement = "Middle" },
            new() { StoreId = "PHI-008", StoreName = "Center City Plaza", Section = "Apparel", SquareFootage = 2700, ShelfCount = 34, AvgHourlyTraffic = 98.1, OptimalPlacement = "Middle" },

            // Original 5 stores — basic sections
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Section = "Electronics", SquareFootage = 3500, ShelfCount = 44, AvgHourlyTraffic = 135.0, OptimalPlacement = "Front" },
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Section = "Electronics", SquareFootage = 2000, ShelfCount = 26, AvgHourlyTraffic = 72.3, OptimalPlacement = "Middle" },
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Section = "Electronics", SquareFootage = 1800, ShelfCount = 20, AvgHourlyTraffic = 195.0, OptimalPlacement = "Front" },
            new() { StoreId = "LAX-004", StoreName = "University District", Section = "Electronics", SquareFootage = 2100, ShelfCount = 24, AvgHourlyTraffic = 82.5, OptimalPlacement = "EndCap" },
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Section = "Electronics", SquareFootage = 2300, ShelfCount = 29, AvgHourlyTraffic = 94.0, OptimalPlacement = "Front" },
        };

        await _context.StoreLayouts.AddRangeAsync(layoutData, cancellationToken);

        // Seed audit entries for a "completed" session demonstrating the workflow
        var completedSessionId = "session-demo-001";
        var baseTime = DateTimeOffset.UtcNow.AddHours(-2);

        var auditData = new List<AuditEntryEntity>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = completedSessionId,
                AgentName = "ChiefSoftwareArchitect",
                Action = "Initiated competitor price response workflow",
                Protocol = "AGUI",
                Timestamp = baseTime,
                DurationMs = 50,
                Status = "Success",
                Details = "User request: Analyze competitor price drop for SKU-1001",
                AffectedSkusCsv = "SKU-1001"
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = completedSessionId,
                AgentName = "MarketIntelAgent",
                Action = "Queried competitor pricing via A2A",
                Protocol = "A2A",
                Timestamp = baseTime.AddSeconds(5),
                DurationMs = 1250,
                Status = "Success",
                Details = "Retrieved 3 competitor prices, validated with ExternalDataValidator",
                AffectedSkusCsv = "SKU-1001"
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = completedSessionId,
                AgentName = "InventoryAgent",
                Action = "Retrieved inventory snapshot",
                Protocol = "MCP",
                Timestamp = baseTime.AddSeconds(7),
                DurationMs = 320,
                Status = "Success",
                Details = "Queried 5 stores using GetInventoryLevels tool",
                AffectedSkusCsv = "SKU-1001",
                AffectedStoresCsv = "SEA-001,PDX-002,SFO-003,LAX-004,DEN-005"
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = completedSessionId,
                AgentName = "PricingAgent",
                Action = "Calculated margin impact scenarios",
                Protocol = "MCP",
                Timestamp = baseTime.AddSeconds(8),
                DurationMs = 450,
                Status = "Success",
                Details = "Generated 4 pricing scenarios with revenue projections",
                AffectedSkusCsv = "SKU-1001"
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = completedSessionId,
                AgentName = "ChiefSoftwareArchitect",
                Action = "Synthesized orchestrator response",
                Protocol = "AGUI",
                Timestamp = baseTime.AddSeconds(9),
                DurationMs = 100,
                Status = "Success",
                Details = "Generated executive summary with 3 A2UI payloads"
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = completedSessionId,
                AgentName = "PricingManager",
                Action = "Reviewed pricing recommendation",
                Protocol = "Internal",
                Timestamp = baseTime.AddMinutes(15),
                DurationMs = 180000,
                Status = "Success",
                Details = "Human review of competitive pricing analysis",
                DecisionOutcome = "Approved Match Competitor scenario"
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = completedSessionId,
                AgentName = "PricingAgent",
                Action = "Applied pricing updates",
                Protocol = "MCP",
                Timestamp = baseTime.AddMinutes(18),
                DurationMs = 890,
                Status = "Success",
                Details = "Updated prices across 5 stores using UpdateStorePricing tool",
                AffectedSkusCsv = "SKU-1001",
                AffectedStoresCsv = "SEA-001,PDX-002,SFO-003,LAX-004,DEN-005"
            }
        };

        await _context.AuditEntries.AddRangeAsync(auditData, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Database seeded successfully with {InventoryCount} inventory, {PricingCount} pricing, {ShipmentCount} shipments, {SentimentCount} sentiment, {SupplierCount} suppliers, {LayoutCount} layouts, {AuditCount} audit entries",
            inventoryData.Count,
            pricingData.Count,
            shipmentData.Count,
            sentimentData.Count,
            supplierData.Count,
            layoutData.Count,
            auditData.Count);
    }
}
