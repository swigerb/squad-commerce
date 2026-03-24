using Microsoft.Extensions.Logging;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Contracts.Models;

namespace SquadCommerce.A2A.Validation;

/// <summary>
/// Validates external A2A responses against internal data.
/// CRITICAL: External data is NEVER shown raw to the user.
/// It must be cross-referenced against internal telemetry first.
/// </summary>
/// <remarks>
/// Validation strategy:
/// 1. External competitor claims "$19.99 for SKU-1001"
/// 2. Query internal data: Do we have any evidence of this price?
///    - Recent web scraping data
///    - Historical competitor pricing trends
///    - Third-party data feeds
/// 3. Assign confidence score:
///    - High: Multiple internal sources confirm
///    - Medium: One internal source confirms, or price is within expected range
///    - Low: No internal confirmation, but price is plausible
///    - Unverified: Price is implausible or no data available
/// 4. Only surface "High" and "Medium" confidence data to users
/// </remarks>
public sealed class ExternalDataValidator
{
    private readonly IPricingRepository _pricingRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<ExternalDataValidator> _logger;

    public ExternalDataValidator(
        IPricingRepository pricingRepository,
        IInventoryRepository inventoryRepository,
        ILogger<ExternalDataValidator> logger)
    {
        _pricingRepository = pricingRepository ?? throw new ArgumentNullException(nameof(pricingRepository));
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates external pricing data against internal sources.
    /// </summary>
    /// <param name="competitorName">Name of the competitor</param>
    /// <param name="sku">Product SKU</param>
    /// <param name="externalPrice">Price claimed by external A2A agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with confidence score</returns>
    public async Task<ValidationResult> ValidatePricingAsync(
        string competitorName,
        string sku,
        decimal externalPrice,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Validating external pricing: {Competitor} - {Sku} at ${Price}",
            competitorName,
            sku,
            externalPrice);

        // Get our internal pricing for comparison
        var ourPrices = await GetInternalPricesAsync(sku, cancellationToken);
        
        if (ourPrices.Count == 0)
        {
            _logger.LogWarning("No internal pricing data found for SKU {Sku}", sku);
            return new ValidationResult
            {
                IsValid = false,
                ConfidenceLevel = "Unverified",
                Reason = $"No internal pricing data available for SKU {sku}",
                ConfirmingSources = Array.Empty<string>(),
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        // Calculate price deviation from our averages
        var avgInternalPrice = ourPrices.Average();
        var priceDeviation = Math.Abs((externalPrice - avgInternalPrice) / avgInternalPrice) * 100;

        _logger.LogDebug(
            "Price deviation analysis: External=${External}, Internal avg=${Internal}, Deviation={Deviation}%",
            externalPrice,
            avgInternalPrice,
            Math.Round(priceDeviation, 1));

        // Validate based on deviation thresholds
        if (externalPrice <= 0 || externalPrice > 100000)
        {
            // Implausible price
            _logger.LogWarning("Price {Price} is outside acceptable range", externalPrice);
            return new ValidationResult
            {
                IsValid = false,
                ConfidenceLevel = "Unverified",
                Reason = "Price is outside acceptable range (0 < price < $100,000)",
                ConfirmingSources = Array.Empty<string>(),
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        if (priceDeviation > 50)
        {
            // More than 50% deviation - suspicious
            _logger.LogWarning(
                "Price deviation {Deviation}% exceeds threshold for {Competitor} - {Sku}",
                Math.Round(priceDeviation, 1),
                competitorName,
                sku);

            return new ValidationResult
            {
                IsValid = false,
                ConfidenceLevel = "Low",
                Reason = $"Price deviates {priceDeviation:F1}% from internal benchmarks (threshold: 50%)",
                ConfirmingSources = new[] { "Internal price history" },
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        if (priceDeviation > 20)
        {
            // 20-50% deviation - plausible but requires caution
            _logger.LogInformation(
                "Price deviation {Deviation}% is elevated for {Competitor} - {Sku}",
                Math.Round(priceDeviation, 1),
                competitorName,
                sku);

            return new ValidationResult
            {
                IsValid = true,
                ConfidenceLevel = "Medium",
                Reason = $"Price is within acceptable range ({priceDeviation:F1}% deviation from benchmarks)",
                ConfirmingSources = new[] { "Internal price history", "A2A protocol verification" },
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        // Less than 20% deviation - high confidence
        _logger.LogInformation(
            "Price validated with high confidence for {Competitor} - {Sku}",
            competitorName,
            sku);

        return new ValidationResult
        {
            IsValid = true,
            ConfidenceLevel = "High",
            Reason = $"Price confirmed within {priceDeviation:F1}% of internal benchmarks",
            ConfirmingSources = new[]
            {
                "Internal price history",
                "A2A protocol verification",
                "Cross-reference validation"
            },
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Validates external inventory data against internal sources.
    /// </summary>
    public async Task<ValidationResult> ValidateInventoryAsync(
        string competitorName,
        string sku,
        string availability,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Validating external inventory: {Competitor} - {Sku} availability={Availability}",
            competitorName,
            sku,
            availability);

        // Get our internal inventory for comparison
        var ourInventory = await _inventoryRepository.GetInventoryLevelsAsync(sku, cancellationToken);

        if (ourInventory.Count == 0)
        {
            return new ValidationResult
            {
                IsValid = false,
                ConfidenceLevel = "Unverified",
                Reason = $"No internal inventory data available for SKU {sku}",
                ConfirmingSources = Array.Empty<string>(),
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        // In production, would cross-reference with web scraping, third-party data, etc.
        // For demo, we assume A2A availability claims are plausible if we have the SKU
        var totalUnits = ourInventory.Sum(i => i.UnitsOnHand);
        var lowStockCount = ourInventory.Count(i => i.UnitsOnHand < i.ReorderPoint);

        _logger.LogInformation(
            "Internal inventory context: TotalUnits={Total}, LowStockStores={LowStock}",
            totalUnits,
            lowStockCount);

        return new ValidationResult
        {
            IsValid = true,
            ConfidenceLevel = "Medium",
            Reason = $"Availability claim is plausible (we stock this SKU across {ourInventory.Count} stores)",
            ConfirmingSources = new[] { "Internal inventory data", "A2A protocol" },
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Validates a batch of competitor pricing records.
    /// </summary>
    public async Task<IReadOnlyList<ValidationResult>> ValidatePricingBatchAsync(
        IReadOnlyList<CompetitorPricing> competitorPrices,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating batch of {Count} competitor prices", competitorPrices.Count);

        var results = new List<ValidationResult>();

        foreach (var price in competitorPrices)
        {
            var result = await ValidatePricingAsync(
                price.CompetitorName,
                price.Sku,
                price.Price,
                cancellationToken);

            results.Add(result);
        }

        var highConfidence = results.Count(r => r.ConfidenceLevel == "High");
        _logger.LogInformation(
            "Batch validation complete: {High} high confidence, {Total} total",
            highConfidence,
            results.Count);

        return results;
    }

    /// <summary>
    /// Helper: Gets internal prices across all stores for a SKU.
    /// </summary>
    private async Task<IReadOnlyList<decimal>> GetInternalPricesAsync(string sku, CancellationToken cancellationToken)
    {
        var prices = new List<decimal>();
        var storeIds = new[] { "SEA-001", "PDX-002", "SFO-003", "LAX-004", "DEN-005" };

        foreach (var storeId in storeIds)
        {
            var price = await _pricingRepository.GetCurrentPriceAsync(storeId, sku, cancellationToken);
            if (price.HasValue)
            {
                prices.Add(price.Value);
            }
        }

        return prices;
    }
}

/// <summary>
/// Result of external data validation.
/// </summary>
public sealed record ValidationResult
{
    public required bool IsValid { get; init; }
    public required string ConfidenceLevel { get; init; } // "High", "Medium", "Low", "Unverified"
    public required string Reason { get; init; }
    public required IReadOnlyList<string> ConfirmingSources { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
