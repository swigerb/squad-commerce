using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Contracts.Models;
using SquadCommerce.Observability;

namespace SquadCommerce.A2A;

/// <summary>
/// Client for calling external vendor agents via A2A protocol.
/// Used by MarketIntelAgent to query competitor pricing.
/// </summary>
/// <remarks>
/// A2A workflow:
/// 1. Discover external agent via Agent Card registry
/// 2. Authenticate using agent's AuthType (OAuth2, API key, etc.)
/// 3. Send A2A request with structured query
/// 4. Receive A2A response with structured data + metadata
/// 5. Validate response (ExternalDataValidator)
/// </remarks>
public sealed class A2AClient : IA2AClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<A2AClient> _logger;
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    public A2AClient(HttpClient httpClient, ILogger<A2AClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure HttpClient with reasonable defaults
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Queries external competitor pricing via A2A protocol.
    /// </summary>
    public async Task<IReadOnlyList<CompetitorPricing>> GetCompetitorPricingAsync(string sku, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        // Create A2A handshake span
        using var activity = SquadCommerceTelemetry.StartA2ASpan("ExternalVendor", "Handshake");
        activity?.SetTag("a2a.target.agent", "ExternalVendor");
        activity?.SetTag("a2a.request.type", "GetCompetitorPricing");
        activity?.SetTag("a2a.sku", sku);
        
        // Record handshake count
        SquadCommerceTelemetry.A2AHandshakeCount.Add(1,
            new KeyValuePair<string, object?>("a2a.target.agent", "ExternalVendor"));

        _logger.LogInformation("A2AClient querying competitor pricing for SKU {Sku}", sku);

        try
        {
            // For demo: simulate A2A call to external vendor agents
            // In production, this would discover agent cards and query multiple vendors
            var mockCompetitors = await GetMockCompetitorDataAsync(sku, cancellationToken);

            _logger.LogInformation("Retrieved {Count} competitor prices for SKU {Sku}", mockCompetitors.Count, sku);
            
            // Record handshake duration
            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.A2AHandshakeDuration.Record(duration,
                new KeyValuePair<string, object?>("a2a.target.agent", "ExternalVendor"));
            
            activity?.SetTag("a2a.response.status", "success");
            activity?.SetTag("a2a.response.count", mockCompetitors.Count);
            
            return mockCompetitors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A2AClient failed for SKU {Sku}", sku);
            
            // Set error status on span
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("a2a.response.status", "error");
            
            // Record duration even on error
            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.A2AHandshakeDuration.Record(duration,
                new KeyValuePair<string, object?>("a2a.target.agent", "ExternalVendor"));
            
            throw;
        }
    }

    /// <summary>
    /// Validates external data by cross-referencing with another A2A source.
    /// </summary>
    public async Task<bool> ValidateExternalDataAsync(CompetitorPricing competitorData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "A2AClient validating competitor data: {Competitor} - {Sku} at ${Price}",
            competitorData.CompetitorName,
            competitorData.Sku,
            competitorData.Price);

        // In production, this would query a verification service or secondary data source
        // For demo, we validate based on price reasonableness
        await Task.Delay(100, cancellationToken); // Simulate network call

        var isValid = competitorData.Price > 0 && competitorData.Price < 10000;
        _logger.LogInformation("Validation result: {IsValid}", isValid);
        
        return isValid;
    }

    /// <summary>
    /// Queries an external agent's pricing information via A2A with retry logic.
    /// </summary>
    private async Task<A2AResponse<PricingData>> QueryExternalPricingWithRetryAsync(
        AgentCard agentCard,
        string sku,
        CancellationToken cancellationToken)
    {
        var request = new A2ARequest
        {
            AgentId = "com.squadcommerce.marketintel",
            RequestId = Guid.NewGuid().ToString(),
            Capability = "GetStorePricing",
            Parameters = new Dictionary<string, object> { { "sku", sku } },
            Timestamp = DateTimeOffset.UtcNow
        };

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug("A2A request attempt {Attempt} of {MaxRetries} to {Endpoint}", 
                    attempt + 1, MaxRetries, agentCard.Endpoint);

                var response = await _httpClient.PostAsJsonAsync(agentCard.Endpoint, request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < MaxRetries - 1)
                {
                    // Rate limited - exponential backoff
                    var delaySeconds = Math.Pow(2, attempt);
                    _logger.LogWarning("Rate limited by {Endpoint}, retrying in {Delay}s", agentCard.Endpoint, delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                var a2aResponse = await response.Content.ReadFromJsonAsync<A2AResponse<PricingData>>(cancellationToken);
                if (a2aResponse == null)
                {
                    throw new InvalidOperationException("Failed to deserialize A2A response");
                }

                _logger.LogInformation("A2A request succeeded for {Endpoint}", agentCard.Endpoint);
                return a2aResponse;
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries - 1)
            {
                _logger.LogWarning(ex, "A2A request attempt {Attempt} failed, retrying...", attempt + 1);
                await Task.Delay(RetryDelay * (attempt + 1), cancellationToken);
            }
        }

        throw new InvalidOperationException($"Failed to query A2A endpoint after {MaxRetries} attempts");
    }

    /// <summary>
    /// Discovers available agents from an A2A registry.
    /// </summary>
    public async Task<IReadOnlyList<AgentCard>> DiscoverAgentsAsync(
        string registryUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Discovering agents from registry: {RegistryUrl}", registryUrl);

        try
        {
            var response = await _httpClient.GetFromJsonAsync<AgentCardRegistryResponse>(
                registryUrl,
                cancellationToken);

            if (response?.AgentCards == null)
            {
                _logger.LogWarning("No agent cards found in registry");
                return Array.Empty<AgentCard>();
            }

            _logger.LogInformation("Discovered {Count} agents", response.AgentCards.Count);
            return response.AgentCards;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover agents from registry");
            return Array.Empty<AgentCard>();
        }
    }

    /// <summary>
    /// Mock implementation: simulates external vendor A2A responses with realistic competitor data.
    /// In production, this would be replaced with real A2A protocol calls.
    /// </summary>
    private async Task<IReadOnlyList<CompetitorPricing>> GetMockCompetitorDataAsync(string sku, CancellationToken cancellationToken)
    {
        // Simulate network latency
        await Task.Delay(250, cancellationToken);

        // Mock competitor pricing based on SKU
        var basePrice = sku switch
        {
            "SKU-1001" => 28.99m, // Wireless Mouse
            "SKU-1002" => 11.99m, // USB-C Cable
            "SKU-1003" => 46.99m, // Laptop Stand
            "SKU-1004" => 74.99m, // Webcam
            "SKU-1005" => 112.99m, // Keyboard
            "SKU-1006" => 189.99m, // Headphones
            "SKU-1007" => 84.99m, // SSD
            "SKU-1008" => 329.99m, // Monitor
            _ => 29.99m
        };

        var competitors = new List<CompetitorPricing>
        {
            new CompetitorPricing
            {
                Sku = sku,
                CompetitorName = "TechMart",
                Price = Math.Round(basePrice * 0.92m, 2), // 8% lower
                Source = "A2A:TechMart Agent",
                Verified = true,
                LastUpdated = DateTimeOffset.UtcNow.AddHours(-2),
                ValidationNotes = "Verified via A2A protocol"
            },
            new CompetitorPricing
            {
                Sku = sku,
                CompetitorName = "ElectroWorld",
                Price = Math.Round(basePrice * 0.95m, 2), // 5% lower
                Source = "A2A:ElectroWorld Agent",
                Verified = true,
                LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                ValidationNotes = "Verified via A2A protocol"
            },
            new CompetitorPricing
            {
                Sku = sku,
                CompetitorName = "GadgetZone",
                Price = Math.Round(basePrice * 1.03m, 2), // 3% higher
                Source = "A2A:GadgetZone Agent",
                Verified = true,
                LastUpdated = DateTimeOffset.UtcNow.AddMinutes(-30),
                ValidationNotes = "Verified via A2A protocol"
            }
        };

        return competitors;
    }

    /// <summary>
    /// Queries external competitor pricing for multiple SKUs via A2A protocol (bulk operation).
    /// </summary>
    public async Task<IReadOnlyList<CompetitorPricing>> GetBulkCompetitorPricingAsync(IReadOnlyList<string> skus, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        using var activity = SquadCommerceTelemetry.StartA2ASpan("ExternalVendor", "BulkHandshake");
        activity?.SetTag("a2a.target.agent", "ExternalVendor");
        activity?.SetTag("a2a.request.type", "GetBulkCompetitorPricing");
        activity?.SetTag("a2a.sku_count", skus.Count);
        
        SquadCommerceTelemetry.A2AHandshakeCount.Add(1,
            new KeyValuePair<string, object?>("a2a.target.agent", "ExternalVendor"));

        _logger.LogInformation("A2AClient querying competitor pricing for {Count} SKUs", skus.Count);

        try
        {
            var allResults = new List<CompetitorPricing>();
            
            // For demo: query each SKU and aggregate results
            foreach (var sku in skus)
            {
                var mockData = await GetMockCompetitorDataAsync(sku, cancellationToken);
                allResults.AddRange(mockData);
            }

            _logger.LogInformation("Retrieved {Count} total competitor prices for {SkuCount} SKUs", allResults.Count, skus.Count);
            
            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.A2AHandshakeDuration.Record(duration,
                new KeyValuePair<string, object?>("a2a.target.agent", "ExternalVendor"));
            
            activity?.SetTag("a2a.response.status", "success");
            activity?.SetTag("a2a.response.count", allResults.Count);
            
            return allResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A2AClient bulk query failed for {Count} SKUs", skus.Count);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("a2a.response.status", "error");
            
            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.A2AHandshakeDuration.Record(duration,
                new KeyValuePair<string, object?>("a2a.target.agent", "ExternalVendor"));
            
            throw;
        }
    }
}

/// <summary>
/// A2A request envelope.
/// </summary>
public sealed record A2ARequest
{
    public required string AgentId { get; init; }
    public required string RequestId { get; init; }
    public required string Capability { get; init; }
    public required Dictionary<string, object> Parameters { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// A2A response envelope with typed data payload.
/// </summary>
public sealed record A2AResponse<T>
{
    public required string RequestId { get; init; }
    public required string AgentId { get; init; }
    public required bool Success { get; init; }
    public required T? Data { get; init; }
    public required ResponseMetadata Metadata { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Response metadata for trust and validation.
/// </summary>
public sealed record ResponseMetadata(
    DateTimeOffset Timestamp,
    string DataSource,
    string ConfidenceLevel,
    string Version);

/// <summary>
/// Pricing data returned by external A2A agents.
/// </summary>
public sealed record PricingData(
    string Sku,
    decimal Price,
    string Currency,
    DateTimeOffset AsOf);

/// <summary>
/// Agent card registry response.
/// </summary>
internal sealed record AgentCardRegistryResponse
{
    public required IReadOnlyList<AgentCard> AgentCards { get; init; }
}
