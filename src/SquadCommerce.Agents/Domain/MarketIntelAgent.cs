using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SquadCommerce.A2A.Validation;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Observability;

namespace SquadCommerce.Agents.Domain;

/// <summary>
/// MarketIntelAgent is responsible for retrieving and validating competitor pricing
/// from external vendor agents via A2A protocol.
/// </summary>
/// <remarks>
/// Allowed tools: [] (uses A2A client instead of MCP)
/// Required scope: SquadCommerce.MarketIntel.Read
/// Protocol: A2A
/// 
/// CRITICAL: External data from A2A is NEVER shown raw to the user.
/// It is cross-referenced against internal data (ExternalDataValidator) before surfacing.
/// </remarks>
public sealed class MarketIntelAgent : IDomainAgent
{
    private readonly IA2AClient _a2aClient;
    private readonly ExternalDataValidator _validator;
    private readonly ILogger<MarketIntelAgent> _logger;

    public string AgentName => "MarketIntelAgent";

    public MarketIntelAgent(
        IA2AClient a2aClient,
        ExternalDataValidator validator,
        ILogger<MarketIntelAgent> logger)
    {
        _a2aClient = a2aClient ?? throw new ArgumentNullException(nameof(a2aClient));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes A2A competitor pricing query with validation and builds A2UI comparison grid.
    /// </summary>
    /// <param name="sku">Product SKU</param>
    /// <param name="ourPrice">Our current price for comparison context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent result with MarketComparisonGrid A2UI payload</returns>
    public async Task<AgentResult> ExecuteAsync(
        string sku,
        decimal ourPrice,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        // Create agent invocation span
        using var activity = SquadCommerceTelemetry.StartAgentSpan(AgentName, "Execute");
        activity?.SetTag("agent.name", AgentName);
        activity?.SetTag("agent.protocol", "A2A");
        activity?.SetTag("agent.sku", sku);
        activity?.SetTag("agent.our_price", ourPrice);
        
        // Record invocation count
        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", AgentName));

        _logger.LogInformation(
            "MarketIntelAgent executing A2A query: SKU {Sku}, OurPrice ${OurPrice:F2}",
            sku,
            ourPrice);

        try
        {
            // Step 1: Query external competitor pricing via A2A
            var competitorPrices = await _a2aClient.GetCompetitorPricingAsync(sku, cancellationToken);

            if (competitorPrices.Count == 0)
            {
                _logger.LogWarning("No competitor pricing data retrieved for SKU {Sku}", sku);
                return new AgentResult
                {
                    TextSummary = $"No competitor pricing available for SKU {sku}",
                    Success = false,
                    ErrorMessage = "A2A query returned no results",
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            _logger.LogInformation("Retrieved {Count} competitor prices for SKU {Sku}", competitorPrices.Count, sku);

            // Step 2: Validate each competitor price against internal data
            var validationResults = await _validator.ValidatePricingBatchAsync(competitorPrices, cancellationToken);

            // Step 3: Filter to only High and Medium confidence data
            var validatedPrices = competitorPrices
                .Zip(validationResults, (price, validation) => new { Price = price, Validation = validation })
                .Where(x => x.Validation.ConfidenceLevel is "High" or "Medium")
                .Select(x => x.Price)
                .ToList();

            if (validatedPrices.Count == 0)
            {
                _logger.LogWarning("All competitor prices failed validation for SKU {Sku}", sku);
                return new AgentResult
                {
                    TextSummary = $"Competitor prices for SKU {sku} could not be verified",
                    Success = false,
                    ErrorMessage = "No high-confidence competitor pricing data available after validation",
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            _logger.LogInformation(
                "Validation complete: {Validated} of {Total} prices passed validation",
                validatedPrices.Count,
                competitorPrices.Count);

            // Step 4: Build A2UI payload for MarketComparisonGrid
            var competitorPriceData = validatedPrices.Select(p => new CompetitorPrice
            {
                CompetitorName = p.CompetitorName,
                Price = p.Price,
                Source = p.Source,
                Verified = p.Verified,
                LastUpdated = p.LastUpdated
            }).ToList();

            var a2uiPayload = new MarketComparisonGridData
            {
                Sku = sku,
                ProductName = GetProductName(sku),
                Competitors = competitorPriceData,
                OurPrice = ourPrice,
                Timestamp = DateTimeOffset.UtcNow
            };

            // Step 5: Generate text summary
            var lowestCompetitorPrice = validatedPrices.Min(p => p.Price);
            var avgCompetitorPrice = validatedPrices.Average(p => p.Price);
            var priceDelta = ourPrice - lowestCompetitorPrice;
            var priceDeltaPercent = (priceDelta / ourPrice) * 100;

            var textSummary = $"SKU {sku}: {validatedPrices.Count} verified competitor prices. " +
                              $"Lowest: ${lowestCompetitorPrice:F2} ({validatedPrices.First(p => p.Price == lowestCompetitorPrice).CompetitorName}), " +
                              $"Avg: ${avgCompetitorPrice:F2}, " +
                              $"Our price: ${ourPrice:F2} ({(priceDelta > 0 ? "+" : "")}{priceDeltaPercent:F1}% vs lowest). " +
                              $"All prices validated via A2A protocol with ExternalDataValidator.";

            _logger.LogInformation(
                "MarketIntelAgent completed: Lowest ${Lowest:F2}, Avg ${Avg:F2}, OurPrice ${Our:F2}",
                lowestCompetitorPrice,
                avgCompetitorPrice,
                ourPrice);

            // Record A2UI payload emission
            SquadCommerceTelemetry.A2UIPayloadCount.Add(1,
                new KeyValuePair<string, object?>("a2ui.component", "MarketComparisonGrid"));

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
            _logger.LogError(ex, "MarketIntelAgent failed for SKU {Sku}", sku);
            
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
                TextSummary = $"Error retrieving competitor pricing for SKU {sku}",
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
        _ => sku
    };
}
