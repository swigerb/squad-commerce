using Microsoft.EntityFrameworkCore;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Contracts.Models;

namespace SquadCommerce.Mcp.Data;

/// <summary>
/// SQLite-backed implementation of IPricingRepository using EF Core.
/// Thread-safe via EF Core scoped lifetime.
/// </summary>
public sealed class SqlitePricingRepository : IPricingRepository, IPricingRepositoryInternal
{
    private readonly SquadCommerceDbContext _context;

    public SqlitePricingRepository(SquadCommerceDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PricingUpdateResult> UpdatePricingAsync(PriceChange priceChange, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Pricing
            .FirstOrDefaultAsync(p => p.StoreId == priceChange.StoreId && p.Sku.ToLower() == priceChange.Sku.ToLower(), cancellationToken);

        if (entity == null)
        {
            return new PricingUpdateResult
            {
                Sku = priceChange.Sku,
                StoresUpdated = Array.Empty<string>(),
                Success = false,
                ErrorMessage = $"Pricing record not found for store {priceChange.StoreId}, SKU {priceChange.Sku}",
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        // Validate new price
        if (priceChange.NewPrice <= 0)
        {
            return new PricingUpdateResult
            {
                Sku = priceChange.Sku,
                StoresUpdated = Array.Empty<string>(),
                Success = false,
                ErrorMessage = "New price must be greater than zero",
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        // Check if new price is below cost (loss scenario)
        if (priceChange.NewPrice < entity.Cost)
        {
            return new PricingUpdateResult
            {
                Sku = priceChange.Sku,
                StoresUpdated = Array.Empty<string>(),
                Success = false,
                ErrorMessage = $"New price ${priceChange.NewPrice:F2} is below cost ${entity.Cost:F2}",
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        // Update pricing
        var newMargin = ((priceChange.NewPrice - entity.Cost) / priceChange.NewPrice) * 100;
        entity.CurrentPrice = priceChange.NewPrice;
        entity.MarginPercent = Math.Round(newMargin, 1);
        entity.LastUpdated = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new PricingUpdateResult
        {
            Sku = priceChange.Sku,
            StoresUpdated = new[] { priceChange.StoreId },
            Success = true,
            ErrorMessage = null,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public async Task<decimal?> GetCurrentPriceAsync(string storeId, string sku, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Pricing
            .Where(p => p.StoreId == storeId && p.Sku.ToLower() == sku.ToLower())
            .Select(p => p.CurrentPrice)
            .FirstOrDefaultAsync(cancellationToken);

        return entity == 0 ? null : entity;
    }

    /// <summary>
    /// Gets cost for margin calculations (internal helper).
    /// </summary>
    public async Task<decimal?> GetCostAsync(string storeId, string sku, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Pricing
            .Where(p => p.StoreId == storeId && p.Sku.ToLower() == sku.ToLower())
            .Select(p => p.Cost)
            .FirstOrDefaultAsync(cancellationToken);

        return entity == 0 ? null : entity;
    }

    /// <summary>
    /// Gets all pricing for a SKU across stores (internal helper).
    /// </summary>
    public async Task<IReadOnlyList<StorePricing>> GetAllPricingForSkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var entities = await _context.Pricing
            .Where(p => p.Sku.ToLower() == sku.ToLower())
            .ToListAsync(cancellationToken);

        return entities.Select(p => new StorePricing(
            p.StoreId,
            p.StoreName,
            p.Sku,
            p.ProductName,
            p.CurrentPrice,
            p.Cost,
            p.MarginPercent,
            p.LastUpdated
        )).ToList();
    }
}
