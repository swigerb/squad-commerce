using System.ComponentModel;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Observability;

namespace SquadCommerce.Mcp.Tools;

/// <summary>
/// MCP tool for calculating demand forecasts based on sentiment velocity and current inventory.
/// Exposed to agents via the Model Context Protocol using the official ModelContextProtocol SDK.
/// </summary>
/// <remarks>
/// This tool:
/// - Calculates demand forecast from sentiment velocity × current inventory
/// - Returns projected demand by store, stockout risk, and recommended actions
/// - Requires a SKU parameter; region is optional
/// </remarks>
[McpServerToolType]
public sealed class GetDemandForecastTool
{
    private const string ToolName = "GetDemandForecast";

    private readonly IInventoryRepository _inventoryRepository;
    private readonly SquadCommerceDbContext _dbContext;
    private readonly ILogger<GetDemandForecastTool> _logger;

    public GetDemandForecastTool(
        IInventoryRepository inventoryRepository,
        SquadCommerceDbContext dbContext,
        ILogger<GetDemandForecastTool> logger)
    {
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Calculates demand forecast for a SKU based on sentiment velocity and current inventory.
    /// </summary>
    [McpServerTool(Name = "GetDemandForecast"), Description("Calculates demand forecast for a SKU based on social sentiment velocity and current inventory levels. Returns projected demand, stockout risk, and recommended actions.")]
    public async Task<object> ExecuteAsync(
        [Description("Product SKU to forecast (e.g. SKU-3001)")] string sku,
        [Description("Optional: Filter by region (e.g. Northeast, Southeast)")] string? region = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        var parameters = new { sku, region };
        using var activity = SquadCommerceTelemetry.StartToolSpan(ToolName, parameters);
        activity?.SetTag("mcp.tool.name", ToolName);
        activity?.SetTag("mcp.sku", sku);

        SquadCommerceTelemetry.McpToolCallCount.Add(1,
            new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

        try
        {
            if (string.IsNullOrWhiteSpace(sku))
            {
                _logger.LogWarning("GetDemandForecast called without sku");
                var valDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(valDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));
                return new { Success = false, Error = "sku is required" };
            }

            _logger.LogInformation("GetDemandForecast executing for sku={Sku}, region={Region}",
                sku, region ?? "(all)");

            // Get sentiment data
            var sentimentQuery = _dbContext.SocialSentiment.Where(s => s.Sku == sku);
            if (!string.IsNullOrWhiteSpace(region))
                sentimentQuery = sentimentQuery.Where(s => s.Region == region);

            var sentimentData = await sentimentQuery.ToListAsync(cancellationToken);

            // Get inventory data
            var inventoryLevels = await _inventoryRepository.GetInventoryLevelsAsync(sku, cancellationToken);

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

            var avgVelocity = sentimentData.Count > 0 ? sentimentData.Average(s => s.Velocity) : 1.0;
            var demandMultiplier = Math.Max(1.0, avgVelocity);
            var totalInventory = inventoryLevels.Sum(i => i.UnitsOnHand);

            // Calculate per-store forecasts
            var storeForecast = inventoryLevels.Select(inv =>
            {
                var projectedDemand = (int)(inv.UnitsOnHand * demandMultiplier);
                var daysOfStock = demandMultiplier > 0
                    ? (int)(inv.UnitsOnHand / (demandMultiplier * 2))
                    : 30;
                var stockoutRisk = daysOfStock <= 3 ? "Critical"
                                 : daysOfStock <= 7 ? "High"
                                 : daysOfStock <= 14 ? "Medium" : "Low";

                return new
                {
                    inv.StoreId,
                    CurrentStock = inv.UnitsOnHand,
                    ProjectedDemand = projectedDemand,
                    DaysOfStockRemaining = daysOfStock,
                    StockoutRisk = stockoutRisk
                };
            }).ToArray();

            var criticalStores = storeForecast.Count(s => s.StockoutRisk is "Critical" or "High");

            // Build recommended actions
            var actions = new List<string>();
            if (criticalStores > 0)
                actions.Add($"Expedite replenishment for {criticalStores} high-risk store(s)");
            if (demandMultiplier > 3.0)
                actions.Add("Consider cross-regional inventory transfer to meet surge demand");
            if (demandMultiplier > 2.0)
                actions.Add("Activate safety stock reserves for affected region");
            actions.Add("Monitor social sentiment velocity for demand trajectory changes");

            var resultDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.McpToolCallDuration.Record(resultDuration,
                new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

            activity?.SetTag("mcp.result.store_count", storeForecast.Length);

            _logger.LogInformation(
                "GetDemandForecast completed: SKU {Sku}, DemandMultiplier {Multiplier}x, CriticalStores {Critical}",
                sku, demandMultiplier, criticalStores);

            return new
            {
                Success = true,
                Sku = sku,
                Region = region,
                DemandMultiplier = Math.Round(demandMultiplier, 2),
                TotalCurrentInventory = totalInventory,
                TotalProjectedDemand = (int)(totalInventory * demandMultiplier),
                StoreForecast = storeForecast,
                CriticalStoreCount = criticalStores,
                RecommendedActions = actions.ToArray(),
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetDemandForecast");

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
}
