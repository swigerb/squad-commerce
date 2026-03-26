using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Observability;

namespace SquadCommerce.Agents.Domain;

/// <summary>
/// InventoryAgent is responsible for querying store inventory levels via MCP.
/// It has read-only access to inventory data and generates A2UI payloads
/// (RetailStockHeatmap) for visualization.
/// </summary>
/// <remarks>
/// Allowed tools: ["GetInventoryLevels"]
/// Required scope: SquadCommerce.Inventory.Read
/// Protocol: MCP
/// </remarks>
public sealed class InventoryAgent : IDomainAgent
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<InventoryAgent> _logger;

    public string AgentName => "InventoryAgent";

    public InventoryAgent(
        IInventoryRepository inventoryRepository,
        ILogger<InventoryAgent> logger)
    {
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes inventory query for a SKU and builds A2UI heatmap payload.
    /// </summary>
    /// <param name="sku">Product SKU to query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent result with RetailStockHeatmap A2UI payload</returns>
    public async Task<AgentResult> ExecuteAsync(string sku, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        // Create agent invocation span
        using var activity = SquadCommerceTelemetry.StartAgentSpan(AgentName, "Execute");
        activity?.SetTag("agent.name", AgentName);
        activity?.SetTag("agent.protocol", "MCP");
        activity?.SetTag("agent.sku", sku);
        
        // Record invocation count
        SquadCommerceTelemetry.AgentInvocationCount.Add(1, 
            new KeyValuePair<string, object?>("agent.name", AgentName));

        _logger.LogInformation("InventoryAgent executing for SKU: {Sku}", sku);

        try
        {
            // Query inventory levels via repository (which backs the MCP tool)
            var inventoryLevels = await _inventoryRepository.GetInventoryLevelsAsync(sku, cancellationToken);

            if (inventoryLevels.Count == 0)
            {
                _logger.LogWarning("No inventory found for SKU {Sku}", sku);
                return new AgentResult
                {
                    TextSummary = $"No inventory records found for SKU {sku}",
                    Success = false,
                    ErrorMessage = $"SKU {sku} not found in inventory system",
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            // Build A2UI payload for RetailStockHeatmap
            var storeStockLevels = inventoryLevels.Select(inv => new StoreStockLevel
            {
                StoreId = inv.StoreId,
                StoreName = GetStoreName(inv.StoreId),
                UnitsOnHand = inv.UnitsOnHand,
                ReorderPoint = inv.ReorderPoint,
                StockStatus = CalculateStockStatus(inv.UnitsOnHand, inv.ReorderPoint)
            }).ToList();

            var a2uiPayload = new RetailStockHeatmapData
            {
                Sku = sku,
                Stores = storeStockLevels,
                Timestamp = DateTimeOffset.UtcNow
            };

            // Generate text summary
            var totalUnits = inventoryLevels.Sum(i => i.UnitsOnHand);
            var lowStockCount = storeStockLevels.Count(s => s.StockStatus == "Low");
            var textSummary = $"SKU {sku}: {totalUnits} total units across {inventoryLevels.Count} stores. " +
                              $"{lowStockCount} store(s) below reorder point.";

            _logger.LogInformation(
                "InventoryAgent completed: {TotalUnits} units, {LowStock} low-stock stores",
                totalUnits,
                lowStockCount);

            // Record A2UI payload emission
            SquadCommerceTelemetry.A2UIPayloadCount.Add(1,
                new KeyValuePair<string, object?>("a2ui.component", "RetailStockHeatmap"));

            // Record invocation duration
            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", AgentName));

            return new AgentResult
            {
                TextSummary = textSummary,
                A2UIPayload = a2uiPayload,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InventoryAgent failed for SKU {Sku}", sku);
            
            // Set error status on span
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            
            // Record duration even on error
            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", AgentName));
            
            return new AgentResult
            {
                TextSummary = $"Error querying inventory for SKU {sku}",
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    /// Executes bulk inventory query for multiple SKUs and builds consolidated A2UI heatmap payload.
    /// </summary>
    /// <param name="skus">List of product SKUs to query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent result with consolidated RetailStockHeatmap A2UI payload</returns>
    public async Task<AgentResult> ExecuteBulkAsync(IReadOnlyList<string> skus, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        using var activity = SquadCommerceTelemetry.StartAgentSpan(AgentName, "ExecuteBulk");
        activity?.SetTag("agent.name", AgentName);
        activity?.SetTag("agent.protocol", "MCP");
        activity?.SetTag("agent.sku_count", skus.Count);
        
        SquadCommerceTelemetry.AgentInvocationCount.Add(1, 
            new KeyValuePair<string, object?>("agent.name", AgentName));

        _logger.LogInformation("InventoryAgent executing bulk query for {Count} SKUs", skus.Count);

        try
        {
            var inventoryLevels = await _inventoryRepository.GetBulkInventoryLevelsAsync(skus, cancellationToken);

            if (inventoryLevels.Count == 0)
            {
                _logger.LogWarning("No inventory found for {Count} SKUs", skus.Count);
                return new AgentResult
                {
                    TextSummary = $"No inventory records found for {skus.Count} SKUs",
                    Success = false,
                    ErrorMessage = $"No SKUs found in inventory system",
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            var allStoreStockLevels = inventoryLevels.Select(inv => new StoreStockLevel
            {
                StoreId = inv.StoreId,
                StoreName = GetStoreName(inv.StoreId),
                UnitsOnHand = inv.UnitsOnHand,
                ReorderPoint = inv.ReorderPoint,
                StockStatus = CalculateStockStatus(inv.UnitsOnHand, inv.ReorderPoint)
            }).ToList();

            var a2uiPayload = new RetailStockHeatmapData
            {
                Sku = string.Join(", ", skus.Take(3)) + (skus.Count > 3 ? $" (+{skus.Count - 3} more)" : ""),
                Stores = allStoreStockLevels,
                Timestamp = DateTimeOffset.UtcNow
            };

            var totalUnits = inventoryLevels.Sum(i => i.UnitsOnHand);
            var lowStockCount = allStoreStockLevels.Count(s => s.StockStatus == "Low");
            var textSummary = $"{skus.Count} SKUs: {totalUnits} total units across {inventoryLevels.Select(i => i.StoreId).Distinct().Count()} stores. " +
                              $"{lowStockCount} store-SKU combinations below reorder point.";

            _logger.LogInformation("InventoryAgent bulk completed: {TotalUnits} units, {LowStock} low-stock combinations", totalUnits, lowStockCount);

            SquadCommerceTelemetry.A2UIPayloadCount.Add(1,
                new KeyValuePair<string, object?>("a2ui.component", "RetailStockHeatmap"));

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", AgentName));

            return new AgentResult
            {
                TextSummary = textSummary,
                A2UIPayload = a2uiPayload,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InventoryAgent bulk query failed for {Count} SKUs", skus.Count);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            
            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", AgentName));
            
            return new AgentResult
            {
                TextSummary = $"Error querying inventory for {skus.Count} SKUs",
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    private static string CalculateStockStatus(int unitsOnHand, int reorderPoint)
    {
        if (unitsOnHand < reorderPoint)
            return "Low";
        if (unitsOnHand < reorderPoint * 2)
            return "Normal";
        return "High";
    }

    private static string GetStoreName(string storeId) => storeId switch
    {
        "SEA-001" => "Downtown Flagship",
        "PDX-002" => "Suburban Mall",
        "SFO-003" => "Airport Terminal",
        "LAX-004" => "University District",
        "DEN-005" => "Waterfront Plaza",
        "NYC-006" => "Times Square Flagship",
        "BOS-007" => "Back Bay Mall",
        "PHI-008" => "Center City Plaza",
        "MIA-009" => "Miami Flagship",
        "TPA-010" => "Tampa Gateway",
        "ORL-011" => "Orlando Resort District",
        "ATL-012" => "Peachtree Center",
        _ => storeId
    };
}
