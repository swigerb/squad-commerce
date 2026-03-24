using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Observability;

namespace SquadCommerce.Mcp.Tools;

/// <summary>
/// MCP tool for querying inventory levels across stores.
/// Exposed to agents via the Model Context Protocol.
/// </summary>
/// <remarks>
/// This tool:
/// - Returns structured inventory data (NOT raw text)
/// - Filters by SKU or StoreId if provided
/// - Returns all inventory if no filter specified
/// - Validates parameters and returns structured errors
/// </remarks>
public sealed class GetInventoryLevelsTool
{
    private readonly IInventoryRepository _repository;
    private readonly ILogger<GetInventoryLevelsTool> _logger;

    public GetInventoryLevelsTool(
        IInventoryRepository repository,
        ILogger<GetInventoryLevelsTool> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tool name as registered with MCP server.
    /// </summary>
    public string Name => "GetInventoryLevels";

    /// <summary>
    /// Tool description for MCP discovery.
    /// </summary>
    public string Description => "Queries inventory levels for stores. Accepts optional 'sku' or 'storeId' parameters.";

    /// <summary>
    /// Executes the tool with the provided parameters.
    /// </summary>
    /// <param name="sku">Optional: Filter by SKU</param>
    /// <param name="storeId">Optional: Filter by store ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON-serializable inventory data with structured errors on failure</returns>
    public async Task<object> ExecuteAsync(string? sku = null, string? storeId = null, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        // Create MCP tool span
        var parameters = new { sku, storeId };
        using var activity = SquadCommerceTelemetry.StartToolSpan(Name, parameters);
        activity?.SetTag("mcp.tool.name", Name);
        
        // Record tool call count
        SquadCommerceTelemetry.McpToolCallCount.Add(1,
            new KeyValuePair<string, object?>("mcp.tool.name", Name));

        try
        {
            _logger.LogInformation(
                "GetInventoryLevels executing with sku={Sku}, storeId={StoreId}",
                sku ?? "(all)",
                storeId ?? "(all)");

            // Query by SKU if provided
            if (!string.IsNullOrWhiteSpace(sku))
            {
                var levels = await _repository.GetInventoryLevelsAsync(sku, cancellationToken);
                
                if (levels.Count == 0)
                {
                    _logger.LogWarning("No inventory found for SKU {Sku}", sku);
                    
                    // Record duration
                    var emptyDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                    SquadCommerceTelemetry.McpToolCallDuration.Record(emptyDuration,
                        new KeyValuePair<string, object?>("mcp.tool.name", Name));
                    
                    activity?.SetTag("mcp.result.count", 0);
                    
                    return new
                    {
                        Success = true,
                        Sku = sku,
                        Stores = Array.Empty<object>(),
                        Message = $"No inventory records found for SKU {sku}"
                    };
                }

                _logger.LogInformation("Found {Count} inventory records for SKU {Sku}", levels.Count, sku);
                
                // Record duration
                var skuDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(skuDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", Name));
                
                activity?.SetTag("mcp.result.count", levels.Count);
                
                return new
                {
                    Success = true,
                    Sku = sku,
                    Stores = levels.Select(l => new
                    {
                        l.StoreId,
                        l.Sku,
                        l.UnitsOnHand,
                        l.ReorderPoint,
                        l.UnitsOnOrder,
                        StockStatus = l.UnitsOnHand < l.ReorderPoint ? "Low" : l.UnitsOnHand < (l.ReorderPoint * 2) ? "Normal" : "High",
                        LastUpdated = l.LastUpdated
                    }).ToArray(),
                    TotalUnits = levels.Sum(l => l.UnitsOnHand),
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            // Query by store if provided
            if (!string.IsNullOrWhiteSpace(storeId))
            {
                // For store query, we need to query all SKUs for that store
                // Since the repository interface doesn't have a GetByStore method, we'll need to query all SKUs
                var allSkus = new[] { "SKU-1001", "SKU-1002", "SKU-1003", "SKU-1004", "SKU-1005", "SKU-1006", "SKU-1007", "SKU-1008" };
                var storeInventory = new List<object>();

                foreach (var skuToQuery in allSkus)
                {
                    var inventory = await _repository.GetInventoryForStoreAsync(storeId, skuToQuery, cancellationToken);
                    if (inventory != null)
                    {
                        storeInventory.Add(new
                        {
                            inventory.StoreId,
                            inventory.Sku,
                            inventory.UnitsOnHand,
                            inventory.ReorderPoint,
                            inventory.UnitsOnOrder,
                            StockStatus = inventory.UnitsOnHand < inventory.ReorderPoint ? "Low" : inventory.UnitsOnHand < (inventory.ReorderPoint * 2) ? "Normal" : "High",
                            LastUpdated = inventory.LastUpdated
                        });
                    }
                }

                if (storeInventory.Count == 0)
                {
                    _logger.LogWarning("No inventory found for StoreId {StoreId}", storeId);
                    
                    // Record duration
                    var storeDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                    SquadCommerceTelemetry.McpToolCallDuration.Record(storeDuration,
                        new KeyValuePair<string, object?>("mcp.tool.name", Name));
                    
                    activity?.SetTag("mcp.result.count", 0);
                    
                    return new
                    {
                        Success = true,
                        StoreId = storeId,
                        Inventory = Array.Empty<object>(),
                        Message = $"No inventory records found for store {storeId}"
                    };
                }

                _logger.LogInformation("Found {Count} inventory records for StoreId {StoreId}", storeInventory.Count, storeId);
                
                // Record duration
                var storeFoundDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(storeFoundDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", Name));
                
                activity?.SetTag("mcp.result.count", storeInventory.Count);
                
                return new
                {
                    Success = true,
                    StoreId = storeId,
                    Inventory = storeInventory.ToArray(),
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            // No filters - this would be expensive in production, but OK for demo
            _logger.LogWarning("GetInventoryLevels called without filters - returning limited results");
            
            // Record duration
            var noFilterDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.McpToolCallDuration.Record(noFilterDuration,
                new KeyValuePair<string, object?>("mcp.tool.name", Name));
            
            return new
            {
                Success = false,
                Error = "Please provide either 'sku' or 'storeId' parameter to filter results",
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetInventoryLevels");
            
            // Set error status on span
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            
            // Record duration even on error
            var errorDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.McpToolCallDuration.Record(errorDuration,
                new KeyValuePair<string, object?>("mcp.tool.name", Name));
            
            return new
            {
                Success = false,
                Error = $"Internal error: {ex.Message}",
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }
}
