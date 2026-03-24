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

        // Seed inventory data (5 stores × 8 SKUs = 40 records)
        var inventoryData = new List<InventoryEntity>
        {
            // Store: Downtown Flagship (SEA-001)
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Sku = "SKU-1001", ProductName = "Wireless Mouse", QuantityOnHand = 45, ReorderThreshold = 20, LastRestocked = DateTimeOffset.UtcNow.AddDays(-5) },
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Sku = "SKU-1002", ProductName = "USB-C Cable 6ft", QuantityOnHand = 120, ReorderThreshold = 50, LastRestocked = DateTimeOffset.UtcNow.AddDays(-3) },
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Sku = "SKU-1003", ProductName = "Laptop Stand", QuantityOnHand = 18, ReorderThreshold = 10, LastRestocked = DateTimeOffset.UtcNow.AddDays(-7) },
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Sku = "SKU-1004", ProductName = "Webcam 1080p", QuantityOnHand = 32, ReorderThreshold = 15, LastRestocked = DateTimeOffset.UtcNow.AddDays(-2) },
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Sku = "SKU-1005", ProductName = "Mechanical Keyboard", QuantityOnHand = 25, ReorderThreshold = 12, LastRestocked = DateTimeOffset.UtcNow.AddDays(-4) },
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Sku = "SKU-1006", ProductName = "Noise-Cancelling Headphones", QuantityOnHand = 15, ReorderThreshold = 8, LastRestocked = DateTimeOffset.UtcNow.AddDays(-1) },
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Sku = "SKU-1007", ProductName = "External SSD 1TB", QuantityOnHand = 8, ReorderThreshold = 10, LastRestocked = DateTimeOffset.UtcNow.AddDays(-6) },
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Sku = "SKU-1008", ProductName = "Monitor 27-inch", QuantityOnHand = 12, ReorderThreshold = 5, LastRestocked = DateTimeOffset.UtcNow.AddDays(-8) },

            // Store: Suburban Mall (PDX-002)
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1001", ProductName = "Wireless Mouse", QuantityOnHand = 38, ReorderThreshold = 20, LastRestocked = DateTimeOffset.UtcNow.AddDays(-4) },
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1002", ProductName = "USB-C Cable 6ft", QuantityOnHand = 95, ReorderThreshold = 50, LastRestocked = DateTimeOffset.UtcNow.AddDays(-6) },
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1003", ProductName = "Laptop Stand", QuantityOnHand = 22, ReorderThreshold = 10, LastRestocked = DateTimeOffset.UtcNow.AddDays(-5) },
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1004", ProductName = "Webcam 1080p", QuantityOnHand = 28, ReorderThreshold = 15, LastRestocked = DateTimeOffset.UtcNow.AddDays(-3) },
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1005", ProductName = "Mechanical Keyboard", QuantityOnHand = 19, ReorderThreshold = 12, LastRestocked = DateTimeOffset.UtcNow.AddDays(-7) },
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1006", ProductName = "Noise-Cancelling Headphones", QuantityOnHand = 11, ReorderThreshold = 8, LastRestocked = DateTimeOffset.UtcNow.AddDays(-2) },
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1007", ProductName = "External SSD 1TB", QuantityOnHand = 14, ReorderThreshold = 10, LastRestocked = DateTimeOffset.UtcNow.AddDays(-4) },
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1008", ProductName = "Monitor 27-inch", QuantityOnHand = 9, ReorderThreshold = 5, LastRestocked = DateTimeOffset.UtcNow.AddDays(-9) },

            // Store: Airport Terminal (SFO-003) - Some low stock
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Sku = "SKU-1001", ProductName = "Wireless Mouse", QuantityOnHand = 52, ReorderThreshold = 20, LastRestocked = DateTimeOffset.UtcNow.AddDays(-2) },
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Sku = "SKU-1002", ProductName = "USB-C Cable 6ft", QuantityOnHand = 140, ReorderThreshold = 50, LastRestocked = DateTimeOffset.UtcNow.AddDays(-1) },
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Sku = "SKU-1003", ProductName = "Laptop Stand", QuantityOnHand = 6, ReorderThreshold = 10, LastRestocked = DateTimeOffset.UtcNow.AddDays(-10) },
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Sku = "SKU-1004", ProductName = "Webcam 1080p", QuantityOnHand = 41, ReorderThreshold = 15, LastRestocked = DateTimeOffset.UtcNow.AddDays(-3) },
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Sku = "SKU-1005", ProductName = "Mechanical Keyboard", QuantityOnHand = 30, ReorderThreshold = 12, LastRestocked = DateTimeOffset.UtcNow.AddDays(-5) },
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Sku = "SKU-1006", ProductName = "Noise-Cancelling Headphones", QuantityOnHand = 20, ReorderThreshold = 8, LastRestocked = DateTimeOffset.UtcNow.AddDays(-4) },
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Sku = "SKU-1007", ProductName = "External SSD 1TB", QuantityOnHand = 4, ReorderThreshold = 10, LastRestocked = DateTimeOffset.UtcNow.AddDays(-12) },
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Sku = "SKU-1008", ProductName = "Monitor 27-inch", QuantityOnHand = 16, ReorderThreshold = 5, LastRestocked = DateTimeOffset.UtcNow.AddDays(-6) },

            // Store: University District (LAX-004) - Some critical low stock
            new() { StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1001", ProductName = "Wireless Mouse", QuantityOnHand = 29, ReorderThreshold = 20, LastRestocked = DateTimeOffset.UtcNow.AddDays(-6) },
            new() { StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1002", ProductName = "USB-C Cable 6ft", QuantityOnHand = 88, ReorderThreshold = 50, LastRestocked = DateTimeOffset.UtcNow.AddDays(-5) },
            new() { StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1003", ProductName = "Laptop Stand", QuantityOnHand = 15, ReorderThreshold = 10, LastRestocked = DateTimeOffset.UtcNow.AddDays(-4) },
            new() { StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1004", ProductName = "Webcam 1080p", QuantityOnHand = 12, ReorderThreshold = 15, LastRestocked = DateTimeOffset.UtcNow.AddDays(-8) },
            new() { StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1005", ProductName = "Mechanical Keyboard", QuantityOnHand = 23, ReorderThreshold = 12, LastRestocked = DateTimeOffset.UtcNow.AddDays(-3) },
            new() { StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1006", ProductName = "Noise-Cancelling Headphones", QuantityOnHand = 18, ReorderThreshold = 8, LastRestocked = DateTimeOffset.UtcNow.AddDays(-2) },
            new() { StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1007", ProductName = "External SSD 1TB", QuantityOnHand = 11, ReorderThreshold = 10, LastRestocked = DateTimeOffset.UtcNow.AddDays(-7) },
            new() { StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1008", ProductName = "Monitor 27-inch", QuantityOnHand = 7, ReorderThreshold = 5, LastRestocked = DateTimeOffset.UtcNow.AddDays(-10) },

            // Store: Waterfront Plaza (DEN-005)
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Sku = "SKU-1001", ProductName = "Wireless Mouse", QuantityOnHand = 34, ReorderThreshold = 20, LastRestocked = DateTimeOffset.UtcNow.AddDays(-3) },
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Sku = "SKU-1002", ProductName = "USB-C Cable 6ft", QuantityOnHand = 105, ReorderThreshold = 50, LastRestocked = DateTimeOffset.UtcNow.AddDays(-4) },
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Sku = "SKU-1003", ProductName = "Laptop Stand", QuantityOnHand = 19, ReorderThreshold = 10, LastRestocked = DateTimeOffset.UtcNow.AddDays(-6) },
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Sku = "SKU-1004", ProductName = "Webcam 1080p", QuantityOnHand = 25, ReorderThreshold = 15, LastRestocked = DateTimeOffset.UtcNow.AddDays(-5) },
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Sku = "SKU-1005", ProductName = "Mechanical Keyboard", QuantityOnHand = 16, ReorderThreshold = 12, LastRestocked = DateTimeOffset.UtcNow.AddDays(-8) },
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Sku = "SKU-1006", ProductName = "Noise-Cancelling Headphones", QuantityOnHand = 13, ReorderThreshold = 8, LastRestocked = DateTimeOffset.UtcNow.AddDays(-3) },
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Sku = "SKU-1007", ProductName = "External SSD 1TB", QuantityOnHand = 9, ReorderThreshold = 10, LastRestocked = DateTimeOffset.UtcNow.AddDays(-9) },
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Sku = "SKU-1008", ProductName = "Monitor 27-inch", QuantityOnHand = 10, ReorderThreshold = 5, LastRestocked = DateTimeOffset.UtcNow.AddDays(-7) },
        };

        // Seed pricing data (5 stores × 8 SKUs = 40 records)
        var pricingData = new List<PricingEntity>
        {
            // Downtown Flagship (SEA-001)
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Sku = "SKU-1001", ProductName = "Wireless Mouse", CurrentPrice = 29.99m, Cost = 15.00m, MarginPercent = 50.0m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-10) },
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Sku = "SKU-1002", ProductName = "USB-C Cable 6ft", CurrentPrice = 12.99m, Cost = 4.50m, MarginPercent = 65.4m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-8) },
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Sku = "SKU-1003", ProductName = "Laptop Stand", CurrentPrice = 49.99m, Cost = 25.00m, MarginPercent = 50.0m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-12) },
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Sku = "SKU-1004", ProductName = "Webcam 1080p", CurrentPrice = 79.99m, Cost = 40.00m, MarginPercent = 50.0m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-5) },
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Sku = "SKU-1005", ProductName = "Mechanical Keyboard", CurrentPrice = 119.99m, Cost = 65.00m, MarginPercent = 45.8m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-7) },
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Sku = "SKU-1006", ProductName = "Noise-Cancelling Headphones", CurrentPrice = 199.99m, Cost = 110.00m, MarginPercent = 45.0m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-3) },
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Sku = "SKU-1007", ProductName = "External SSD 1TB", CurrentPrice = 89.99m, Cost = 50.00m, MarginPercent = 44.4m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-9) },
            new() { StoreId = "SEA-001", StoreName = "Downtown Flagship", Sku = "SKU-1008", ProductName = "Monitor 27-inch", CurrentPrice = 349.99m, Cost = 200.00m, MarginPercent = 42.9m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-6) },

            // Suburban Mall (PDX-002)
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1001", ProductName = "Wireless Mouse", CurrentPrice = 27.99m, Cost = 15.00m, MarginPercent = 46.4m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-11) },
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1002", ProductName = "USB-C Cable 6ft", CurrentPrice = 11.99m, Cost = 4.50m, MarginPercent = 62.5m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-9) },
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1003", ProductName = "Laptop Stand", CurrentPrice = 47.99m, Cost = 25.00m, MarginPercent = 47.9m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-13) },
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1004", ProductName = "Webcam 1080p", CurrentPrice = 74.99m, Cost = 40.00m, MarginPercent = 46.7m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-6) },
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1005", ProductName = "Mechanical Keyboard", CurrentPrice = 114.99m, Cost = 65.00m, MarginPercent = 43.5m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-8) },
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1006", ProductName = "Noise-Cancelling Headphones", CurrentPrice = 189.99m, Cost = 110.00m, MarginPercent = 42.1m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-4) },
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1007", ProductName = "External SSD 1TB", CurrentPrice = 84.99m, Cost = 50.00m, MarginPercent = 41.2m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-10) },
            new() { StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1008", ProductName = "Monitor 27-inch", CurrentPrice = 329.99m, Cost = 200.00m, MarginPercent = 39.4m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-7) },

            // Airport Terminal (SFO-003)
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Sku = "SKU-1001", ProductName = "Wireless Mouse", CurrentPrice = 32.99m, Cost = 15.00m, MarginPercent = 54.6m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-9) },
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Sku = "SKU-1002", ProductName = "USB-C Cable 6ft", CurrentPrice = 14.99m, Cost = 4.50m, MarginPercent = 70.0m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-7) },
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Sku = "SKU-1003", ProductName = "Laptop Stand", CurrentPrice = 54.99m, Cost = 25.00m, MarginPercent = 54.6m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-11) },
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Sku = "SKU-1004", ProductName = "Webcam 1080p", CurrentPrice = 84.99m, Cost = 40.00m, MarginPercent = 52.9m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-4) },
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Sku = "SKU-1005", ProductName = "Mechanical Keyboard", CurrentPrice = 129.99m, Cost = 65.00m, MarginPercent = 50.0m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-6) },
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Sku = "SKU-1006", ProductName = "Noise-Cancelling Headphones", CurrentPrice = 219.99m, Cost = 110.00m, MarginPercent = 50.0m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-2) },
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Sku = "SKU-1007", ProductName = "External SSD 1TB", CurrentPrice = 94.99m, Cost = 50.00m, MarginPercent = 47.4m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-8) },
            new() { StoreId = "SFO-003", StoreName = "Airport Terminal", Sku = "SKU-1008", ProductName = "Monitor 27-inch", CurrentPrice = 369.99m, Cost = 200.00m, MarginPercent = 45.9m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-5) },

            // University District (LAX-004)
            new() { StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1001", ProductName = "Wireless Mouse", CurrentPrice = 30.99m, Cost = 15.00m, MarginPercent = 51.6m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-10) },
            new() { StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1002", ProductName = "USB-C Cable 6ft", CurrentPrice = 13.99m, Cost = 4.50m, MarginPercent = 67.8m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-8) },
            new() { StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1003", ProductName = "Laptop Stand", CurrentPrice = 51.99m, Cost = 25.00m, MarginPercent = 51.9m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-12) },
            new() { StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1004", ProductName = "Webcam 1080p", CurrentPrice = 79.99m, Cost = 40.00m, MarginPercent = 50.0m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-5) },
            new() { StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1005", ProductName = "Mechanical Keyboard", CurrentPrice = 124.99m, Cost = 65.00m, MarginPercent = 48.0m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-7) },
            new() { StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1006", ProductName = "Noise-Cancelling Headphones", CurrentPrice = 209.99m, Cost = 110.00m, MarginPercent = 47.6m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-3) },
            new() { StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1007", ProductName = "External SSD 1TB", CurrentPrice = 89.99m, Cost = 50.00m, MarginPercent = 44.4m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-9) },
            new() { StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1008", ProductName = "Monitor 27-inch", CurrentPrice = 359.99m, Cost = 200.00m, MarginPercent = 44.4m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-6) },

            // Waterfront Plaza (DEN-005)
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Sku = "SKU-1001", ProductName = "Wireless Mouse", CurrentPrice = 28.99m, Cost = 15.00m, MarginPercent = 48.3m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-11) },
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Sku = "SKU-1002", ProductName = "USB-C Cable 6ft", CurrentPrice = 12.49m, Cost = 4.50m, MarginPercent = 64.0m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-9) },
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Sku = "SKU-1003", ProductName = "Laptop Stand", CurrentPrice = 48.99m, Cost = 25.00m, MarginPercent = 49.0m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-13) },
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Sku = "SKU-1004", ProductName = "Webcam 1080p", CurrentPrice = 76.99m, Cost = 40.00m, MarginPercent = 48.1m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-6) },
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Sku = "SKU-1005", ProductName = "Mechanical Keyboard", CurrentPrice = 117.99m, Cost = 65.00m, MarginPercent = 44.9m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-8) },
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Sku = "SKU-1006", ProductName = "Noise-Cancelling Headphones", CurrentPrice = 194.99m, Cost = 110.00m, MarginPercent = 43.6m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-4) },
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Sku = "SKU-1007", ProductName = "External SSD 1TB", CurrentPrice = 86.99m, Cost = 50.00m, MarginPercent = 42.5m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-10) },
            new() { StoreId = "DEN-005", StoreName = "Waterfront Plaza", Sku = "SKU-1008", ProductName = "Monitor 27-inch", CurrentPrice = 339.99m, Cost = 200.00m, MarginPercent = 41.2m, LastUpdated = DateTimeOffset.UtcNow.AddDays(-7) },
        };

        await _context.Inventory.AddRangeAsync(inventoryData, cancellationToken);
        await _context.Pricing.AddRangeAsync(pricingData, cancellationToken);

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
            "Database seeded successfully with {InventoryCount} inventory records, {PricingCount} pricing records, and {AuditCount} audit entries",
            inventoryData.Count,
            pricingData.Count,
            auditData.Count);
    }
}
