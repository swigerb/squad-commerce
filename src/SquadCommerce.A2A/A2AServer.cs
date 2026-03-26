using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Observability;

namespace SquadCommerce.A2A;

/// <summary>
/// A2A server for receiving and handling external A2A requests.
/// Exposes Squad-Commerce agents to external partners via A2A protocol.
/// </summary>
public sealed class A2AServer
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IPricingRepository _pricingRepository;
    private readonly ILogger<A2AServer> _logger;

    public A2AServer(
        IInventoryRepository inventoryRepository,
        IPricingRepository pricingRepository,
        ILogger<A2AServer> logger)
    {
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _pricingRepository = pricingRepository ?? throw new ArgumentNullException(nameof(pricingRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles an incoming A2A request.
    /// </summary>
    public async Task<A2AResponse<object>> HandleRequest(
        A2ARequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = SquadCommerceTelemetry.StartA2ASpan("A2AServer", request.Capability);
        activity?.SetTag("a2a.capability", request.Capability);
        activity?.SetTag("a2a.agentId", request.AgentId);
        activity?.SetTag("a2a.requestId", request.RequestId);

        _logger.LogInformation("A2AServer handling capability {Capability} for agent {AgentId}",
            request.Capability, request.AgentId);

        try
        {
            return request.Capability switch
            {
                "GetInventoryLevels" => await HandleGetInventoryLevels(request, cancellationToken),
                "GetStorePricing" => await HandleGetStorePricing(request, cancellationToken),
                "CalculateMarginImpact" => await HandleCalculateMarginImpact(request, cancellationToken),
                _ => new A2AResponse<object>
                {
                    RequestId = request.RequestId,
                    AgentId = "com.squadcommerce",
                    Success = false,
                    Data = null,
                    ErrorMessage = $"Unknown capability: {request.Capability}",
                    Metadata = new ResponseMetadata(
                        DateTimeOffset.UtcNow,
                        "Squad-Commerce A2A Server",
                        "N/A",
                        "1.0")
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A2AServer error handling {Capability}", request.Capability);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            return new A2AResponse<object>
            {
                RequestId = request.RequestId,
                AgentId = "com.squadcommerce",
                Success = false,
                Data = null,
                ErrorMessage = ex.Message,
                Metadata = new ResponseMetadata(
                    DateTimeOffset.UtcNow,
                    "Squad-Commerce A2A Server",
                    "N/A",
                    "1.0")
            };
        }
    }

    private async Task<A2AResponse<object>> HandleGetInventoryLevels(
        A2ARequest request,
        CancellationToken cancellationToken)
    {
        var sku = ExtractStringParam(request, "sku");

        _logger.LogInformation("A2AServer GetInventoryLevels for SKU {Sku}", sku);

        var inventory = await _inventoryRepository.GetInventoryLevelsAsync(sku, cancellationToken);

        var data = inventory.Select(inv => new
        {
            inv.StoreId,
            inv.Sku,
            inv.UnitsOnHand,
            inv.ReorderPoint,
            inv.UnitsOnOrder,
            inv.LastUpdated
        }).ToList();

        return new A2AResponse<object>
        {
            RequestId = request.RequestId,
            AgentId = "com.squadcommerce.inventory",
            Success = true,
            Data = new { Sku = sku, StoreCount = data.Count, Stores = data },
            Metadata = new ResponseMetadata(
                DateTimeOffset.UtcNow,
                "Squad-Commerce Inventory Repository",
                "High",
                "1.0")
        };
    }

    private async Task<A2AResponse<object>> HandleGetStorePricing(
        A2ARequest request,
        CancellationToken cancellationToken)
    {
        var sku = ExtractStringParam(request, "sku");

        _logger.LogInformation("A2AServer GetStorePricing for SKU {Sku}", sku);

        var storeIds = new[] { "SEA-001", "PDX-002", "SFO-003", "LAX-004", "DEN-005" };
        var prices = new List<object>();

        foreach (var storeId in storeIds)
        {
            var price = await _pricingRepository.GetCurrentPriceAsync(storeId, sku, cancellationToken);
            if (price.HasValue)
            {
                prices.Add(new { StoreId = storeId, Sku = sku, Price = price.Value });
            }
        }

        return new A2AResponse<object>
        {
            RequestId = request.RequestId,
            AgentId = "com.squadcommerce.pricing",
            Success = true,
            Data = new { Sku = sku, StoreCount = prices.Count, Stores = prices },
            Metadata = new ResponseMetadata(
                DateTimeOffset.UtcNow,
                "Squad-Commerce Pricing Repository",
                "High",
                "1.0")
        };
    }

    private async Task<A2AResponse<object>> HandleCalculateMarginImpact(
        A2ARequest request,
        CancellationToken cancellationToken)
    {
        var sku = ExtractStringParam(request, "sku");
        var competitorPrice = ExtractDecimalParam(request, "competitorPrice");

        _logger.LogInformation("A2AServer CalculateMarginImpact for SKU {Sku} at competitor ${Price:F2}",
            sku, competitorPrice);

        var storeIds = new[] { "SEA-001", "PDX-002", "SFO-003", "LAX-004", "DEN-005" };
        var currentPrices = new List<decimal>();

        foreach (var storeId in storeIds)
        {
            var price = await _pricingRepository.GetCurrentPriceAsync(storeId, sku, cancellationToken);
            if (price.HasValue)
                currentPrices.Add(price.Value);
        }

        if (currentPrices.Count == 0)
        {
            return new A2AResponse<object>
            {
                RequestId = request.RequestId,
                AgentId = "com.squadcommerce.pricing",
                Success = false,
                Data = null,
                ErrorMessage = $"No pricing data found for SKU {sku}",
                Metadata = new ResponseMetadata(
                    DateTimeOffset.UtcNow,
                    "Squad-Commerce Pricing Repository",
                    "N/A",
                    "1.0")
            };
        }

        var avgCurrentPrice = currentPrices.Average();
        var priceDelta = avgCurrentPrice - competitorPrice;
        var priceDeltaPercent = avgCurrentPrice > 0 ? (priceDelta / avgCurrentPrice) * 100 : 0;

        var inventory = await _inventoryRepository.GetInventoryLevelsAsync(sku, cancellationToken);
        var totalUnits = inventory.Sum(i => i.UnitsOnHand);

        var currentRevenue = avgCurrentPrice * totalUnits;
        var projectedRevenue = competitorPrice * (int)(totalUnits * 1.1m);
        var revenueDelta = projectedRevenue - currentRevenue;

        return new A2AResponse<object>
        {
            RequestId = request.RequestId,
            AgentId = "com.squadcommerce.pricing",
            Success = true,
            Data = new
            {
                Sku = sku,
                CurrentAvgPrice = Math.Round(avgCurrentPrice, 2),
                CompetitorPrice = competitorPrice,
                PriceDelta = Math.Round(priceDelta, 2),
                PriceDeltaPercent = Math.Round(priceDeltaPercent, 1),
                TotalUnitsInStock = totalUnits,
                CurrentRevenue = Math.Round(currentRevenue, 2),
                ProjectedRevenue = Math.Round(projectedRevenue, 2),
                RevenueDelta = Math.Round(revenueDelta, 2)
            },
            Metadata = new ResponseMetadata(
                DateTimeOffset.UtcNow,
                "Squad-Commerce Margin Calculator",
                "High",
                "1.0")
        };
    }

    private static string ExtractStringParam(A2ARequest request, string key)
    {
        if (request.Parameters.TryGetValue(key, out var value) && value is string s)
            return s;
        return value?.ToString() ?? throw new ArgumentException($"Missing required parameter: {key}");
    }

    private static decimal ExtractDecimalParam(A2ARequest request, string key)
    {
        if (!request.Parameters.TryGetValue(key, out var value))
            throw new ArgumentException($"Missing required parameter: {key}");
        return value switch
        {
            decimal d => d,
            double dbl => (decimal)dbl,
            int i => i,
            long l => l,
            string s when decimal.TryParse(s, out var parsed) => parsed,
            _ => Convert.ToDecimal(value)
        };
    }
}
