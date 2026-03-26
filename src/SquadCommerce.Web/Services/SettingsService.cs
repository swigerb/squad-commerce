namespace SquadCommerce.Web.Services;

public sealed class SettingsService
{
    // AI Configuration
    public string? AzureOpenAiEndpoint { get; set; }
    public string? AzureOpenAiDeployment { get; set; } = "gpt-4o-mini";
    public string? AzureOpenAiApiKey { get; set; }

    // Agent Configuration
    public bool InventoryAgentEnabled { get; set; } = true;
    public bool PricingAgentEnabled { get; set; } = true;
    public bool MarketIntelAgentEnabled { get; set; } = true;
    public bool ComplianceAgentEnabled { get; set; } = true;

    // A2A Configuration
    public string? CompetitorAgentEndpoint { get; set; }
    public bool DemoMode { get; set; } = true;

    // MCP Configuration
    public string McpEndpoint { get; set; } = "/mcp";

    // UI Preferences
    public bool AudioCuesEnabled { get; set; } = true;
    public bool ShowReasoningTrace { get; set; } = true;
    public bool ShowPipelineView { get; set; } = true;
    public string Theme { get; set; } = "dark";

    // Telemetry
    public bool ShowTelemetryDashboard { get; set; } = true;
    public int TelemetryRefreshIntervalMs { get; set; } = 2000;

    public event Action? OnSettingsChanged;

    public void NotifySettingsChanged() => OnSettingsChanged?.Invoke();

    public void ResetToDefaults()
    {
        AzureOpenAiEndpoint = null;
        AzureOpenAiDeployment = "gpt-4o-mini";
        AzureOpenAiApiKey = null;
        InventoryAgentEnabled = true;
        PricingAgentEnabled = true;
        MarketIntelAgentEnabled = true;
        ComplianceAgentEnabled = true;
        CompetitorAgentEndpoint = null;
        DemoMode = true;
        McpEndpoint = "/mcp";
        AudioCuesEnabled = true;
        ShowReasoningTrace = true;
        ShowPipelineView = true;
        Theme = "dark";
        ShowTelemetryDashboard = true;
        TelemetryRefreshIntervalMs = 2000;
        NotifySettingsChanged();
    }
}
