using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Observability;

namespace SquadCommerce.Agents.Domain;

/// <summary>
/// LogisticsAgent is responsible for analyzing delayed shipments and calculating
/// the impact on affected stores. Builds A2UI ReroutingMapData showing risk.
/// </summary>
/// <remarks>
/// Allowed tools: ["GetShipmentStatus"]
/// Required scope: SquadCommerce.Logistics.Read
/// Protocol: MCP
/// </remarks>
public sealed class LogisticsAgent : IDomainAgent
{
    private readonly SquadCommerceDbContext _dbContext;
    private readonly ILogger<LogisticsAgent> _logger;

    public string AgentName => "LogisticsAgent";

    public LogisticsAgent(
        SquadCommerceDbContext dbContext,
        ILogger<LogisticsAgent> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Analyzes delayed shipments for a SKU, calculates store impact and stock runway,
    /// and builds an A2UI ReroutingMapData payload showing risk.
    /// </summary>
    /// <param name="sku">Product SKU to analyze</param>
    /// <param name="delayDays">Number of delay days</param>
    /// <param name="reason">Reason for the delay</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent result with ReroutingMapData A2UI payload</returns>
    public async Task<AgentResult> ExecuteAsync(
        string sku,
        int delayDays,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        using var activity = SquadCommerceTelemetry.StartAgentSpan(AgentName, "Execute");
        activity?.SetTag("agent.name", AgentName);
        activity?.SetTag("agent.protocol", "MCP");
        activity?.SetTag("agent.sku", sku);
        activity?.SetTag("agent.delay_days", delayDays);

        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", AgentName));

        _logger.LogInformation(
            "LogisticsAgent executing for SKU: {Sku}, DelayDays: {DelayDays}, Reason: {Reason}",
            sku, delayDays, reason);

        try
        {
            // Query delayed shipments for this SKU
            var delayedShipments = await _dbContext.Shipments
                .Where(s => s.Sku == sku && s.Status == "Delayed")
                .ToListAsync(cancellationToken);

            // Also get all shipments for full picture
            var allShipments = await _dbContext.Shipments
                .Where(s => s.Sku == sku)
                .ToListAsync(cancellationToken);

            if (allShipments.Count == 0)
            {
                _logger.LogWarning("No shipments found for SKU {Sku}", sku);
                return new AgentResult
                {
                    TextSummary = $"No shipment records found for SKU {sku}",
                    Success = false,
                    ErrorMessage = $"SKU {sku} not found in shipment system",
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            // Get inventory at destination stores to calculate stock runway
            var affectedDestStores = delayedShipments.Select(s => s.DestStoreId).Distinct().ToList();
            var inventoryAtRisk = await _dbContext.Inventory
                .Where(i => i.Sku == sku && affectedDestStores.Contains(i.StoreId))
                .ToListAsync(cancellationToken);

            // Calculate overall risk score (0.0 to 1.0)
            var avgDaysOfStock = inventoryAtRisk.Count > 0
                ? inventoryAtRisk.Average(i => i.QuantityOnHand > 0 ? (double)i.QuantityOnHand / 8.0 : 0.0) // ~8 units/day baseline
                : 0.0;
            var riskScore = Math.Clamp(1.0 - (avgDaysOfStock / (delayDays + 7)), 0.0, 1.0);

            // Build impact routes showing which stores are affected
            var routes = new List<ReroutingRoute>();
            foreach (var shipment in delayedShipments)
            {
                var destInventory = inventoryAtRisk.FirstOrDefault(i => i.StoreId == shipment.DestStoreId);
                var stockRunway = destInventory != null ? (int)((double)destInventory.QuantityOnHand / 8.0) : 0;
                var priority = stockRunway <= 1 ? "Critical"
                             : stockRunway <= 3 ? "High"
                             : stockRunway <= 7 ? "Medium" : "Low";

                routes.Add(new ReroutingRoute
                {
                    SourceStoreId = shipment.OriginStoreId,
                    SourceStoreName = GetStoreName(shipment.OriginStoreId),
                    DestStoreId = shipment.DestStoreId,
                    DestStoreName = GetStoreName(shipment.DestStoreId),
                    UnitsToTransfer = 0, // Impact analysis only — redistribution agent handles transfers
                    Priority = priority,
                    DistanceMiles = GetSimulatedDistance(shipment.OriginStoreId, shipment.DestStoreId),
                    EstimatedHours = shipment.DelayDays * 24
                });
            }

            var a2uiPayload = new ReroutingMapData
            {
                Sku = sku,
                ProductName = GetProductName(sku),
                Routes = routes,
                OverallRiskScore = Math.Round(riskScore, 2),
                DelayDays = delayDays,
                DelayReason = reason,
                Timestamp = DateTimeOffset.UtcNow
            };

            var textSummary = $"SKU {sku} ({GetProductName(sku)}): {delayedShipments.Count} delayed shipment(s), " +
                              $"{affectedDestStores.Count} store(s) affected. " +
                              $"Overall risk score: {riskScore:P0}. " +
                              $"Delay reason: {reason}, estimated {delayDays} additional day(s).";

            _logger.LogInformation(
                "LogisticsAgent completed: {DelayedCount} delayed shipments, {AffectedStores} affected stores, risk {Risk:P0}",
                delayedShipments.Count, affectedDestStores.Count, riskScore);

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
            _logger.LogError(ex, "LogisticsAgent failed for SKU {Sku}", sku);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", AgentName));

            return new AgentResult
            {
                TextSummary = $"Error analyzing shipments for SKU {sku}",
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

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
