using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Observability;

namespace SquadCommerce.Agents.Domain;

/// <summary>
/// RedistributionAgent is responsible for finding stores with surplus stock and
/// calculating optimal rerouting to affected stores. Builds A2UI ReroutingMapData
/// with source/destination stores, quantities, and priority.
/// </summary>
/// <remarks>
/// Allowed tools: ["GetDeliveryRoutes"]
/// Required scope: SquadCommerce.Logistics.Write
/// Protocol: MCP
/// </remarks>
public sealed class RedistributionAgent : IDomainAgent
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<RedistributionAgent> _logger;

    public string AgentName => "RedistributionAgent";

    public RedistributionAgent(
        IInventoryRepository inventoryRepository,
        ILogger<RedistributionAgent> logger)
    {
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Finds stores with surplus stock and calculates optimal rerouting to affected regions.
    /// Builds a transfer plan with ReroutingMapData A2UI payload.
    /// </summary>
    /// <param name="sku">Product SKU to redistribute</param>
    /// <param name="affectedRegions">Regions/store IDs that are at risk</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent result with ReroutingMapData A2UI payload showing transfer plan</returns>
    public async Task<AgentResult> ExecuteAsync(
        string sku,
        string[] affectedRegions,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        using var activity = SquadCommerceTelemetry.StartAgentSpan(AgentName, "Execute");
        activity?.SetTag("agent.name", AgentName);
        activity?.SetTag("agent.protocol", "MCP");
        activity?.SetTag("agent.sku", sku);
        activity?.SetTag("agent.affected_regions", string.Join(",", affectedRegions));

        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", AgentName));

        _logger.LogInformation(
            "RedistributionAgent executing for SKU: {Sku}, AffectedRegions: {Regions}",
            sku, string.Join(", ", affectedRegions));

        try
        {
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

            // Map regions to store IDs for matching
            var regionStoreMapping = GetRegionStoreMapping();
            var affectedStoreIds = affectedRegions
                .SelectMany(r => regionStoreMapping.TryGetValue(r, out var stores) ? stores : new[] { r })
                .ToHashSet();

            // Classify stores
            var surplusStores = inventoryLevels
                .Where(i => i.UnitsOnHand > i.ReorderPoint * 2 && !affectedStoreIds.Contains(i.StoreId))
                .OrderByDescending(i => i.UnitsOnHand - i.ReorderPoint)
                .ToList();

            var atRiskStores = inventoryLevels
                .Where(i => i.UnitsOnHand <= i.ReorderPoint || affectedStoreIds.Contains(i.StoreId))
                .OrderBy(i => i.UnitsOnHand)
                .ToList();

            if (surplusStores.Count == 0)
            {
                _logger.LogWarning("No surplus stores available for SKU {Sku}", sku);

                var noSurplusDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.AgentInvocationDuration.Record(noSurplusDuration,
                    new KeyValuePair<string, object?>("agent.name", AgentName));

                return new AgentResult
                {
                    TextSummary = $"SKU {sku}: No stores with surplus stock available for redistribution. " +
                                  $"All {inventoryLevels.Count} stores are at or below optimal levels.",
                    Success = true,
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            // Build optimal transfer routes
            var routes = new List<ReroutingRoute>();
            var totalUnitsTransferred = 0;

            foreach (var dest in atRiskStores)
            {
                // Find closest surplus store
                var bestSource = surplusStores
                    .Where(s => s.UnitsOnHand - s.ReorderPoint > 5)
                    .OrderBy(s => GetSimulatedDistance(s.StoreId, dest.StoreId))
                    .FirstOrDefault();

                if (bestSource == null) continue;

                // Calculate transfer quantity: bring dest up to reorder point + safety buffer
                var deficit = Math.Max(dest.ReorderPoint - dest.UnitsOnHand + 10, 5);
                var available = bestSource.UnitsOnHand - bestSource.ReorderPoint - 5;
                var transferQty = Math.Min(deficit, Math.Max(available, 0));

                if (transferQty <= 0) continue;

                var distance = GetSimulatedDistance(bestSource.StoreId, dest.StoreId);
                var priority = dest.UnitsOnHand == 0 ? "Critical"
                             : dest.UnitsOnHand < dest.ReorderPoint / 2 ? "High"
                             : dest.UnitsOnHand < dest.ReorderPoint ? "Medium" : "Low";

                routes.Add(new ReroutingRoute
                {
                    SourceStoreId = bestSource.StoreId,
                    SourceStoreName = GetStoreName(bestSource.StoreId),
                    DestStoreId = dest.StoreId,
                    DestStoreName = GetStoreName(dest.StoreId),
                    UnitsToTransfer = transferQty,
                    Priority = priority,
                    DistanceMiles = distance,
                    EstimatedHours = (int)(distance / 45) + 2
                });

                totalUnitsTransferred += transferQty;
            }

            // Calculate overall risk
            var criticalRoutes = routes.Count(r => r.Priority is "Critical" or "High");
            var overallRisk = routes.Count > 0
                ? Math.Clamp((double)criticalRoutes / routes.Count, 0.0, 1.0)
                : 0.0;

            var a2uiPayload = new ReroutingMapData
            {
                Sku = sku,
                ProductName = GetProductName(sku),
                Routes = routes,
                OverallRiskScore = Math.Round(overallRisk, 2),
                DelayDays = 0, // Redistribution doesn't have delay — it's the solution
                DelayReason = $"Redistribution plan for {string.Join(", ", affectedRegions)}",
                Timestamp = DateTimeOffset.UtcNow
            };

            var textSummary = $"SKU {sku} redistribution plan: {routes.Count} transfer route(s) identified. " +
                              $"{totalUnitsTransferred} total units to redistribute from {surplusStores.Count} surplus store(s) " +
                              $"to {atRiskStores.Count} at-risk store(s). " +
                              $"{criticalRoutes} critical/high priority transfer(s).";

            _logger.LogInformation(
                "RedistributionAgent completed: {RouteCount} routes, {TotalUnits} units to transfer, {Critical} critical",
                routes.Count, totalUnitsTransferred, criticalRoutes);

            SquadCommerceTelemetry.A2UIPayloadCount.Add(1,
                new KeyValuePair<string, object?>("a2ui.component", "ReroutingMap"));

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
            _logger.LogError(ex, "RedistributionAgent failed for SKU {Sku}", sku);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", AgentName));

            return new AgentResult
            {
                TextSummary = $"Error calculating redistribution for SKU {sku}",
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    private static Dictionary<string, string[]> GetRegionStoreMapping() => new()
    {
        ["Southeast"] = new[] { "MIA-009", "TPA-010", "ORL-011", "ATL-012" },
        ["Northeast"] = new[] { "NYC-006", "BOS-007", "PHI-008" },
        ["Northwest"] = new[] { "SEA-001", "PDX-002" },
        ["Southwest"] = new[] { "LAX-004", "DEN-005" },
        ["West Coast"] = new[] { "SEA-001", "PDX-002", "SFO-003", "LAX-004" },
        ["East Coast"] = new[] { "NYC-006", "BOS-007", "PHI-008", "MIA-009" },
        ["Midwest"] = new[] { "DEN-005" }
    };

    private static double GetSimulatedDistance(string sourceId, string destId)
    {
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
            _ => 500 + Math.Abs(pair.GetHashCode() % 800)
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

    private static string GetProductName(string sku) => sku switch
    {
        "SKU-1001" => "Wireless Mouse",
        "SKU-1002" => "USB-C Cable 6ft",
        "SKU-1003" => "Laptop Stand",
        "SKU-1004" => "Webcam 1080p",
        "SKU-1005" => "Mechanical Keyboard",
        "SKU-1006" => "Noise-Cancelling Headphones",
        "SKU-1007" => "External SSD 1TB",
        "SKU-1008" => "Monitor 27-inch",
        "SKU-2001" => "Organic Fair Trade Coffee",
        "SKU-2002" => "Dark Chocolate Bar 72% Cocoa",
        "SKU-2003" => "Cocoa Powder Premium",
        "SKU-2004" => "Hot Chocolate Mix",
        "SKU-3001" => "Classic Straight Denim",
        "SKU-3002" => "Classic Boot-Cut Denim",
        "SKU-3003" => "Denim Jacket Classic",
        "SKU-3004" => "Canvas Belt",
        _ => sku
    };
}
