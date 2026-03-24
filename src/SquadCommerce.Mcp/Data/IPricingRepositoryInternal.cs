namespace SquadCommerce.Mcp.Data;

/// <summary>
/// Internal interface for repository helper methods used by agents.
/// Not part of the public Contracts API.
/// </summary>
public interface IPricingRepositoryInternal
{
    /// <summary>
    /// Gets cost for margin calculations.
    /// </summary>
    Task<decimal?> GetCostAsync(string storeId, string sku, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pricing for a SKU across stores.
    /// </summary>
    Task<IReadOnlyList<StorePricing>> GetAllPricingForSkuAsync(string sku, CancellationToken cancellationToken = default);
}
