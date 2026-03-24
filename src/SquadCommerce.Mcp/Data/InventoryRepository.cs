namespace SquadCommerce.Mcp.Data;

/// <summary>
/// In-memory repository for inventory data.
/// Contains realistic demo data for 10 stores and 8 SKUs.
/// </summary>
public interface IInventoryRepository
{
    Task<IReadOnlyList<InventoryLevel>> GetInventoryLevelsBySku(string sku, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryLevel>> GetInventoryLevelsByStore(string storeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryLevel>> GetAllInventoryLevels(CancellationToken cancellationToken = default);
}

public sealed record InventoryLevel(
    string StoreId,
    string StoreName,
    string Sku,
    string ProductName,
    int QuantityOnHand,
    int ReorderThreshold,
    DateTimeOffset LastRestocked);

public sealed class InventoryRepository : IInventoryRepository
{
    private readonly List<InventoryLevel> _inventoryData;

    public InventoryRepository()
    {
        // Realistic demo data: 10 stores, 8 SKUs
        _inventoryData = new List<InventoryLevel>
        {
            // Store: Seattle Downtown
            new("SEA-001", "Seattle Downtown", "SKU-1001", "Wireless Mouse", 45, 20, DateTimeOffset.UtcNow.AddDays(-5)),
            new("SEA-001", "Seattle Downtown", "SKU-1002", "USB-C Cable 6ft", 120, 50, DateTimeOffset.UtcNow.AddDays(-3)),
            new("SEA-001", "Seattle Downtown", "SKU-1003", "Laptop Stand", 18, 10, DateTimeOffset.UtcNow.AddDays(-7)),
            new("SEA-001", "Seattle Downtown", "SKU-1004", "Webcam 1080p", 32, 15, DateTimeOffset.UtcNow.AddDays(-2)),
            new("SEA-001", "Seattle Downtown", "SKU-1005", "Mechanical Keyboard", 25, 12, DateTimeOffset.UtcNow.AddDays(-4)),
            new("SEA-001", "Seattle Downtown", "SKU-1006", "Noise-Cancelling Headphones", 15, 8, DateTimeOffset.UtcNow.AddDays(-1)),
            new("SEA-001", "Seattle Downtown", "SKU-1007", "External SSD 1TB", 8, 10, DateTimeOffset.UtcNow.AddDays(-6)),
            new("SEA-001", "Seattle Downtown", "SKU-1008", "Monitor 27-inch", 12, 5, DateTimeOffset.UtcNow.AddDays(-8)),

            // Store: Portland Mall
            new("PDX-002", "Portland Mall", "SKU-1001", "Wireless Mouse", 38, 20, DateTimeOffset.UtcNow.AddDays(-4)),
            new("PDX-002", "Portland Mall", "SKU-1002", "USB-C Cable 6ft", 95, 50, DateTimeOffset.UtcNow.AddDays(-6)),
            new("PDX-002", "Portland Mall", "SKU-1003", "Laptop Stand", 22, 10, DateTimeOffset.UtcNow.AddDays(-5)),
            new("PDX-002", "Portland Mall", "SKU-1004", "Webcam 1080p", 28, 15, DateTimeOffset.UtcNow.AddDays(-3)),
            new("PDX-002", "Portland Mall", "SKU-1005", "Mechanical Keyboard", 19, 12, DateTimeOffset.UtcNow.AddDays(-7)),
            new("PDX-002", "Portland Mall", "SKU-1006", "Noise-Cancelling Headphones", 11, 8, DateTimeOffset.UtcNow.AddDays(-2)),
            new("PDX-002", "Portland Mall", "SKU-1007", "External SSD 1TB", 14, 10, DateTimeOffset.UtcNow.AddDays(-4)),
            new("PDX-002", "Portland Mall", "SKU-1008", "Monitor 27-inch", 9, 5, DateTimeOffset.UtcNow.AddDays(-9)),

            // Store: San Francisco Union Square
            new("SFO-003", "San Francisco Union Square", "SKU-1001", "Wireless Mouse", 52, 20, DateTimeOffset.UtcNow.AddDays(-2)),
            new("SFO-003", "San Francisco Union Square", "SKU-1002", "USB-C Cable 6ft", 140, 50, DateTimeOffset.UtcNow.AddDays(-1)),
            new("SFO-003", "San Francisco Union Square", "SKU-1003", "Laptop Stand", 6, 10, DateTimeOffset.UtcNow.AddDays(-10)),
            new("SFO-003", "San Francisco Union Square", "SKU-1004", "Webcam 1080p", 41, 15, DateTimeOffset.UtcNow.AddDays(-3)),
            new("SFO-003", "San Francisco Union Square", "SKU-1005", "Mechanical Keyboard", 30, 12, DateTimeOffset.UtcNow.AddDays(-5)),
            new("SFO-003", "San Francisco Union Square", "SKU-1006", "Noise-Cancelling Headphones", 20, 8, DateTimeOffset.UtcNow.AddDays(-4)),
            new("SFO-003", "San Francisco Union Square", "SKU-1007", "External SSD 1TB", 4, 10, DateTimeOffset.UtcNow.AddDays(-12)),
            new("SFO-003", "San Francisco Union Square", "SKU-1008", "Monitor 27-inch", 16, 5, DateTimeOffset.UtcNow.AddDays(-6)),

            // Store: Los Angeles Beverly Center
            new("LAX-004", "Los Angeles Beverly Center", "SKU-1001", "Wireless Mouse", 29, 20, DateTimeOffset.UtcNow.AddDays(-6)),
            new("LAX-004", "Los Angeles Beverly Center", "SKU-1002", "USB-C Cable 6ft", 88, 50, DateTimeOffset.UtcNow.AddDays(-5)),
            new("LAX-004", "Los Angeles Beverly Center", "SKU-1003", "Laptop Stand", 15, 10, DateTimeOffset.UtcNow.AddDays(-4)),
            new("LAX-004", "Los Angeles Beverly Center", "SKU-1004", "Webcam 1080p", 12, 15, DateTimeOffset.UtcNow.AddDays(-8)),
            new("LAX-004", "Los Angeles Beverly Center", "SKU-1005", "Mechanical Keyboard", 23, 12, DateTimeOffset.UtcNow.AddDays(-3)),
            new("LAX-004", "Los Angeles Beverly Center", "SKU-1006", "Noise-Cancelling Headphones", 18, 8, DateTimeOffset.UtcNow.AddDays(-2)),
            new("LAX-004", "Los Angeles Beverly Center", "SKU-1007", "External SSD 1TB", 11, 10, DateTimeOffset.UtcNow.AddDays(-7)),
            new("LAX-004", "Los Angeles Beverly Center", "SKU-1008", "Monitor 27-inch", 7, 5, DateTimeOffset.UtcNow.AddDays(-10)),

            // Store: Denver Tech Center
            new("DEN-005", "Denver Tech Center", "SKU-1001", "Wireless Mouse", 34, 20, DateTimeOffset.UtcNow.AddDays(-3)),
            new("DEN-005", "Denver Tech Center", "SKU-1002", "USB-C Cable 6ft", 105, 50, DateTimeOffset.UtcNow.AddDays(-4)),
            new("DEN-005", "Denver Tech Center", "SKU-1003", "Laptop Stand", 19, 10, DateTimeOffset.UtcNow.AddDays(-6)),
            new("DEN-005", "Denver Tech Center", "SKU-1004", "Webcam 1080p", 25, 15, DateTimeOffset.UtcNow.AddDays(-5)),
            new("DEN-005", "Denver Tech Center", "SKU-1005", "Mechanical Keyboard", 16, 12, DateTimeOffset.UtcNow.AddDays(-8)),
            new("DEN-005", "Denver Tech Center", "SKU-1006", "Noise-Cancelling Headphones", 13, 8, DateTimeOffset.UtcNow.AddDays(-3)),
            new("DEN-005", "Denver Tech Center", "SKU-1007", "External SSD 1TB", 9, 10, DateTimeOffset.UtcNow.AddDays(-9)),
            new("DEN-005", "Denver Tech Center", "SKU-1008", "Monitor 27-inch", 10, 5, DateTimeOffset.UtcNow.AddDays(-7)),
        };
    }

    public Task<IReadOnlyList<InventoryLevel>> GetInventoryLevelsBySku(string sku, CancellationToken cancellationToken = default)
    {
        var results = _inventoryData.Where(i => i.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IReadOnlyList<InventoryLevel>>(results);
    }

    public Task<IReadOnlyList<InventoryLevel>> GetInventoryLevelsByStore(string storeId, CancellationToken cancellationToken = default)
    {
        var results = _inventoryData.Where(i => i.StoreId.Equals(storeId, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IReadOnlyList<InventoryLevel>>(results);
    }

    public Task<IReadOnlyList<InventoryLevel>> GetAllInventoryLevels(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<InventoryLevel>>(_inventoryData);
    }
}
