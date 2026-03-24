using Microsoft.EntityFrameworkCore;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Contracts.Models;

namespace SquadCommerce.Mcp.Data;

/// <summary>
/// SQLite-backed implementation of IInventoryRepository using EF Core.
/// Thread-safe via EF Core scoped lifetime.
/// </summary>
public sealed class SqliteInventoryRepository : IInventoryRepository
{
    private readonly SquadCommerceDbContext _context;

    public SqliteInventoryRepository(SquadCommerceDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<InventorySnapshot>> GetInventoryLevelsAsync(string sku, CancellationToken cancellationToken = default)
    {
        var query = _context.Inventory
            .Where(i => i.Sku.ToLower() == sku.ToLower())
            .Select(i => new InventorySnapshot
            {
                StoreId = i.StoreId,
                Sku = i.Sku,
                UnitsOnHand = i.QuantityOnHand,
                ReorderPoint = i.ReorderThreshold,
                UnitsOnOrder = 0, // Demo: no pending orders
                LastUpdated = i.LastRestocked
            });

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<InventorySnapshot?> GetInventoryForStoreAsync(string storeId, string sku, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Inventory
            .FirstOrDefaultAsync(i => i.StoreId == storeId && i.Sku.ToLower() == sku.ToLower(), cancellationToken);

        if (entity == null)
        {
            return null;
        }

        return new InventorySnapshot
        {
            StoreId = entity.StoreId,
            Sku = entity.Sku,
            UnitsOnHand = entity.QuantityOnHand,
            ReorderPoint = entity.ReorderThreshold,
            UnitsOnOrder = 0,
            LastUpdated = entity.LastRestocked
        };
    }

    /// <summary>
    /// Gets store name by ID for enriching responses.
    /// </summary>
    public async Task<string> GetStoreNameAsync(string storeId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Inventory
            .Where(i => i.StoreId == storeId)
            .Select(i => i.StoreName)
            .FirstOrDefaultAsync(cancellationToken);

        return entity ?? storeId;
    }

    /// <summary>
    /// Gets product name by SKU for enriching responses.
    /// </summary>
    public async Task<string> GetProductNameAsync(string sku, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Inventory
            .Where(i => i.Sku.ToLower() == sku.ToLower())
            .Select(i => i.ProductName)
            .FirstOrDefaultAsync(cancellationToken);

        return entity ?? sku;
    }

    /// <summary>
    /// Gets inventory levels for multiple SKUs across all stores (bulk operation).
    /// </summary>
    public async Task<IReadOnlyList<InventorySnapshot>> GetBulkInventoryLevelsAsync(IReadOnlyList<string> skus, CancellationToken cancellationToken = default)
    {
        var skuLower = skus.Select(s => s.ToLower()).ToList();
        
        var query = _context.Inventory
            .Where(i => skuLower.Contains(i.Sku.ToLower()))
            .Select(i => new InventorySnapshot
            {
                StoreId = i.StoreId,
                Sku = i.Sku,
                UnitsOnHand = i.QuantityOnHand,
                ReorderPoint = i.ReorderThreshold,
                UnitsOnOrder = 0,
                LastUpdated = i.LastRestocked
            });

        return await query.ToListAsync(cancellationToken);
    }
}
