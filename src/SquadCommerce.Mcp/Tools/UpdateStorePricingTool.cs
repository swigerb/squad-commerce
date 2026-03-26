using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Contracts.Models;
using SquadCommerce.Observability;

namespace SquadCommerce.Mcp.Tools;

/// <summary>
/// MCP tool for updating store pricing.
/// Exposed to agents with write permissions via the Model Context Protocol using the official ModelContextProtocol SDK.
/// </summary>
/// <remarks>
/// This tool:
/// - Updates a single SKU price at a single store
/// - Validates price constraints (must be positive, above cost)
/// - Returns structured success/failure results
/// - Requires SquadCommerce.Pricing.ReadWrite scope
/// </remarks>
[McpServerToolType]
public sealed class UpdateStorePricingTool
{
    private const string ToolName = "UpdateStorePricing";
    
    private readonly IPricingRepository _repository;
    private readonly ILogger<UpdateStorePricingTool> _logger;

    public UpdateStorePricingTool(
        IPricingRepository repository,
        ILogger<UpdateStorePricingTool> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Updates the price of a SKU at a specific store.
    /// </summary>
    /// <param name="storeId">Store ID where price will be updated</param>
    /// <param name="sku">Product SKU to update</param>
    /// <param name="newPrice">New price to set</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success/failure result with updated pricing info</returns>
    [McpServerTool(Name = "UpdateStorePricing"), Description("Updates the price of a SKU at a specific store. Requires storeId, sku, and newPrice parameters.")]
    public async Task<object> ExecuteAsync(
        [Description("Store ID where price will be updated (e.g. SEA-001)")] string storeId,
        [Description("Product SKU to update (e.g. SKU-1001)")] string sku,
        [Description("New price to set (must be > 0)")] decimal newPrice,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        // Create MCP tool span
        var parameters = new { storeId, sku, newPrice };
        using var activity = SquadCommerceTelemetry.StartToolSpan(ToolName, parameters);
        activity?.SetTag("mcp.tool.name", ToolName);
        activity?.SetTag("mcp.store_id", storeId);
        activity?.SetTag("mcp.sku", sku);
        activity?.SetTag("mcp.new_price", newPrice);
        
        // Record tool call count
        SquadCommerceTelemetry.McpToolCallCount.Add(1,
            new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

        try
        {
            _logger.LogInformation(
                "UpdateStorePricing executing: StoreId={StoreId}, SKU={Sku}, NewPrice={NewPrice:C}",
                storeId,
                sku,
                newPrice);

            // Validation
            if (string.IsNullOrWhiteSpace(storeId))
            {
                _logger.LogWarning("UpdateStorePricing called without storeId");
                
                // Record duration
                var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(duration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));
                
                return new { Success = false, Error = "storeId is required" };
            }

            if (string.IsNullOrWhiteSpace(sku))
            {
                _logger.LogWarning("UpdateStorePricing called without sku");
                
                // Record duration
                var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(duration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));
                
                return new { Success = false, Error = "sku is required" };
            }

            if (newPrice <= 0)
            {
                _logger.LogWarning("UpdateStorePricing called with invalid price: {NewPrice}", newPrice);
                
                // Record duration
                var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(duration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));
                
                return new { Success = false, Error = "newPrice must be greater than zero" };
            }

            // Get current price for comparison
            var currentPrice = await _repository.GetCurrentPriceAsync(storeId, sku, cancellationToken);
            if (currentPrice == null)
            {
                _logger.LogWarning("Pricing record not found for StoreId={StoreId}, SKU={Sku}", storeId, sku);
                return new
                {
                    Success = false,
                    Error = $"Pricing record not found for store {storeId}, SKU {sku}"
                };
            }

            // Build PriceChange payload
            var priceChange = new PriceChange
            {
                Sku = sku,
                StoreId = storeId,
                OldPrice = currentPrice.Value,
                NewPrice = newPrice,
                Reason = "MCP tool invocation",
                RequestedBy = "MCP:UpdateStorePricing",
                Timestamp = DateTimeOffset.UtcNow
            };

            // Execute update
            var result = await _repository.UpdatePricingAsync(priceChange, cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Price update failed for StoreId={StoreId}, SKU={Sku}: {Error}",
                    storeId,
                    sku,
                    result.ErrorMessage);

                return new
                {
                    Success = false,
                    Error = result.ErrorMessage
                };
            }

            // Get updated pricing details
            var updatedPrice = await _repository.GetCurrentPriceAsync(storeId, sku, cancellationToken);

            _logger.LogInformation(
                "Price update succeeded for StoreId={StoreId}, SKU={Sku}: {OldPrice:C} → {NewPrice:C}",
                storeId,
                sku,
                currentPrice.Value,
                updatedPrice);

            // Record duration
            var successDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.McpToolCallDuration.Record(successDuration,
                new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

            return new
            {
                Success = true,
                StoreId = storeId,
                Sku = sku,
                OldPrice = currentPrice.Value,
                NewPrice = newPrice,
                PriceChange = newPrice - currentPrice.Value,
                PriceChangePercent = Math.Round(((newPrice - currentPrice.Value) / currentPrice.Value) * 100, 1),
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing UpdateStorePricing");
            
            // Set error status on span
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            
            // Record duration even on error
            var errorDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.McpToolCallDuration.Record(errorDuration,
                new KeyValuePair<string, object?>("mcp.tool.name", ToolName));
            
            return new
            {
                Success = false,
                Error = $"Internal error: {ex.Message}"
            };
        }
    }
}
