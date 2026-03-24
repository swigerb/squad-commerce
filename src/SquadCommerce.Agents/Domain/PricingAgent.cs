using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Contracts.Models;
using SquadCommerce.Observability;

namespace SquadCommerce.Agents.Domain;

/// <summary>
/// PricingAgent is responsible for calculating margin impact and proposing/applying price changes.
/// It has read/write access to pricing data and updates store prices via MCP.
/// </summary>
/// <remarks>
/// Allowed tools: ["GetInventoryLevels", "UpdateStorePricing"]
/// Required scope: SquadCommerce.Pricing.ReadWrite
/// Protocol: MCP
/// </remarks>
public sealed class PricingAgent : IDomainAgent
{
    private readonly IPricingRepository _pricingRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<PricingAgent> _logger;

    public string AgentName => "PricingAgent";

    public PricingAgent(
        IPricingRepository pricingRepository,
        IInventoryRepository inventoryRepository,
        ILogger<PricingAgent> logger)
    {
        _pricingRepository = pricingRepository ?? throw new ArgumentNullException(nameof(pricingRepository));
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes margin impact analysis and builds A2UI pricing chart payload.
    /// </summary>
    /// <param name="sku">Product SKU</param>
    /// <param name="competitorPrice">Competitor's proposed price</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent result with PricingImpactChart A2UI payload</returns>
    public async Task<AgentResult> ExecuteAsync(
        string sku,
        decimal competitorPrice,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        // Create agent invocation span
        using var activity = SquadCommerceTelemetry.StartAgentSpan(AgentName, "Execute");
        activity?.SetTag("agent.name", AgentName);
        activity?.SetTag("agent.protocol", "MCP");
        activity?.SetTag("agent.sku", sku);
        activity?.SetTag("agent.competitor_price", competitorPrice);
        
        // Record invocation count
        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", AgentName));

        _logger.LogInformation(
            "PricingAgent executing margin analysis: SKU {Sku}, CompetitorPrice ${CompetitorPrice:F2}",
            sku,
            competitorPrice);

        try
        {
            // Get current pricing across all stores
            var storeIds = new[] { "SEA-001", "PDX-002", "SFO-003", "LAX-004", "DEN-005" };
            var currentPrices = new List<decimal>();
            decimal totalCost = 0;
            int storeCount = 0;

            foreach (var storeId in storeIds)
            {
                var price = await _pricingRepository.GetCurrentPriceAsync(storeId, sku, cancellationToken);
                if (price.HasValue)
                {
                    currentPrices.Add(price.Value);
                    storeCount++;

                    // Try to get cost (using internal helper if available)
                    if (_pricingRepository is Mcp.Data.IPricingRepositoryInternal repoInternal)
                    {
                        var cost = await repoInternal.GetCostAsync(storeId, sku, cancellationToken);
                        if (cost.HasValue)
                        {
                            totalCost += cost.Value;
                        }
                    }
                }
            }

            if (currentPrices.Count == 0)
            {
                _logger.LogWarning("No pricing data found for SKU {Sku}", sku);
                return new AgentResult
                {
                    TextSummary = $"No pricing data found for SKU {sku}",
                    Success = false,
                    ErrorMessage = $"SKU {sku} not found in pricing system",
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            var avgCurrentPrice = currentPrices.Average();
            var avgCost = totalCost / storeCount;

            // Get inventory context for volume estimates
            var inventory = await _inventoryRepository.GetInventoryLevelsAsync(sku, cancellationToken);
            var totalUnits = inventory.Sum(i => i.UnitsOnHand);

            // Calculate scenarios
            var scenarios = new List<PriceScenario>
            {
                // Current state
                CalculateScenario("Current Pricing", avgCurrentPrice, avgCost, totalUnits, 100),
                
                // Match competitor
                CalculateScenario("Match Competitor", competitorPrice, avgCost, totalUnits, 110),
                
                // Beat competitor by 5%
                CalculateScenario("Beat by 5%", competitorPrice * 0.95m, avgCost, totalUnits, 120),
                
                // Split difference
                CalculateScenario("Split Difference", (avgCurrentPrice + competitorPrice) / 2, avgCost, totalUnits, 105)
            };

            var a2uiPayload = new PricingImpactChartData
            {
                Sku = sku,
                CurrentPrice = avgCurrentPrice,
                ProposedPrice = competitorPrice,
                Scenarios = scenarios,
                Timestamp = DateTimeOffset.UtcNow
            };

            // Generate text summary
            var currentMargin = ((avgCurrentPrice - avgCost) / avgCurrentPrice) * 100;
            var proposedMargin = ((competitorPrice - avgCost) / competitorPrice) * 100;
            var marginDelta = proposedMargin - currentMargin;

            var textSummary = $"SKU {sku}: Current ${avgCurrentPrice:F2} ({currentMargin:F1}% margin) → " +
                              $"Proposed ${competitorPrice:F2} ({proposedMargin:F1}% margin). " +
                              $"Margin impact: {marginDelta:+0.0;-0.0}pp. " +
                              $"Estimated volume uplift: +10-20% ({totalUnits} units in stock).";

            _logger.LogInformation(
                "PricingAgent completed: Margin delta {MarginDelta:F1}pp, Current {Current:F1}% → Proposed {Proposed:F1}%",
                marginDelta,
                currentMargin,
                proposedMargin);

            // Record A2UI payload emission
            SquadCommerceTelemetry.A2UIPayloadCount.Add(1,
                new KeyValuePair<string, object?>("a2ui.component", "PricingImpactChart"));

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
            _logger.LogError(ex, "PricingAgent failed for SKU {Sku}", sku);
            
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
                TextSummary = $"Error calculating pricing impact for SKU {sku}",
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    /// Executes bulk margin impact analysis for multiple SKUs and builds consolidated A2UI pricing chart payload.
    /// </summary>
    public async Task<AgentResult> ExecuteBulkAsync(
        IReadOnlyList<(string Sku, decimal CompetitorPrice)> items,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        using var activity = SquadCommerceTelemetry.StartAgentSpan(AgentName, "ExecuteBulk");
        activity?.SetTag("agent.name", AgentName);
        activity?.SetTag("agent.protocol", "MCP");
        activity?.SetTag("agent.sku_count", items.Count);
        
        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", AgentName));

        _logger.LogInformation("PricingAgent executing bulk margin analysis for {Count} SKUs", items.Count);

        try
        {
            var storeIds = new[] { "SEA-001", "PDX-002", "SFO-003", "LAX-004", "DEN-005" };
            var allScenarios = new List<PriceScenario>();
            var skuList = items.Select(i => i.Sku).ToList();
            
            decimal totalCurrentRevenue = 0;
            decimal totalProposedRevenue = 0;
            int totalUnits = 0;

            foreach (var item in items)
            {
                var currentPrices = new List<decimal>();
                decimal totalCost = 0;
                int storeCount = 0;

                foreach (var storeId in storeIds)
                {
                    var price = await _pricingRepository.GetCurrentPriceAsync(storeId, item.Sku, cancellationToken);
                    if (price.HasValue)
                    {
                        currentPrices.Add(price.Value);
                        storeCount++;

                        if (_pricingRepository is Mcp.Data.IPricingRepositoryInternal repoInternal)
                        {
                            var cost = await repoInternal.GetCostAsync(storeId, item.Sku, cancellationToken);
                            if (cost.HasValue)
                            {
                                totalCost += cost.Value;
                            }
                        }
                    }
                }

                if (currentPrices.Count > 0)
                {
                    var avgCurrentPrice = currentPrices.Average();
                    var avgCost = totalCost / storeCount;

                    var inventory = await _inventoryRepository.GetInventoryLevelsAsync(item.Sku, cancellationToken);
                    var skuUnits = inventory.Sum(i => i.UnitsOnHand);
                    totalUnits += skuUnits;

                    var scenario = CalculateScenario(
                        $"{item.Sku} - Match Competitor", 
                        item.CompetitorPrice, 
                        avgCost, 
                        skuUnits, 
                        110);
                    allScenarios.Add(scenario);
                    
                    totalCurrentRevenue += avgCurrentPrice * skuUnits;
                    totalProposedRevenue += item.CompetitorPrice * (int)(skuUnits * 1.1);
                }
            }

            if (allScenarios.Count == 0)
            {
                _logger.LogWarning("No pricing data found for {Count} SKUs", items.Count);
                return new AgentResult
                {
                    TextSummary = $"No pricing data found for {items.Count} SKUs",
                    Success = false,
                    ErrorMessage = $"No SKUs found in pricing system",
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            var bulkAvgCurrentPrice = items.Average(i => i.CompetitorPrice) * 1.05m;
            var avgProposedPrice = items.Average(i => i.CompetitorPrice);
            
            var a2uiPayload = new PricingImpactChartData
            {
                Sku = string.Join(", ", skuList.Take(3)) + (skuList.Count > 3 ? $" (+{skuList.Count - 3} more)" : ""),
                CurrentPrice = bulkAvgCurrentPrice,
                ProposedPrice = avgProposedPrice,
                Scenarios = allScenarios,
                Timestamp = DateTimeOffset.UtcNow
            };

            var revenueDelta = totalProposedRevenue - totalCurrentRevenue;
            var revenueDeltaPercent = totalCurrentRevenue > 0 ? (revenueDelta / totalCurrentRevenue) * 100 : 0;

            var textSummary = $"Bulk analysis for {items.Count} SKUs: " +
                              $"Current revenue: ${totalCurrentRevenue:F2} → Proposed: ${totalProposedRevenue:F2} " +
                              $"({revenueDeltaPercent:+0.0;-0.0}% change). " +
                              $"Total {totalUnits} units in stock across all SKUs.";

            _logger.LogInformation("PricingAgent bulk completed: Revenue delta ${RevenueDelta:F2} ({RevenueDeltaPercent:F1}%)", 
                revenueDelta, revenueDeltaPercent);

            SquadCommerceTelemetry.A2UIPayloadCount.Add(1,
                new KeyValuePair<string, object?>("a2ui.component", "PricingImpactChart"));

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
            _logger.LogError(ex, "PricingAgent bulk analysis failed for {Count} SKUs", items.Count);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            
            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", AgentName));
            
            return new AgentResult
            {
                TextSummary = $"Error calculating bulk pricing impact for {items.Count} SKUs",
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    private static PriceScenario CalculateScenario(
        string name,
        decimal price,
        decimal cost,
        int totalUnits,
        int volumeMultiplier)
    {
        var margin = ((price - cost) / price) * 100;
        var projectedUnits = (int)(totalUnits * (volumeMultiplier / 100.0));
        var revenue = price * projectedUnits;

        return new PriceScenario
        {
            ScenarioName = name,
            Price = Math.Round(price, 2),
            EstimatedMargin = Math.Round(margin, 1),
            EstimatedRevenue = Math.Round(revenue, 2),
            ProjectedUnitsSold = projectedUnits
        };
    }
}
