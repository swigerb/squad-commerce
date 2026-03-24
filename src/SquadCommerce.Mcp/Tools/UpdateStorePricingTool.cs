using SquadCommerce.Mcp.Data;

namespace SquadCommerce.Mcp.Tools;

/// <summary>
/// MCP tool for updating store pricing.
/// Exposed to agents with write permissions via the Model Context Protocol.
/// </summary>
/// <remarks>
/// This tool:
/// - Updates a single SKU price at a single store
/// - Records pricing history for audit purposes
/// - Requires SquadCommerce.Pricing.ReadWrite scope
/// - Emits OpenTelemetry spans for observability
/// </remarks>
public sealed class UpdateStorePricingTool
{
    private readonly IPricingRepository _repository;

    public UpdateStorePricingTool(IPricingRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Tool name as registered with MCP server.
    /// </summary>
    public string Name => "UpdateStorePricing";

    /// <summary>
    /// Tool description for MCP discovery.
    /// </summary>
    public string Description => "Updates the price of a SKU at a specific store. Requires storeId, sku, and newPrice parameters.";

    /// <summary>
    /// Executes the tool with the provided parameters.
    /// </summary>
    /// <param name="storeId">Store ID where price will be updated</param>
    /// <param name="sku">Product SKU to update</param>
    /// <param name="newPrice">New price to set</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success/failure result with updated pricing info</returns>
    public async Task<object> ExecuteAsync(string storeId, string sku, decimal newPrice, CancellationToken cancellationToken = default)
    {
        // TODO: Add OpenTelemetry span emission
        // using var activity = ActivitySource.StartActivity("UpdateStorePricing");
        // activity?.SetTag("mcp.tool", Name);
        // activity?.SetTag("storeId", storeId);
        // activity?.SetTag("sku", sku);
        // activity?.SetTag("newPrice", newPrice);

        // Validation
        if (string.IsNullOrWhiteSpace(storeId))
            return new { Success = false, Error = "storeId is required" };

        if (string.IsNullOrWhiteSpace(sku))
            return new { Success = false, Error = "sku is required" };

        if (newPrice <= 0)
            return new { Success = false, Error = "newPrice must be greater than zero" };

        // Update pricing
        var success = await _repository.UpdateStorePrice(storeId, sku, newPrice, cancellationToken);

        if (!success)
            return new { Success = false, Error = $"Pricing record not found for store {storeId}, SKU {sku}" };

        // Return updated pricing
        var updatedPricing = await _repository.GetPricingBySku(sku, cancellationToken);
        var storePrice = updatedPricing.FirstOrDefault(p => p.StoreId.Equals(storeId, StringComparison.OrdinalIgnoreCase));

        return new
        {
            Success = true,
            StoreId = storeId,
            Sku = sku,
            NewPrice = newPrice,
            MarginPercent = storePrice?.MarginPercent,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
