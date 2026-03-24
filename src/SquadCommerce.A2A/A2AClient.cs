using System.Net.Http.Json;

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
/// 6. Emit OpenTelemetry span for observability
/// </remarks>
public sealed class A2AClient
{
    private readonly HttpClient _httpClient;

    public A2AClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Queries an external agent's pricing information via A2A.
    /// </summary>
    /// <param name="agentCard">The external agent's Agent Card (discovered via registry)</param>
    /// <param name="sku">Product SKU to query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A2A response with pricing data and metadata</returns>
    public async Task<A2AResponse<PricingData>> QueryExternalPricing(
        AgentCard agentCard,
        string sku,
        CancellationToken cancellationToken = default)
    {
        // TODO: Add OpenTelemetry span emission
        // using var activity = ActivitySource.StartActivity("A2AClient.QueryExternalPricing");
        // activity?.SetTag("a2a.agentId", agentCard.AgentId);
        // activity?.SetTag("sku", sku);

        var request = new A2ARequest
        {
            AgentId = "com.squadcommerce.marketintel",
            RequestId = Guid.NewGuid().ToString(),
            Capability = "GetStorePricing",
            Parameters = new Dictionary<string, object> { { "sku", sku } },
            Timestamp = DateTimeOffset.UtcNow
        };

        // TODO: Handle authentication based on agentCard.AuthType
        // TODO: Add retry logic with exponential backoff
        // TODO: Handle A2A-specific error codes

        var response = await _httpClient.PostAsJsonAsync(agentCard.Endpoint, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var a2aResponse = await response.Content.ReadFromJsonAsync<A2AResponse<PricingData>>(cancellationToken);
        return a2aResponse ?? throw new InvalidOperationException("Failed to deserialize A2A response");
    }

    /// <summary>
    /// Discovers available agents from an A2A registry.
    /// </summary>
    public async Task<IReadOnlyList<AgentCard>> DiscoverAgents(
        string registryUrl,
        CancellationToken cancellationToken = default)
    {
        // TODO: Query A2A Agent Card registry
        // For now, return empty list (stub)
        await Task.CompletedTask;
        return Array.Empty<AgentCard>();
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
