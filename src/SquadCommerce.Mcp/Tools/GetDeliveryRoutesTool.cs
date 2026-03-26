using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Observability;

namespace SquadCommerce.Mcp.Tools;

/// <summary>
/// MCP tool for computing delivery rerouting options between stores.
/// Finds stores with surplus stock and suggests transfer routes to at-risk stores.
/// </summary>
/// <remarks>
/// This tool:
/// - Uses IInventoryRepository to find stores with excess stock
/// - Computes rerouting options from surplus stores to deficit stores
/// - Returns routes with simulated distances, transfer quantities, and priority
/// </remarks>
[McpServerToolType]
public sealed class GetDeliveryRoutesTool
{
    private const string ToolName = "GetDeliveryRoutes";

    private readonly IInventoryRepository _repository;
    private readonly ILogger<GetDeliveryRoutesTool> _logger;

    public GetDeliveryRoutesTool(
        IInventoryRepository repository,
        ILogger<GetDeliveryRoutesTool> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Computes available rerouting options for a SKU between stores.
    /// </summary>
    [McpServerTool(Name = "GetDeliveryRoutes"), Description("Computes rerouting options for a SKU. Finds stores with surplus stock and suggests transfers to at-risk stores.")]
    public async Task<object> ExecuteAsync(
        [Description("Product SKU to find rerouting options for (e.g. SKU-2001)")] string sku,
        [Description("Optional: Source region to prioritize (e.g. Southeast)")] string? sourceRegion = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        var parameters = new { sku, sourceRegion };
        using var activity = SquadCommerceTelemetry.StartToolSpan(ToolName, parameters);
        activity?.SetTag("mcp.tool.name", ToolName);
        activity?.SetTag("mcp.sku", sku);

        SquadCommerceTelemetry.McpToolCallCount.Add(1,
            new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

        try
        {
            if (string.IsNullOrWhiteSpace(sku))
            {
                _logger.LogWarning("GetDeliveryRoutes called without sku");
                var valDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(valDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));
                return new { Success = false, Error = "sku is required" };
            }

            _logger.LogInformation("GetDeliveryRoutes executing for sku={Sku}, sourceRegion={SourceRegion}",
                sku, sourceRegion ?? "(all)");

            var inventoryLevels = await _repository.GetInventoryLevelsAsync(sku, cancellationToken);

            if (inventoryLevels.Count == 0)
            {
                _logger.LogWarning("No inventory data found for SKU {Sku}", sku);
                var noInvDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(noInvDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));
                return new
                {
                    Success = false,
                    Error = $"No inventory data found for SKU {sku}",
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            // Classify stores: surplus (above 2x reorder point) vs at-risk (below reorder point)
            var surplusStores = inventoryLevels
                .Where(i => i.UnitsOnHand > i.ReorderPoint * 2)
                .OrderByDescending(i => i.UnitsOnHand)
                .ToList();

            var atRiskStores = inventoryLevels
                .Where(i => i.UnitsOnHand <= i.ReorderPoint)
                .OrderBy(i => i.UnitsOnHand)
                .ToList();

            if (surplusStores.Count == 0 || atRiskStores.Count == 0)
            {
                _logger.LogInformation("No rerouting needed: {Surplus} surplus stores, {AtRisk} at-risk stores",
                    surplusStores.Count, atRiskStores.Count);

                var noRouteDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(noRouteDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

                return new
                {
                    Success = true,
                    Sku = sku,
                    Routes = Array.Empty<object>(),
                    Message = surplusStores.Count == 0
                        ? "No stores with surplus stock available for rerouting"
                        : "No stores currently at risk — no rerouting needed",
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            // Build routes: match surplus stores to at-risk stores
            var routes = new List<object>();
            foreach (var dest in atRiskStores)
            {
                var bestSource = surplusStores
                    .OrderBy(s => GetSimulatedDistance(s.StoreId, dest.StoreId))
                    .FirstOrDefault();

                if (bestSource == null) continue;

                var transferable = Math.Min(
                    bestSource.UnitsOnHand - bestSource.ReorderPoint,
                    dest.ReorderPoint - dest.UnitsOnHand + 10);
                transferable = Math.Max(transferable, 5);

                var distance = GetSimulatedDistance(bestSource.StoreId, dest.StoreId);
                var priority = dest.UnitsOnHand == 0 ? "Critical"
                             : dest.UnitsOnHand < dest.ReorderPoint / 2 ? "High"
                             : "Medium";

                routes.Add(new
                {
                    SourceStoreId = bestSource.StoreId,
                    SourceStoreName = GetStoreName(bestSource.StoreId),
                    DestStoreId = dest.StoreId,
                    DestStoreName = GetStoreName(dest.StoreId),
                    UnitsToTransfer = transferable,
                    Priority = priority,
                    DistanceMiles = distance,
                    EstimatedHours = (int)(distance / 45) + 2 // ~45 mph avg + 2hr loading
                });
            }

            _logger.LogInformation("GetDeliveryRoutes found {Count} routes for SKU {Sku}",
                routes.Count, sku);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.McpToolCallDuration.Record(duration,
                new KeyValuePair<string, object?>("mcp.tool.name", ToolName));
            activity?.SetTag("mcp.result.count", routes.Count);

            return new
            {
                Success = true,
                Sku = sku,
                Routes = routes.ToArray(),
                SurplusStoreCount = surplusStores.Count,
                AtRiskStoreCount = atRiskStores.Count,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetDeliveryRoutes");

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            var errorDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.McpToolCallDuration.Record(errorDuration,
                new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

            return new
            {
                Success = false,
                Error = $"Internal error: {ex.Message}",
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    /// Simulated distances (miles) between store pairs based on geographic proximity.
    /// </summary>
    private static double GetSimulatedDistance(string sourceId, string destId)
    {
        // Deterministic simulated distances based on store pair hash
        var pair = string.Compare(sourceId, destId, StringComparison.Ordinal) < 0
            ? $"{sourceId}-{destId}"
            : $"{destId}-{sourceId}";

        return pair switch
        {
            "MIA-009-TPA-010" => 280,
            "MIA-009-ORL-011" => 235,
            "ORL-011-TPA-010" => 85,
            "ATL-012-MIA-009" => 660,
            "ATL-012-ORL-011" => 440,
            "ATL-012-TPA-010" => 460,
            "BOS-007-NYC-006" => 215,
            "NYC-006-PHI-008" => 95,
            "BOS-007-PHI-008" => 310,
            "DEN-005-SEA-001" => 1320,
            "LAX-004-SFO-003" => 380,
            "PDX-002-SEA-001" => 175,
            "DEN-005-LAX-004" => 1020,
            "DEN-005-SFO-003" => 1235,
            "LAX-004-PDX-002" => 960,
            "LAX-004-SEA-001" => 1135,
            "SFO-003-SEA-001" => 810,
            _ => 500 + Math.Abs(pair.GetHashCode() % 800) // Fallback: 500-1300 miles
        };
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
