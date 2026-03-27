namespace SquadCommerce.Web.Services;

/// <summary>
/// Bridges AG-UI SSE stream activity to UI components like the Agent Fleet panel.
/// When the chat receives status updates from the streaming API, this service
/// forwards them so agent cards can show real-time activity.
/// </summary>
public sealed class AgentActivityService
{
    /// <summary>Fired when an agent starts or stops processing.</summary>
    public event Action<string, bool>? OnAgentActivity;

    /// <summary>Fired when an agent's status text changes.</summary>
    public event Action<string, string>? OnAgentStatusUpdate;

    /// <summary>Fired when all agents should reset to idle (e.g., stream completed).</summary>
    public event Action? OnAllAgentsIdle;

    private static readonly (string Key, string[] Keywords)[] AgentKeywords =
    [
        ("ChiefSoftwareArchitect", ["orchestrat", "routing", "delegat", "analyzing request", "processing"]),
        ("InventoryAgent", ["inventory", "stock", "warehouse", "sku"]),
        ("PricingAgent", ["pricing", "margin", "price", "cost"]),
        ("MarketIntelAgent", ["market", "competitor", "intel", "comparison"])
    ];

    /// <summary>
    /// Signals that streaming has started — the orchestrator is active.
    /// </summary>
    public void NotifyStreamingStarted()
    {
        OnAgentActivity?.Invoke("ChiefSoftwareArchitect", true);
    }

    /// <summary>
    /// Forwards a status update from the SSE stream, resolving which agent it relates to.
    /// </summary>
    public void NotifyStatusUpdate(string status)
    {
        var lower = status.ToLowerInvariant();

        foreach (var (key, keywords) in AgentKeywords)
        {
            if (Array.Exists(keywords, kw => lower.Contains(kw)))
            {
                OnAgentActivity?.Invoke(key, true);
                OnAgentStatusUpdate?.Invoke(key, status);
                return;
            }
        }

        // Generic status — attribute to orchestrator
        OnAgentStatusUpdate?.Invoke("ChiefSoftwareArchitect", status);
    }

    /// <summary>
    /// Signals that streaming has completed — reset all agents to idle.
    /// </summary>
    public void NotifyStreamingCompleted()
    {
        OnAllAgentsIdle?.Invoke();
    }
}
