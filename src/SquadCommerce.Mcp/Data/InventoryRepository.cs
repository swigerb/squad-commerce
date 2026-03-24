using System.Collections.Concurrent;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Contracts.Models;

namespace SquadCommerce.Mcp.Data;

/// <summary>
/// Internal data model for inventory with store details.
/// </summary>
public sealed record InventoryLevel(
    string StoreId,
    string StoreName,
    string Sku,
    string ProductName,
    int QuantityOnHand,
    int ReorderThreshold,
    DateTimeOffset LastRestocked);

/// <summary>
/// In-memory repository for inventory data implementing Contracts interface.
/// Thread-safe with realistic demo data for 5 stores and 8 SKUs.
/// Renamed to InMemoryInventoryRepository for test usage.
/// </summary>
public sealed class InMemoryInventoryRepository : IInventoryRepository
{
    private readonly ConcurrentDictionary<string, InventoryLevel> _inventoryData;
    private static readonly Dictionary<string, string> StoreNames = new()
    {
        ["SEA-001"] = "Downtown Flagship",
        ["PDX-002"] = "Suburban Mall",
        ["SFO-003"] = "Airport Terminal",
        ["LAX-004"] = "University District",
        ["DEN-005"] = "Waterfront Plaza"
    };

    private static readonly Dictionary<string, string> ProductNames = new()
    {
        ["SKU-1001"] = "Wireless Mouse",
        ["SKU-1002"] = "USB-C Cable 6ft",
        ["SKU-1003"] = "Laptop Stand",
        ["SKU-1004"] = "Webcam 1080p",
        ["SKU-1005"] = "Mechanical Keyboard",
        ["SKU-1006"] = "Noise-Cancelling Headphones",
        ["SKU-1007"] = "External SSD 1TB",
        ["SKU-1008"] = "Monitor 27-inch"
    };

    public InMemoryInventoryRepository()
    {
        // Realistic demo data: 5 stores, 8 SKUs (40 records)
        // Thread-safe using ConcurrentDictionary
        var data = new List<InventoryLevel>
        {
            // Store: Downtown Flagship (SEA-001)
            new("SEA-001", "Downtown Flagship", "SKU-1001", "Wireless Mouse", 45, 20, DateTimeOffset.UtcNow.AddDays(-5)),
            new("SEA-001", "Downtown Flagship", "SKU-1002", "USB-C Cable 6ft", 120, 50, DateTimeOffset.UtcNow.AddDays(-3)),
            new("SEA-001", "Downtown Flagship", "SKU-1003", "Laptop Stand", 18, 10, DateTimeOffset.UtcNow.AddDays(-7)),
            new("SEA-001", "Downtown Flagship", "SKU-1004", "Webcam 1080p", 32, 15, DateTimeOffset.UtcNow.AddDays(-2)),
            new("SEA-001", "Downtown Flagship", "SKU-1005", "Mechanical Keyboard", 25, 12, DateTimeOffset.UtcNow.AddDays(-4)),
            new("SEA-001", "Downtown Flagship", "SKU-1006", "Noise-Cancelling Headphones", 15, 8, DateTimeOffset.UtcNow.AddDays(-1)),
            new("SEA-001", "Downtown Flagship", "SKU-1007", "External SSD 1TB", 8, 10, DateTimeOffset.UtcNow.AddDays(-6)),
            new("SEA-001", "Downtown Flagship", "SKU-1008", "Monitor 27-inch", 12, 5, DateTimeOffset.UtcNow.AddDays(-8)),

            // Store: Suburban Mall (PDX-002)
            new("PDX-002", "Suburban Mall", "SKU-1001", "Wireless Mouse", 38, 20, DateTimeOffset.UtcNow.AddDays(-4)),
            new("PDX-002", "Suburban Mall", "SKU-1002", "USB-C Cable 6ft", 95, 50, DateTimeOffset.UtcNow.AddDays(-6)),
            new("PDX-002", "Suburban Mall", "SKU-1003", "Laptop Stand", 22, 10, DateTimeOffset.UtcNow.AddDays(-5)),
            new("PDX-002", "Suburban Mall", "SKU-1004", "Webcam 1080p", 28, 15, DateTimeOffset.UtcNow.AddDays(-3)),
            new("PDX-002", "Suburban Mall", "SKU-1005", "Mechanical Keyboard", 19, 12, DateTimeOffset.UtcNow.AddDays(-7)),
            new("PDX-002", "Suburban Mall", "SKU-1006", "Noise-Cancelling Headphones", 11, 8, DateTimeOffset.UtcNow.AddDays(-2)),
            new("PDX-002", "Suburban Mall", "SKU-1007", "External SSD 1TB", 14, 10, DateTimeOffset.UtcNow.AddDays(-4)),
            new("PDX-002", "Suburban Mall", "SKU-1008", "Monitor 27-inch", 9, 5, DateTimeOffset.UtcNow.AddDays(-9)),

            // Store: Airport Terminal (SFO-003) - Some low stock
            new("SFO-003", "Airport Terminal", "SKU-1001", "Wireless Mouse", 52, 20, DateTimeOffset.UtcNow.AddDays(-2)),
            new("SFO-003", "Airport Terminal", "SKU-1002", "USB-C Cable 6ft", 140, 50, DateTimeOffset.UtcNow.AddDays(-1)),
            new("SFO-003", "Airport Terminal", "SKU-1003", "Laptop Stand", 6, 10, DateTimeOffset.UtcNow.AddDays(-10)),
            new("SFO-003", "Airport Terminal", "SKU-1004", "Webcam 1080p", 41, 15, DateTimeOffset.UtcNow.AddDays(-3)),
            new("SFO-003", "Airport Terminal", "SKU-1005", "Mechanical Keyboard", 30, 12, DateTimeOffset.UtcNow.AddDays(-5)),
            new("SFO-003", "Airport Terminal", "SKU-1006", "Noise-Cancelling Headphones", 20, 8, DateTimeOffset.UtcNow.AddDays(-4)),
            new("SFO-003", "Airport Terminal", "SKU-1007", "External SSD 1TB", 4, 10, DateTimeOffset.UtcNow.AddDays(-12)),
            new("SFO-003", "Airport Terminal", "SKU-1008", "Monitor 27-inch", 16, 5, DateTimeOffset.UtcNow.AddDays(-6)),

            // Store: University District (LAX-004) - Some critical low stock
            new("LAX-004", "University District", "SKU-1001", "Wireless Mouse", 29, 20, DateTimeOffset.UtcNow.AddDays(-6)),
            new("LAX-004", "University District", "SKU-1002", "USB-C Cable 6ft", 88, 50, DateTimeOffset.UtcNow.AddDays(-5)),
            new("LAX-004", "University District", "SKU-1003", "Laptop Stand", 15, 10, DateTimeOffset.UtcNow.AddDays(-4)),
            new("LAX-004", "University District", "SKU-1004", "Webcam 1080p", 12, 15, DateTimeOffset.UtcNow.AddDays(-8)),
            new("LAX-004", "University District", "SKU-1005", "Mechanical Keyboard", 23, 12, DateTimeOffset.UtcNow.AddDays(-3)),
            new("LAX-004", "University District", "SKU-1006", "Noise-Cancelling Headphones", 18, 8, DateTimeOffset.UtcNow.AddDays(-2)),
            new("LAX-004", "University District", "SKU-1007", "External SSD 1TB", 11, 10, DateTimeOffset.UtcNow.AddDays(-7)),
            new("LAX-004", "University District", "SKU-1008", "Monitor 27-inch", 7, 5, DateTimeOffset.UtcNow.AddDays(-10)),

            // Store: Waterfront Plaza (DEN-005)
            new("DEN-005", "Waterfront Plaza", "SKU-1001", "Wireless Mouse", 34, 20, DateTimeOffset.UtcNow.AddDays(-3)),
            new("DEN-005", "Waterfront Plaza", "SKU-1002", "USB-C Cable 6ft", 105, 50, DateTimeOffset.UtcNow.AddDays(-4)),
            new("DEN-005", "Waterfront Plaza", "SKU-1003", "Laptop Stand", 19, 10, DateTimeOffset.UtcNow.AddDays(-6)),
            new("DEN-005", "Waterfront Plaza", "SKU-1004", "Webcam 1080p", 25, 15, DateTimeOffset.UtcNow.AddDays(-5)),
            new("DEN-005", "Waterfront Plaza", "SKU-1005", "Mechanical Keyboard", 16, 12, DateTimeOffset.UtcNow.AddDays(-8)),
            new("DEN-005", "Waterfront Plaza", "SKU-1006", "Noise-Cancelling Headphones", 13, 8, DateTimeOffset.UtcNow.AddDays(-3)),
            new("DEN-005", "Waterfront Plaza", "SKU-1007", "External SSD 1TB", 9, 10, DateTimeOffset.UtcNow.AddDays(-9)),
            new("DEN-005", "Waterfront Plaza", "SKU-1008", "Monitor 27-inch", 10, 5, DateTimeOffset.UtcNow.AddDays(-7)),
        };

        _inventoryData = new ConcurrentDictionary<string, InventoryLevel>(
            data.Select(item => new KeyValuePair<string, InventoryLevel>($"{item.StoreId}:{item.Sku}", item)));
    }

    public Task<IReadOnlyList<InventorySnapshot>> GetInventoryLevelsAsync(string sku, CancellationToken cancellationToken = default)
    {
        var results = _inventoryData.Values
            .Where(i => i.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase))
            .Select(i => new InventorySnapshot
            {
                StoreId = i.StoreId,
                Sku = i.Sku,
                UnitsOnHand = i.QuantityOnHand,
                ReorderPoint = i.ReorderThreshold,
                UnitsOnOrder = 0, // Demo: no pending orders
                LastUpdated = i.LastRestocked
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<InventorySnapshot>>(results);
    }

    public Task<InventorySnapshot?> GetInventoryForStoreAsync(string storeId, string sku, CancellationToken cancellationToken = default)
    {
        var key = $"{storeId}:{sku}";
        if (!_inventoryData.TryGetValue(key, out var item))
        {
            return Task.FromResult<InventorySnapshot?>(null);
        }

        var snapshot = new InventorySnapshot
        {
            StoreId = item.StoreId,
            Sku = item.Sku,
            UnitsOnHand = item.QuantityOnHand,
            ReorderPoint = item.ReorderThreshold,
            UnitsOnOrder = 0,
            LastUpdated = item.LastRestocked
        };

        return Task.FromResult<InventorySnapshot?>(snapshot);
    }

    public Task<IReadOnlyList<InventorySnapshot>> GetBulkInventoryLevelsAsync(IReadOnlyList<string> skus, CancellationToken cancellationToken = default)
    {
        var results = _inventoryData.Values
            .Where(i => skus.Any(sku => sku.Equals(i.Sku, StringComparison.OrdinalIgnoreCase)))
            .Select(i => new InventorySnapshot
            {
                StoreId = i.StoreId,
                Sku = i.Sku,
                UnitsOnHand = i.QuantityOnHand,
                ReorderPoint = i.ReorderThreshold,
                UnitsOnOrder = 0,
                LastUpdated = i.LastRestocked
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<InventorySnapshot>>(results);
    }

    /// <summary>
    /// Gets store name by ID for enriching responses.
    /// </summary>
    public string GetStoreName(string storeId)
    {
        return StoreNames.TryGetValue(storeId, out var name) ? name : storeId;
    }

    /// <summary>
    /// Gets product name by SKU for enriching responses.
    /// </summary>
    public string GetProductName(string sku)
    {
        return ProductNames.TryGetValue(sku, out var name) ? name : sku;
    }
}
