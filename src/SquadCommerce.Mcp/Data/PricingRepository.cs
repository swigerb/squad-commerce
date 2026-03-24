namespace SquadCommerce.Mcp.Data;

/// <summary>
/// In-memory repository for pricing data.
/// Contains realistic demo data for 5 stores and 8 SKUs with pricing history.
/// </summary>
public interface IPricingRepository
{
    Task<IReadOnlyList<StorePricing>> GetPricingBySku(string sku, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StorePricing>> GetPricingByStore(string storeId, CancellationToken cancellationToken = default);
    Task<bool> UpdateStorePrice(string storeId, string sku, decimal newPrice, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceHistory>> GetPriceHistory(string sku, CancellationToken cancellationToken = default);
}

public sealed record StorePricing(
    string StoreId,
    string StoreName,
    string Sku,
    string ProductName,
    decimal CurrentPrice,
    decimal Cost,
    decimal MarginPercent,
    DateTimeOffset LastUpdated);

public sealed record PriceHistory(
    string Sku,
    decimal OldPrice,
    decimal NewPrice,
    string Reason,
    DateTimeOffset ChangedAt);

public sealed class PricingRepository : IPricingRepository
{
    private readonly List<StorePricing> _pricingData;
    private readonly List<PriceHistory> _priceHistory;

    public PricingRepository()
    {
        // Realistic demo data: 5 stores, 8 SKUs with cost and margin
        _pricingData = new List<StorePricing>
        {
            // Seattle Downtown
            new("SEA-001", "Seattle Downtown", "SKU-1001", "Wireless Mouse", 29.99m, 15.00m, 50.0m, DateTimeOffset.UtcNow.AddDays(-10)),
            new("SEA-001", "Seattle Downtown", "SKU-1002", "USB-C Cable 6ft", 12.99m, 4.50m, 65.4m, DateTimeOffset.UtcNow.AddDays(-8)),
            new("SEA-001", "Seattle Downtown", "SKU-1003", "Laptop Stand", 49.99m, 25.00m, 50.0m, DateTimeOffset.UtcNow.AddDays(-12)),
            new("SEA-001", "Seattle Downtown", "SKU-1004", "Webcam 1080p", 79.99m, 40.00m, 50.0m, DateTimeOffset.UtcNow.AddDays(-5)),
            new("SEA-001", "Seattle Downtown", "SKU-1005", "Mechanical Keyboard", 119.99m, 65.00m, 45.8m, DateTimeOffset.UtcNow.AddDays(-7)),
            new("SEA-001", "Seattle Downtown", "SKU-1006", "Noise-Cancelling Headphones", 199.99m, 110.00m, 45.0m, DateTimeOffset.UtcNow.AddDays(-3)),
            new("SEA-001", "Seattle Downtown", "SKU-1007", "External SSD 1TB", 89.99m, 50.00m, 44.4m, DateTimeOffset.UtcNow.AddDays(-9)),
            new("SEA-001", "Seattle Downtown", "SKU-1008", "Monitor 27-inch", 349.99m, 200.00m, 42.9m, DateTimeOffset.UtcNow.AddDays(-6)),

            // Portland Mall
            new("PDX-002", "Portland Mall", "SKU-1001", "Wireless Mouse", 27.99m, 15.00m, 46.4m, DateTimeOffset.UtcNow.AddDays(-11)),
            new("PDX-002", "Portland Mall", "SKU-1002", "USB-C Cable 6ft", 11.99m, 4.50m, 62.5m, DateTimeOffset.UtcNow.AddDays(-9)),
            new("PDX-002", "Portland Mall", "SKU-1003", "Laptop Stand", 47.99m, 25.00m, 47.9m, DateTimeOffset.UtcNow.AddDays(-13)),
            new("PDX-002", "Portland Mall", "SKU-1004", "Webcam 1080p", 74.99m, 40.00m, 46.7m, DateTimeOffset.UtcNow.AddDays(-6)),
            new("PDX-002", "Portland Mall", "SKU-1005", "Mechanical Keyboard", 114.99m, 65.00m, 43.5m, DateTimeOffset.UtcNow.AddDays(-8)),
            new("PDX-002", "Portland Mall", "SKU-1006", "Noise-Cancelling Headphones", 189.99m, 110.00m, 42.1m, DateTimeOffset.UtcNow.AddDays(-4)),
            new("PDX-002", "Portland Mall", "SKU-1007", "External SSD 1TB", 84.99m, 50.00m, 41.2m, DateTimeOffset.UtcNow.AddDays(-10)),
            new("PDX-002", "Portland Mall", "SKU-1008", "Monitor 27-inch", 329.99m, 200.00m, 39.4m, DateTimeOffset.UtcNow.AddDays(-7)),

            // San Francisco Union Square
            new("SFO-003", "San Francisco Union Square", "SKU-1001", "Wireless Mouse", 32.99m, 15.00m, 54.6m, DateTimeOffset.UtcNow.AddDays(-9)),
            new("SFO-003", "San Francisco Union Square", "SKU-1002", "USB-C Cable 6ft", 14.99m, 4.50m, 70.0m, DateTimeOffset.UtcNow.AddDays(-7)),
            new("SFO-003", "San Francisco Union Square", "SKU-1003", "Laptop Stand", 54.99m, 25.00m, 54.6m, DateTimeOffset.UtcNow.AddDays(-11)),
            new("SFO-003", "San Francisco Union Square", "SKU-1004", "Webcam 1080p", 84.99m, 40.00m, 52.9m, DateTimeOffset.UtcNow.AddDays(-4)),
            new("SFO-003", "San Francisco Union Square", "SKU-1005", "Mechanical Keyboard", 129.99m, 65.00m, 50.0m, DateTimeOffset.UtcNow.AddDays(-6)),
            new("SFO-003", "San Francisco Union Square", "SKU-1006", "Noise-Cancelling Headphones", 219.99m, 110.00m, 50.0m, DateTimeOffset.UtcNow.AddDays(-2)),
            new("SFO-003", "San Francisco Union Square", "SKU-1007", "External SSD 1TB", 94.99m, 50.00m, 47.4m, DateTimeOffset.UtcNow.AddDays(-8)),
            new("SFO-003", "San Francisco Union Square", "SKU-1008", "Monitor 27-inch", 369.99m, 200.00m, 45.9m, DateTimeOffset.UtcNow.AddDays(-5)),

            // Los Angeles Beverly Center
            new("LAX-004", "Los Angeles Beverly Center", "SKU-1001", "Wireless Mouse", 30.99m, 15.00m, 51.6m, DateTimeOffset.UtcNow.AddDays(-10)),
            new("LAX-004", "Los Angeles Beverly Center", "SKU-1002", "USB-C Cable 6ft", 13.99m, 4.50m, 67.8m, DateTimeOffset.UtcNow.AddDays(-8)),
            new("LAX-004", "Los Angeles Beverly Center", "SKU-1003", "Laptop Stand", 51.99m, 25.00m, 51.9m, DateTimeOffset.UtcNow.AddDays(-12)),
            new("LAX-004", "Los Angeles Beverly Center", "SKU-1004", "Webcam 1080p", 79.99m, 40.00m, 50.0m, DateTimeOffset.UtcNow.AddDays(-5)),
            new("LAX-004", "Los Angeles Beverly Center", "SKU-1005", "Mechanical Keyboard", 124.99m, 65.00m, 48.0m, DateTimeOffset.UtcNow.AddDays(-7)),
            new("LAX-004", "Los Angeles Beverly Center", "SKU-1006", "Noise-Cancelling Headphones", 209.99m, 110.00m, 47.6m, DateTimeOffset.UtcNow.AddDays(-3)),
            new("LAX-004", "Los Angeles Beverly Center", "SKU-1007", "External SSD 1TB", 89.99m, 50.00m, 44.4m, DateTimeOffset.UtcNow.AddDays(-9)),
            new("LAX-004", "Los Angeles Beverly Center", "SKU-1008", "Monitor 27-inch", 359.99m, 200.00m, 44.4m, DateTimeOffset.UtcNow.AddDays(-6)),

            // Denver Tech Center
            new("DEN-005", "Denver Tech Center", "SKU-1001", "Wireless Mouse", 28.99m, 15.00m, 48.3m, DateTimeOffset.UtcNow.AddDays(-11)),
            new("DEN-005", "Denver Tech Center", "SKU-1002", "USB-C Cable 6ft", 12.49m, 4.50m, 64.0m, DateTimeOffset.UtcNow.AddDays(-9)),
            new("DEN-005", "Denver Tech Center", "SKU-1003", "Laptop Stand", 48.99m, 25.00m, 49.0m, DateTimeOffset.UtcNow.AddDays(-13)),
            new("DEN-005", "Denver Tech Center", "SKU-1004", "Webcam 1080p", 76.99m, 40.00m, 48.1m, DateTimeOffset.UtcNow.AddDays(-6)),
            new("DEN-005", "Denver Tech Center", "SKU-1005", "Mechanical Keyboard", 117.99m, 65.00m, 44.9m, DateTimeOffset.UtcNow.AddDays(-8)),
            new("DEN-005", "Denver Tech Center", "SKU-1006", "Noise-Cancelling Headphones", 194.99m, 110.00m, 43.6m, DateTimeOffset.UtcNow.AddDays(-4)),
            new("DEN-005", "Denver Tech Center", "SKU-1007", "External SSD 1TB", 86.99m, 50.00m, 42.5m, DateTimeOffset.UtcNow.AddDays(-10)),
            new("DEN-005", "Denver Tech Center", "SKU-1008", "Monitor 27-inch", 339.99m, 200.00m, 41.2m, DateTimeOffset.UtcNow.AddDays(-7)),
        };

        _priceHistory = new List<PriceHistory>
        {
            new("SKU-1001", 31.99m, 29.99m, "Competitor price match", DateTimeOffset.UtcNow.AddDays(-10)),
            new("SKU-1003", 54.99m, 49.99m, "Seasonal promotion", DateTimeOffset.UtcNow.AddDays(-12)),
            new("SKU-1006", 229.99m, 199.99m, "Black Friday discount", DateTimeOffset.UtcNow.AddDays(-30)),
        };
    }

    public Task<IReadOnlyList<StorePricing>> GetPricingBySku(string sku, CancellationToken cancellationToken = default)
    {
        var results = _pricingData.Where(p => p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IReadOnlyList<StorePricing>>(results);
    }

    public Task<IReadOnlyList<StorePricing>> GetPricingByStore(string storeId, CancellationToken cancellationToken = default)
    {
        var results = _pricingData.Where(p => p.StoreId.Equals(storeId, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IReadOnlyList<StorePricing>>(results);
    }

    public Task<bool> UpdateStorePrice(string storeId, string sku, decimal newPrice, CancellationToken cancellationToken = default)
    {
        var pricing = _pricingData.FirstOrDefault(p =>
            p.StoreId.Equals(storeId, StringComparison.OrdinalIgnoreCase) &&
            p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));

        if (pricing == null)
            return Task.FromResult(false);

        // Record history
        _priceHistory.Add(new PriceHistory(sku, pricing.CurrentPrice, newPrice, "Price update", DateTimeOffset.UtcNow));

        // Update pricing
        var index = _pricingData.IndexOf(pricing);
        var newMargin = ((newPrice - pricing.Cost) / newPrice) * 100;
        _pricingData[index] = pricing with
        {
            CurrentPrice = newPrice,
            MarginPercent = Math.Round(newMargin, 1),
            LastUpdated = DateTimeOffset.UtcNow
        };

        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<PriceHistory>> GetPriceHistory(string sku, CancellationToken cancellationToken = default)
    {
        var results = _priceHistory.Where(h => h.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IReadOnlyList<PriceHistory>>(results);
    }
}
