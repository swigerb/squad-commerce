using System.Security.Claims;

namespace SquadCommerce.Api.Middleware;

/// <summary>
/// Validates Entra ID scopes against agent policy requirements.
/// Supports demo mode for development without full Entra ID integration.
/// </summary>
public class EntraIdScopeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EntraIdScopeMiddleware> _logger;
    private readonly IConfiguration _configuration;

    // Agent scope mappings
    private static readonly Dictionary<string, string> AgentScopes = new()
    {
        ["ChiefSoftwareArchitect"] = "SquadCommerce.Orchestrate",
        ["InventoryAgent"] = "SquadCommerce.Inventory.Read",
        ["PricingAgent"] = "SquadCommerce.Pricing.ReadWrite",
        ["MarketIntelAgent"] = "SquadCommerce.MarketIntel.Read"
    };

    public EntraIdScopeMiddleware(RequestDelegate next, ILogger<EntraIdScopeMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var enforcementMode = _configuration["EntraId:EnforcementMode"] ?? "Demo";

        // Skip scope validation for health checks and static assets
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/alive") ||
            context.Request.Path.StartsWithSegments("/_framework"))
        {
            await _next(context);
            return;
        }

        // Extract agent name from request (simplified for demo)
        var agentName = ExtractAgentNameFromRequest(context);

        if (!string.IsNullOrEmpty(agentName) && AgentScopes.TryGetValue(agentName, out var requiredScope))
        {
            var userScopes = ExtractScopes(context.User);

            if (!userScopes.Contains(requiredScope))
            {
                if (enforcementMode == "Demo")
                {
                    _logger.LogWarning("DEMO MODE: Scope validation failed for agent {AgentName}. Required: {RequiredScope}, Present: {PresentScopes}. Request allowed.",
                        agentName, requiredScope, string.Join(", ", userScopes));
                }
                else
                {
                    _logger.LogWarning("Scope validation failed for agent {AgentName}. Required: {RequiredScope}, Present: {PresentScopes}. Request denied.",
                        agentName, requiredScope, string.Join(", ", userScopes));
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "insufficient_scope",
                        message = $"Required scope '{requiredScope}' not present in token.",
                        agentName,
                        requiredScope
                    });
                    return;
                }
            }
            else
            {
                _logger.LogInformation("Scope validation passed for agent {AgentName}. Scope: {RequiredScope}", agentName, requiredScope);
            }
        }

        await _next(context);
    }

    private static string? ExtractAgentNameFromRequest(HttpContext context)
    {
        // Check query parameters
        if (context.Request.Query.TryGetValue("agentName", out var agentParam))
        {
            return agentParam.ToString();
        }

        // Check route values
        if (context.Request.RouteValues.TryGetValue("agentName", out var agentRoute))
        {
            return agentRoute?.ToString();
        }

        // Check custom header
        if (context.Request.Headers.TryGetValue("X-Agent-Name", out var agentHeader))
        {
            return agentHeader.ToString();
        }

        return null;
    }

    private static IReadOnlyList<string> ExtractScopes(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return Array.Empty<string>();
        }

        // Extract scopes from JWT claims
        var scopeClaims = user.FindAll("scope").Select(c => c.Value).ToList();
        var scopesClaim = user.FindFirst("scopes")?.Value;

        if (!string.IsNullOrEmpty(scopesClaim))
        {
            scopeClaims.AddRange(scopesClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        return scopeClaims.Distinct().ToArray();
    }
}

