using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SquadCommerce.Observability;

/// <summary>
/// Singleton service that holds all Squad-Commerce metrics instances.
/// Register this as a singleton in DI and inject it into agents/tools to record metrics.
/// </summary>
public sealed class SquadCommerceMetrics
{
    private readonly Meter _meter;

    // Activity Sources for distributed tracing
    public ActivitySource Agents { get; }
    public ActivitySource Mcp { get; }
    public ActivitySource A2A { get; }
    public ActivitySource AgUi { get; }

    // Custom metrics
    public Counter<long> AgentInvocationCount { get; }
    public Histogram<double> AgentInvocationDuration { get; }
    public Counter<long> McpToolCallCount { get; }
    public Histogram<double> McpToolCallDuration { get; }
    public Counter<long> A2AHandshakeCount { get; }
    public Histogram<double> A2AHandshakeDuration { get; }
    public Counter<long> A2UIPayloadCount { get; }
    public Counter<long> PricingDecisionCount { get; }

    public SquadCommerceMetrics()
    {
        // Create meter
        _meter = new Meter("SquadCommerce", "1.0.0");

        // Create activity sources
        Agents = new ActivitySource("SquadCommerce.Agents", "1.0.0");
        Mcp = new ActivitySource("SquadCommerce.Mcp", "1.0.0");
        A2A = new ActivitySource("SquadCommerce.A2A", "1.0.0");
        AgUi = new ActivitySource("SquadCommerce.AgUi", "1.0.0");

        // Create metrics
        AgentInvocationCount = _meter.CreateCounter<long>(
            "squad.agent.invocation.count",
            description: "Number of agent invocations");

        AgentInvocationDuration = _meter.CreateHistogram<double>(
            "squad.agent.invocation.duration",
            unit: "ms",
            description: "Agent execution time in milliseconds");

        McpToolCallCount = _meter.CreateCounter<long>(
            "squad.mcp.tool.call.count",
            description: "Number of MCP tool calls");

        McpToolCallDuration = _meter.CreateHistogram<double>(
            "squad.mcp.tool.call.duration",
            unit: "ms",
            description: "MCP tool execution time in milliseconds");

        A2AHandshakeCount = _meter.CreateCounter<long>(
            "squad.a2a.handshake.count",
            description: "Number of A2A handshakes");

        A2AHandshakeDuration = _meter.CreateHistogram<double>(
            "squad.a2a.handshake.duration",
            unit: "ms",
            description: "A2A round-trip time in milliseconds");

        A2UIPayloadCount = _meter.CreateCounter<long>(
            "squad.a2ui.payload.count",
            description: "Number of A2UI payloads emitted");

        PricingDecisionCount = _meter.CreateCounter<long>(
            "squad.pricing.decision.count",
            description: "Number of pricing decisions (approved/rejected/modified)");
    }

    /// <summary>
    /// Starts a new agent invocation span with proper tagging.
    /// </summary>
    public Activity? StartAgentSpan(string agentName, string operation)
    {
        var activity = Agents.StartActivity($"{agentName}.{operation}", ActivityKind.Internal);
        activity?.SetTag("agent.name", agentName);
        activity?.SetTag("agent.operation", operation);
        return activity;
    }

    /// <summary>
    /// Starts a new MCP tool call span with proper tagging.
    /// </summary>
    public Activity? StartToolSpan(string toolName, object? parameters = null)
    {
        var activity = Mcp.StartActivity($"MCP.{toolName}", ActivityKind.Client);
        if (activity != null)
        {
            activity.SetTag("mcp.tool.name", toolName);
            if (parameters != null)
            {
                activity.SetTag("mcp.tool.parameters", System.Text.Json.JsonSerializer.Serialize(parameters));
            }
        }
        return activity;
    }

    /// <summary>
    /// Starts a new A2A handshake span with proper tagging.
    /// </summary>
    public Activity? StartA2ASpan(string externalAgent, string operation)
    {
        var activity = A2A.StartActivity($"A2A.{operation}", ActivityKind.Client);
        if (activity != null)
        {
            activity.SetTag("a2a.external_agent", externalAgent);
            activity.SetTag("a2a.operation", operation);
        }
        return activity;
    }

    /// <summary>
    /// Starts a new AG-UI streaming span with proper tagging.
    /// </summary>
    public Activity? StartAgUiSpan(string sessionId, string eventType)
    {
        var activity = AgUi.StartActivity($"AGUI.{eventType}", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag("agui.session_id", sessionId);
            activity.SetTag("agui.event_type", eventType);
        }
        return activity;
    }

    /// <summary>
    /// Records an agent invocation with metrics and span.
    /// </summary>
    public void RecordAgentInvocation(string agentName, double durationMs, bool success)
    {
        var tags = new TagList
        {
            { "agent.name", agentName },
            { "success", success }
        };

        AgentInvocationCount.Add(1, tags);
        AgentInvocationDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records an MCP tool call with metrics and span.
    /// </summary>
    public void RecordMcpToolCall(string toolName, double durationMs, bool success)
    {
        var tags = new TagList
        {
            { "mcp.tool.name", toolName },
            { "success", success }
        };

        McpToolCallCount.Add(1, tags);
        McpToolCallDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records an A2A handshake with metrics and span.
    /// </summary>
    public void RecordA2AHandshake(string externalAgent, double durationMs, bool success)
    {
        var tags = new TagList
        {
            { "a2a.external_agent", externalAgent },
            { "success", success }
        };

        A2AHandshakeCount.Add(1, tags);
        A2AHandshakeDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records an A2UI payload emission.
    /// </summary>
    public void RecordA2UIPayload(string componentType, string sessionId)
    {
        var tags = new TagList
        {
            { "a2ui.component_type", componentType },
            { "agui.session_id", sessionId }
        };

        A2UIPayloadCount.Add(1, tags);
    }

    /// <summary>
    /// Records a pricing decision.
    /// </summary>
    public void RecordPricingDecision(string action, string proposalId)
    {
        var tags = new TagList
        {
            { "pricing.action", action },
            { "pricing.proposal_id", proposalId }
        };

        PricingDecisionCount.Add(1, tags);
    }
}
