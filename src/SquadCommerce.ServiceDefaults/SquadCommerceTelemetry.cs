using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SquadCommerce.Observability;

/// <summary>
/// Centralized telemetry constants for Squad-Commerce.
/// Provides ActivitySources for tracing and Meter for custom metrics.
/// </summary>
public static class SquadCommerceTelemetry
{
    /// <summary>
    /// Service name for telemetry.
    /// </summary>
    public const string ServiceName = "SquadCommerce";

    // Activity Sources for distributed tracing
    public static readonly ActivitySource Agents = new("SquadCommerce.Agents", "1.0.0");
    public static readonly ActivitySource Mcp = new("SquadCommerce.Mcp", "1.0.0");
    public static readonly ActivitySource A2A = new("SquadCommerce.A2A", "1.0.0");
    public static readonly ActivitySource AgUi = new("SquadCommerce.AgUi", "1.0.0");

    // Meter for custom metrics
    private static readonly Meter _meter = new("SquadCommerce", "1.0.0");

    // Custom metrics
    public static readonly Counter<long> AgentInvocationCount = _meter.CreateCounter<long>(
        "squad.agent.invocation.count",
        description: "Number of agent invocations");

    public static readonly Histogram<double> AgentInvocationDuration = _meter.CreateHistogram<double>(
        "squad.agent.invocation.duration",
        unit: "ms",
        description: "Agent execution time in milliseconds");

    public static readonly Counter<long> McpToolCallCount = _meter.CreateCounter<long>(
        "squad.mcp.tool.call.count",
        description: "Number of MCP tool calls");

    public static readonly Histogram<double> McpToolCallDuration = _meter.CreateHistogram<double>(
        "squad.mcp.tool.call.duration",
        unit: "ms",
        description: "MCP tool execution time in milliseconds");

    public static readonly Counter<long> A2AHandshakeCount = _meter.CreateCounter<long>(
        "squad.a2a.handshake.count",
        description: "Number of A2A handshakes");

    public static readonly Histogram<double> A2AHandshakeDuration = _meter.CreateHistogram<double>(
        "squad.a2a.handshake.duration",
        unit: "ms",
        description: "A2A round-trip time in milliseconds");

    public static readonly Counter<long> A2UIPayloadCount = _meter.CreateCounter<long>(
        "squad.a2ui.payload.count",
        description: "Number of A2UI payloads emitted");

    public static readonly Counter<long> PricingDecisionCount = _meter.CreateCounter<long>(
        "squad.pricing.decision.count",
        description: "Number of pricing decisions (approved/rejected/modified)");

    /// <summary>
    /// Starts a new agent invocation span.
    /// </summary>
    public static Activity? StartAgentSpan(string agentName, string operation)
    {
        return Agents.StartActivity($"{agentName}.{operation}", ActivityKind.Internal);
    }

    /// <summary>
    /// Starts a new MCP tool call span.
    /// </summary>
    public static Activity? StartToolSpan(string toolName, object? parameters = null)
    {
        var activity = Mcp.StartActivity($"MCP.{toolName}", ActivityKind.Client);
        if (activity != null && parameters != null)
        {
            activity.SetTag("mcp.tool.name", toolName);
            activity.SetTag("mcp.tool.parameters", System.Text.Json.JsonSerializer.Serialize(parameters));
        }
        return activity;
    }

    /// <summary>
    /// Starts a new A2A handshake span.
    /// </summary>
    public static Activity? StartA2ASpan(string externalAgent, string operation)
    {
        var activity = A2A.StartActivity($"A2A.{operation}", ActivityKind.Client);
        activity?.SetTag("a2a.external_agent", externalAgent);
        return activity;
    }

    /// <summary>
    /// Starts a new AG-UI streaming span.
    /// </summary>
    public static Activity? StartAgUiSpan(string sessionId, string eventType)
    {
        var activity = AgUi.StartActivity($"AGUI.{eventType}", ActivityKind.Producer);
        activity?.SetTag("agui.session_id", sessionId);
        activity?.SetTag("agui.event_type", eventType);
        return activity;
    }
}
