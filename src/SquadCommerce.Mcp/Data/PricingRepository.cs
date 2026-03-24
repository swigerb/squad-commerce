using System.Collections.Concurrent;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Contracts.Models;

namespace SquadCommerce.Mcp.Data;

/// <summary>
/// Internal data model for pricing with cost and margin details.
/// </summary>
public sealed record StorePricing(
    string StoreId,
    string StoreName,
    string Sku,
    string ProductName,
    decimal CurrentPrice,
    decimal Cost,
    decimal MarginPercent,
    DateTimeOffset LastUpdated);

/// <summary>
/// In-memory repository for pricing data implementing Contracts interface.
/// Thread-safe with realistic demo data for 5 stores and 8 SKUs.
/// Renamed to InMemoryPricingRepository for test usage.
/// </summary>
public sealed class InMemoryPricingRepository : IPricingRepository, IPricingRepositoryInternal
{
    private readonly ConcurrentDictionary<string, StorePricing> _pricingData;

    public InMemoryPricingRepository()
    {
        // Realistic demo data: 5 stores, 8 SKUs with cost and margin (40 records)
        var data = new List<StorePricing>
        {
            // Downtown Flagship (SEA-001)
            new("SEA-001", "Downtown Flagship", "SKU-1001", "Wireless Mouse", 29.99m, 15.00m, 50.0m, DateTimeOffset.UtcNow.AddDays(-10)),
            new("SEA-001", "Downtown Flagship", "SKU-1002", "USB-C Cable 6ft", 12.99m, 4.50m, 65.4m, DateTimeOffset.UtcNow.AddDays(-8)),
            new("SEA-001", "Downtown Flagship", "SKU-1003", "Laptop Stand", 49.99m, 25.00m, 50.0m, DateTimeOffset.UtcNow.AddDays(-12)),
            new("SEA-001", "Downtown Flagship", "SKU-1004", "Webcam 1080p", 79.99m, 40.00m, 50.0m, DateTimeOffset.UtcNow.AddDays(-5)),
            new("SEA-001", "Downtown Flagship", "SKU-1005", "Mechanical Keyboard", 119.99m, 65.00m, 45.8m, DateTimeOffset.UtcNow.AddDays(-7)),
            new("SEA-001", "Downtown Flagship", "SKU-1006", "Noise-Cancelling Headphones", 199.99m, 110.00m, 45.0m, DateTimeOffset.UtcNow.AddDays(-3)),
            new("SEA-001", "Downtown Flagship", "SKU-1007", "External SSD 1TB", 89.99m, 50.00m, 44.4m, DateTimeOffset.UtcNow.AddDays(-9)),
            new("SEA-001", "Downtown Flagship", "SKU-1008", "Monitor 27-inch", 349.99m, 200.00m, 42.9m, DateTimeOffset.UtcNow.AddDays(-6)),

            // Suburban Mall (PDX-002)
            new("PDX-002", "Suburban Mall", "SKU-1001", "Wireless Mouse", 27.99m, 15.00m, 46.4m, DateTimeOffset.UtcNow.AddDays(-11)),
            new("PDX-002", "Suburban Mall", "SKU-1002", "USB-C Cable 6ft", 11.99m, 4.50m, 62.5m, DateTimeOffset.UtcNow.AddDays(-9)),
            new("PDX-002", "Suburban Mall", "SKU-1003", "Laptop Stand", 47.99m, 25.00m, 47.9m, DateTimeOffset.UtcNow.AddDays(-13)),
            new("PDX-002", "Suburban Mall", "SKU-1004", "Webcam 1080p", 74.99m, 40.00m, 46.7m, DateTimeOffset.UtcNow.AddDays(-6)),
            new("PDX-002", "Suburban Mall", "SKU-1005", "Mechanical Keyboard", 114.99m, 65.00m, 43.5m, DateTimeOffset.UtcNow.AddDays(-8)),
            new("PDX-002", "Suburban Mall", "SKU-1006", "Noise-Cancelling Headphones", 189.99m, 110.00m, 42.1m, DateTimeOffset.UtcNow.AddDays(-4)),
            new("PDX-002", "Suburban Mall", "SKU-1007", "External SSD 1TB", 84.99m, 50.00m, 41.2m, DateTimeOffset.UtcNow.AddDays(-10)),
            new("PDX-002", "Suburban Mall", "SKU-1008", "Monitor 27-inch", 329.99m, 200.00m, 39.4m, DateTimeOffset.UtcNow.AddDays(-7)),

            // Airport Terminal (SFO-003)
            new("SFO-003", "Airport Terminal", "SKU-1001", "Wireless Mouse", 32.99m, 15.00m, 54.6m, DateTimeOffset.UtcNow.AddDays(-9)),
            new("SFO-003", "Airport Terminal", "SKU-1002", "USB-C Cable 6ft", 14.99m, 4.50m, 70.0m, DateTimeOffset.UtcNow.AddDays(-7)),
            new("SFO-003", "Airport Terminal", "SKU-1003", "Laptop Stand", 54.99m, 25.00m, 54.6m, DateTimeOffset.UtcNow.AddDays(-11)),
            new("SFO-003", "Airport Terminal", "SKU-1004", "Webcam 1080p", 84.99m, 40.00m, 52.9m, DateTimeOffset.UtcNow.AddDays(-4)),
            new("SFO-003", "Airport Terminal", "SKU-1005", "Mechanical Keyboard", 129.99m, 65.00m, 50.0m, DateTimeOffset.UtcNow.AddDays(-6)),
            new("SFO-003", "Airport Terminal", "SKU-1006", "Noise-Cancelling Headphones", 219.99m, 110.00m, 50.0m, DateTimeOffset.UtcNow.AddDays(-2)),
            new("SFO-003", "Airport Terminal", "SKU-1007", "External SSD 1TB", 94.99m, 50.00m, 47.4m, DateTimeOffset.UtcNow.AddDays(-8)),
            new("SFO-003", "Airport Terminal", "SKU-1008", "Monitor 27-inch", 369.99m, 200.00m, 45.9m, DateTimeOffset.UtcNow.AddDays(-5)),

            // University District (LAX-004)
            new("LAX-004", "University District", "SKU-1001", "Wireless Mouse", 30.99m, 15.00m, 51.6m, DateTimeOffset.UtcNow.AddDays(-10)),
            new("LAX-004", "University District", "SKU-1002", "USB-C Cable 6ft", 13.99m, 4.50m, 67.8m, DateTimeOffset.UtcNow.AddDays(-8)),
            new("LAX-004", "University District", "SKU-1003", "Laptop Stand", 51.99m, 25.00m, 51.9m, DateTimeOffset.UtcNow.AddDays(-12)),
            new("LAX-004", "University District", "SKU-1004", "Webcam 1080p", 79.99m, 40.00m, 50.0m, DateTimeOffset.UtcNow.AddDays(-5)),
            new("LAX-004", "University District", "SKU-1005", "Mechanical Keyboard", 124.99m, 65.00m, 48.0m, DateTimeOffset.UtcNow.AddDays(-7)),
            new("LAX-004", "University District", "SKU-1006", "Noise-Cancelling Headphones", 209.99m, 110.00m, 47.6m, DateTimeOffset.UtcNow.AddDays(-3)),
            new("LAX-004", "University District", "SKU-1007", "External SSD 1TB", 89.99m, 50.00m, 44.4m, DateTimeOffset.UtcNow.AddDays(-9)),
            new("LAX-004", "University District", "SKU-1008", "Monitor 27-inch", 359.99m, 200.00m, 44.4m, DateTimeOffset.UtcNow.AddDays(-6)),

            // Waterfront Plaza (DEN-005)
            new("DEN-005", "Waterfront Plaza", "SKU-1001", "Wireless Mouse", 28.99m, 15.00m, 48.3m, DateTimeOffset.UtcNow.AddDays(-11)),
            new("DEN-005", "Waterfront Plaza", "SKU-1002", "USB-C Cable 6ft", 12.49m, 4.50m, 64.0m, DateTimeOffset.UtcNow.AddDays(-9)),
            new("DEN-005", "Waterfront Plaza", "SKU-1003", "Laptop Stand", 48.99m, 25.00m, 49.0m, DateTimeOffset.UtcNow.AddDays(-13)),
            new("DEN-005", "Waterfront Plaza", "SKU-1004", "Webcam 1080p", 76.99m, 40.00m, 48.1m, DateTimeOffset.UtcNow.AddDays(-6)),
            new("DEN-005", "Waterfront Plaza", "SKU-1005", "Mechanical Keyboard", 117.99m, 65.00m, 44.9m, DateTimeOffset.UtcNow.AddDays(-8)),
            new("DEN-005", "Waterfront Plaza", "SKU-1006", "Noise-Cancelling Headphones", 194.99m, 110.00m, 43.6m, DateTimeOffset.UtcNow.AddDays(-4)),
            new("DEN-005", "Waterfront Plaza", "SKU-1007", "External SSD 1TB", 86.99m, 50.00m, 42.5m, DateTimeOffset.UtcNow.AddDays(-10)),
            new("DEN-005", "Waterfront Plaza", "SKU-1008", "Monitor 27-inch", 339.99m, 200.00m, 41.2m, DateTimeOffset.UtcNow.AddDays(-7)),
        };

        _pricingData = new ConcurrentDictionary<string, StorePricing>(
            data.Select(item => new KeyValuePair<string, StorePricing>($"{item.StoreId}:{item.Sku}", item)));
    }

    public Task<PricingUpdateResult> UpdatePricingAsync(PriceChange priceChange, CancellationToken cancellationToken = default)
    {
        var key = $"{priceChange.StoreId}:{priceChange.Sku}";

        if (!_pricingData.TryGetValue(key, out var existing))
        {
            return Task.FromResult(new PricingUpdateResult
            {
                Sku = priceChange.Sku,
                StoresUpdated = Array.Empty<string>(),
                Success = false,
                ErrorMessage = $"Pricing record not found for store {priceChange.StoreId}, SKU {priceChange.Sku}",
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        // Validate new price
        if (priceChange.NewPrice <= 0)
        {
            return Task.FromResult(new PricingUpdateResult
            {
                Sku = priceChange.Sku,
                StoresUpdated = Array.Empty<string>(),
                Success = false,
                ErrorMessage = "New price must be greater than zero",
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        // Check if new price is below cost (loss scenario)
        if (priceChange.NewPrice < existing.Cost)
        {
            return Task.FromResult(new PricingUpdateResult
            {
                Sku = priceChange.Sku,
                StoresUpdated = Array.Empty<string>(),
                Success = false,
                ErrorMessage = $"New price ${priceChange.NewPrice:F2} is below cost ${existing.Cost:F2}",
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        // Update pricing atomically
        var newMargin = ((priceChange.NewPrice - existing.Cost) / priceChange.NewPrice) * 100;
        var updated = existing with
        {
            CurrentPrice = priceChange.NewPrice,
            MarginPercent = Math.Round(newMargin, 1),
            LastUpdated = DateTimeOffset.UtcNow
        };

        _pricingData[key] = updated;

        return Task.FromResult(new PricingUpdateResult
        {
            Sku = priceChange.Sku,
            StoresUpdated = new[] { priceChange.StoreId },
            Success = true,
            ErrorMessage = null,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public Task<decimal?> GetCurrentPriceAsync(string storeId, string sku, CancellationToken cancellationToken = default)
    {
        var key = $"{storeId}:{sku}";
        if (_pricingData.TryGetValue(key, out var pricing))
        {
            return Task.FromResult<decimal?>(pricing.CurrentPrice);
        }
        return Task.FromResult<decimal?>(null);
    }

    /// <summary>
    /// Gets cost for margin calculations (internal helper).
    /// </summary>
    public Task<decimal?> GetCostAsync(string storeId, string sku, CancellationToken cancellationToken = default)
    {
        var key = $"{storeId}:{sku}";
        decimal? result = _pricingData.TryGetValue(key, out var pricing) ? pricing.Cost : null;
        return Task.FromResult(result);
    }

    /// <summary>
    /// Gets all pricing for a SKU across stores (internal helper).
    /// </summary>
    public Task<IReadOnlyList<StorePricing>> GetAllPricingForSkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var results = _pricingData.Values
            .Where(p => p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IReadOnlyList<StorePricing>>(results);
    }

    public Task<IReadOnlyDictionary<string, decimal>> GetBulkPricingAsync(IReadOnlyList<string> skus, CancellationToken cancellationToken = default)
    {
        var results = _pricingData.Values
            .Where(p => skus.Any(sku => sku.Equals(p.Sku, StringComparison.OrdinalIgnoreCase)))
            .GroupBy(p => p.Sku)
            .ToDictionary(g => g.Key, g => g.Average(p => p.CurrentPrice));

        return Task.FromResult<IReadOnlyDictionary<string, decimal>>(results);
    }
}
