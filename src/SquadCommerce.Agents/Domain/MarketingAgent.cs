using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Observability;

namespace SquadCommerce.Agents.Domain;

/// <summary>
/// MarketingAgent is responsible for building campaign previews and flash sale pricing
/// for viral demand scenarios. Uses template-based generation (deterministic, not LLM).
/// </summary>
/// <remarks>
/// Allowed tools: []
/// Required scope: SquadCommerce.Marketing.Read
/// Protocol: Internal
/// </remarks>
public sealed class MarketingAgent : IDomainAgent
{
    private readonly SquadCommerceDbContext _dbContext;
    private readonly IPricingRepository _pricingRepository;
    private readonly ILogger<MarketingAgent> _logger;

    public string AgentName => "MarketingAgent";

    public MarketingAgent(
        SquadCommerceDbContext dbContext,
        IPricingRepository pricingRepository,
        ILogger<MarketingAgent> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _pricingRepository = pricingRepository ?? throw new ArgumentNullException(nameof(pricingRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Builds a campaign preview based on a viral SKU: email copy, hero banner, and flash sale pricing.
    /// </summary>
    /// <param name="sku">Viral product SKU</param>
    /// <param name="demandMultiplier">Current demand multiplier (e.g., 4.0 for 400% spike)</param>
    /// <param name="region">Target region for the campaign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent result with CampaignPreview A2UI payload</returns>
    public async Task<AgentResult> ExecuteAsync(
        string sku,
        decimal demandMultiplier,
        string region,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        using var activity = SquadCommerceTelemetry.StartAgentSpan(AgentName, "Execute");
        activity?.SetTag("agent.name", AgentName);
        activity?.SetTag("agent.protocol", "Internal");
        activity?.SetTag("agent.sku", sku);
        activity?.SetTag("agent.demand_multiplier", (double)demandMultiplier);
        activity?.SetTag("agent.region", region);

        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", AgentName));

        _logger.LogInformation(
            "MarketingAgent executing campaign preview: SKU {Sku}, DemandMultiplier {Multiplier}x, Region {Region}",
            sku, demandMultiplier, region);

        try
        {
            var productName = GetProductName(sku);

            // Get current pricing for flash sale calculation
            var storeIds = new[] { "SEA-001", "PDX-002", "SFO-003", "LAX-004", "DEN-005" };
            var prices = new List<decimal>();
            foreach (var storeId in storeIds)
            {
                var price = await _pricingRepository.GetCurrentPriceAsync(storeId, sku, cancellationToken);
                if (price.HasValue) prices.Add(price.Value);
            }

            var originalPrice = prices.Count > 0 ? prices.Average() : 59.99m;

            // Calculate flash sale discount (15-25% based on demand multiplier)
            var discountPercent = demandMultiplier >= 4.0m ? 0.15m :
                                  demandMultiplier >= 3.0m ? 0.18m :
                                  demandMultiplier >= 2.0m ? 0.20m : 0.25m;
            var flashSalePrice = Math.Round(originalPrice * (1 - discountPercent), 2);

            // Template-based campaign copy
            var emailSubject = $"🔥 Trending Now: {productName} — {region} Exclusive Flash Sale";
            var emailBody = $"Hi there!\n\n" +
                           $"You've got great taste — {productName} is the #1 trending item in the {region} right now. " +
                           $"Demand has surged {demandMultiplier:F0}x and we're offering you an exclusive flash sale " +
                           $"at ${flashSalePrice:F2} (was ${originalPrice:F2}).\n\n" +
                           $"Plus, check out these complementary picks curated just for you.\n\n" +
                           $"Shop now before they're gone!";
            var heroBannerHeadline = $"{productName} — As Seen Everywhere";
            var heroBannerSubtext = $"Flash Sale: ${flashSalePrice:F2} | Limited Time | {region} Only";
            var callToAction = "Shop the Trend →";

            var a2uiPayload = new CampaignPreviewData
            {
                Sku = sku,
                ProductName = productName,
                EmailSubjectLine = emailSubject,
                EmailBody = emailBody,
                HeroBannerHeadline = heroBannerHeadline,
                HeroBannerSubtext = heroBannerSubtext,
                CallToAction = callToAction,
                OriginalPrice = originalPrice,
                FlashSalePrice = flashSalePrice,
                TargetRegion = region,
                Timestamp = DateTimeOffset.UtcNow
            };

            var textSummary = $"Campaign preview for {productName} ({sku}): " +
                             $"Flash sale ${flashSalePrice:F2} ({discountPercent * 100:F0}% off ${originalPrice:F2}) " +
                             $"targeting {region}. Demand multiplier: {demandMultiplier:F1}x.";

            _logger.LogInformation(
                "MarketingAgent completed: FlashSale ${FlashPrice:F2} ({Discount}% off), Region {Region}",
                flashSalePrice, discountPercent * 100, region);

            SquadCommerceTelemetry.A2UIPayloadCount.Add(1,
                new KeyValuePair<string, object?>("a2ui.component", "CampaignPreview"));

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
            _logger.LogError(ex, "MarketingAgent failed for SKU {Sku}", sku);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", AgentName));

            return new AgentResult
            {
                TextSummary = $"Error building campaign preview for SKU {sku}",
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

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
