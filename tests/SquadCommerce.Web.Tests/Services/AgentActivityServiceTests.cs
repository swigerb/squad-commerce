using FluentAssertions;
using SquadCommerce.Web.Services;

namespace SquadCommerce.Web.Tests.Services;

public class AgentActivityServiceTests
{
    private readonly AgentActivityService _service = new();

    // ── NotifyStreamingStarted ──────────────────────────────────────────

    [Fact]
    public void Should_ActivateOrchestrator_When_StreamingStarted()
    {
        // Arrange
        string? activatedAgent = null;
        bool? isActive = null;
        _service.OnAgentActivity += (agent, active) =>
        {
            activatedAgent = agent;
            isActive = active;
        };

        // Act
        _service.NotifyStreamingStarted();

        // Assert
        activatedAgent.Should().Be("ChiefSoftwareArchitect");
        isActive.Should().BeTrue();
    }

    [Fact]
    public void Should_NotThrow_When_StreamingStartedWithNoSubscribers()
    {
        var act = () => _service.NotifyStreamingStarted();
        act.Should().NotThrow();
    }

    // ── NotifyStreamingCompleted ─────────────────────────────────────────

    [Fact]
    public void Should_FireAllAgentsIdle_When_StreamingCompleted()
    {
        // Arrange
        bool idleFired = false;
        _service.OnAllAgentsIdle += () => idleFired = true;

        // Act
        _service.NotifyStreamingCompleted();

        // Assert
        idleFired.Should().BeTrue();
    }

    [Fact]
    public void Should_NotThrow_When_StreamingCompletedWithNoSubscribers()
    {
        var act = () => _service.NotifyStreamingCompleted();
        act.Should().NotThrow();
    }

    // ── NotifyStatusUpdate — Keyword Routing ────────────────────────────

    [Theory]
    [InlineData("Orchestrating agent workflow", "ChiefSoftwareArchitect")]
    [InlineData("Routing request to pricing", "ChiefSoftwareArchitect")]
    [InlineData("Delegating to inventory agent", "ChiefSoftwareArchitect")]
    [InlineData("Analyzing request parameters", "ChiefSoftwareArchitect")]
    [InlineData("Processing user query", "ChiefSoftwareArchitect")]
    public void Should_RouteToOrchestrator_When_StatusContainsOrchestratorKeywords(
        string status, string expectedAgent)
    {
        // Arrange
        string? routedAgent = null;
        _service.OnAgentActivity += (agent, _) => routedAgent = agent;

        // Act
        _service.NotifyStatusUpdate(status);

        // Assert
        routedAgent.Should().Be(expectedAgent);
    }

    [Theory]
    [InlineData("Checking inventory levels for SKU-1001", "InventoryAgent")]
    [InlineData("Stock count retrieved from warehouse", "InventoryAgent")]
    [InlineData("Warehouse availability confirmed", "InventoryAgent")]
    [InlineData("SKU lookup in progress", "InventoryAgent")]
    public void Should_RouteToInventoryAgent_When_StatusContainsInventoryKeywords(
        string status, string expectedAgent)
    {
        string? routedAgent = null;
        _service.OnAgentActivity += (agent, _) => routedAgent = agent;

        _service.NotifyStatusUpdate(status);

        routedAgent.Should().Be(expectedAgent);
    }

    [Theory]
    [InlineData("Calculating pricing impact", "PricingAgent")]
    [InlineData("Margin analysis complete", "PricingAgent")]
    [InlineData("Price adjustment recommended", "PricingAgent")]
    [InlineData("Cost breakdown analysis", "PricingAgent")]
    public void Should_RouteToPricingAgent_When_StatusContainsPricingKeywords(
        string status, string expectedAgent)
    {
        string? routedAgent = null;
        _service.OnAgentActivity += (agent, _) => routedAgent = agent;

        _service.NotifyStatusUpdate(status);

        routedAgent.Should().Be(expectedAgent);
    }

    [Theory]
    [InlineData("Market analysis in progress", "MarketIntelAgent")]
    [InlineData("Competitor data retrieved", "MarketIntelAgent")]
    [InlineData("Intel report generated", "MarketIntelAgent")]
    [InlineData("Comparison with competitor data", "MarketIntelAgent")]
    public void Should_RouteToMarketIntelAgent_When_StatusContainsMarketKeywords(
        string status, string expectedAgent)
    {
        string? routedAgent = null;
        _service.OnAgentActivity += (agent, _) => routedAgent = agent;

        _service.NotifyStatusUpdate(status);

        routedAgent.Should().Be(expectedAgent);
    }

    [Fact]
    public void Should_FallbackToOrchestrator_When_StatusMatchesNoKeywords()
    {
        // Arrange
        string? routedAgent = null;
        string? statusText = null;
        _service.OnAgentStatusUpdate += (agent, text) =>
        {
            routedAgent = agent;
            statusText = text;
        };

        // Act
        _service.NotifyStatusUpdate("Something completely unrelated happening");

        // Assert
        routedAgent.Should().Be("ChiefSoftwareArchitect");
        statusText.Should().Be("Something completely unrelated happening");
    }

    [Fact]
    public void Should_BeCaseInsensitive_When_MatchingKeywords()
    {
        // Arrange
        string? routedAgent = null;
        _service.OnAgentActivity += (agent, _) => routedAgent = agent;

        // Act — mixed case
        _service.NotifyStatusUpdate("INVENTORY check started");

        // Assert
        routedAgent.Should().Be("InventoryAgent");
    }

    [Fact]
    public void Should_FireBothEvents_When_KeywordMatched()
    {
        // Arrange
        bool activityFired = false;
        bool statusFired = false;
        _service.OnAgentActivity += (_, _) => activityFired = true;
        _service.OnAgentStatusUpdate += (_, _) => statusFired = true;

        // Act
        _service.NotifyStatusUpdate("Checking stock levels");

        // Assert
        activityFired.Should().BeTrue();
        statusFired.Should().BeTrue();
    }

    [Fact]
    public void Should_OnlyFireStatusUpdate_When_NoKeywordMatchedFallsToOrchestrator()
    {
        // Arrange
        bool activityFired = false;
        bool statusFired = false;
        _service.OnAgentActivity += (_, _) => activityFired = true;
        _service.OnAgentStatusUpdate += (_, _) => statusFired = true;

        // Act — unmatched text only fires status update
        _service.NotifyStatusUpdate("hello world");

        // Assert — no keyword match means no activity event, only status update
        activityFired.Should().BeFalse();
        statusFired.Should().BeTrue();
    }

    [Fact]
    public void Should_SetActiveTrue_When_KeywordMatchedInActivityEvent()
    {
        bool? isActive = null;
        _service.OnAgentActivity += (_, active) => isActive = active;

        _service.NotifyStatusUpdate("price optimization running");

        isActive.Should().BeTrue();
    }

    // ── Multiple Sequential Status Updates ──────────────────────────────

    [Fact]
    public void Should_RouteCorrectly_When_MultipleStatusUpdatesReceived()
    {
        // Arrange
        var agentSequence = new List<string>();
        _service.OnAgentActivity += (agent, _) => agentSequence.Add(agent);

        // Act
        _service.NotifyStatusUpdate("Orchestrating workflow");
        _service.NotifyStatusUpdate("Checking inventory levels");
        _service.NotifyStatusUpdate("Calculating pricing impact");
        _service.NotifyStatusUpdate("Market analysis started");

        // Assert
        agentSequence.Should().Equal(
            "ChiefSoftwareArchitect",
            "InventoryAgent",
            "PricingAgent",
            "MarketIntelAgent");
    }

    // ── Full Lifecycle ──────────────────────────────────────────────────

    [Fact]
    public void Should_FollowCorrectLifecycle_When_FullStreamSimulated()
    {
        // Arrange
        var events = new List<string>();
        _service.OnAgentActivity += (agent, active) =>
            events.Add($"activity:{agent}:{active}");
        _service.OnAgentStatusUpdate += (agent, _) =>
            events.Add($"status:{agent}");
        _service.OnAllAgentsIdle += () =>
            events.Add("idle");

        // Act — simulate a full stream lifecycle
        _service.NotifyStreamingStarted();
        _service.NotifyStatusUpdate("Routing to inventory");
        _service.NotifyStatusUpdate("Stock levels retrieved");
        _service.NotifyStreamingCompleted();

        // Assert
        events.Should().StartWith("activity:ChiefSoftwareArchitect:True");
        events.Should().EndWith("idle");
        events.Count.Should().BeGreaterThan(3);
    }

    [Fact]
    public void Should_NotThrow_When_NotifyStatusUpdateWithNoSubscribers()
    {
        var act = () => _service.NotifyStatusUpdate("some status");
        act.Should().NotThrow();
    }
}
