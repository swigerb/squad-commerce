using Bunit;
using FluentAssertions;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Web.Components.A2UI;

namespace SquadCommerce.Web.Tests.Components;

public class A2UIRendererTests : TestContext
{
    private static A2UIPayload CreatePayload(string renderAs, object? data = null)
    {
        return new A2UIPayload
        {
            Type = "visualization",
            RenderAs = renderAs,
            Data = data ?? new { }
        };
    }

    // ── Null Payload ────────────────────────────────────────────────────

    [Fact]
    public void Should_ShowNoDataMessage_When_PayloadIsNull()
    {
        var cut = Render<A2UIRenderer>(p =>
            p.Add(x => x.Payload, (A2UIPayload?)null));

        cut.Markup.Should().Contain("No data to display");
    }

    // ── Unknown RenderAs ────────────────────────────────────────────────

    [Fact]
    public void Should_ShowWarningAlert_When_RenderAsIsUnknown()
    {
        var cut = Render<A2UIRenderer>(p =>
            p.Add(x => x.Payload, CreatePayload("NonExistentComponent")));

        var alert = cut.Find(".alert-warning");
        alert.TextContent.Should().Contain("Unknown A2UI component");
        alert.TextContent.Should().Contain("NonExistentComponent");
        alert.GetAttribute("role").Should().Be("alert");
    }

    // ── Wrapper ─────────────────────────────────────────────────────────

    [Fact]
    public void Should_WrapInRendererDiv_When_Rendered()
    {
        var cut = Render<A2UIRenderer>(p =>
            p.Add(x => x.Payload, (A2UIPayload?)null));

        cut.Find(".a2ui-renderer").Should().NotBeNull();
    }

    // ── Protocol Badge Routing ──────────────────────────────────────────

    [Theory]
    [InlineData("RetailStockHeatmap", "MCP")]
    [InlineData("PricingImpactChart", "MCP")]
    [InlineData("SocialSentimentGraph", "MCP")]
    [InlineData("MarketComparisonGrid", "A2A")]
    [InlineData("ReroutingMap", "A2A")]
    [InlineData("SupplierRiskMatrix", "A2A")]
    [InlineData("DecisionAuditTrail", "A2UI")]
    [InlineData("InsightCard", "A2UI")]
    [InlineData("CampaignPreview", "A2UI")]
    [InlineData("AgentPipelineVisualizer", "AGUI")]
    [InlineData("InteractiveFloorplan", "HITL")]
    public void Should_RenderCorrectProtocolBadge_When_RenderAsSpecified(
        string renderAs, string expectedProtocol)
    {
        var cut = Render<A2UIRenderer>(p =>
            p.Add(x => x.Payload, CreatePayload(renderAs)));

        // A ProtocolBadge child component should be rendered with the right protocol.
        // The badge text maps protocol → label; verify via the rendered badge class.
        var expectedClass = expectedProtocol.ToLowerInvariant() switch
        {
            "mcp" => "badge-mcp",
            "a2a" => "badge-a2a",
            "agui" => "badge-agui",
            "a2ui" => "badge-a2ui",
            "hitl" => "badge-hitl",
            _ => ""
        };

        cut.Find(".protocol-badge").ClassList.Should().Contain(expectedClass);
    }

    // ── Each RenderAs routes to a child component (not the unknown alert) ──

    [Theory]
    [InlineData("RetailStockHeatmap")]
    [InlineData("PricingImpactChart")]
    [InlineData("MarketComparisonGrid")]
    [InlineData("DecisionAuditTrail")]
    [InlineData("AgentPipelineVisualizer")]
    [InlineData("InsightCard")]
    [InlineData("SocialSentimentGraph")]
    [InlineData("CampaignPreview")]
    [InlineData("ReroutingMap")]
    [InlineData("SupplierRiskMatrix")]
    [InlineData("InteractiveFloorplan")]
    public void Should_NotShowWarning_When_RenderAsIsKnown(string renderAs)
    {
        var cut = Render<A2UIRenderer>(p =>
            p.Add(x => x.Payload, CreatePayload(renderAs)));

        cut.FindAll(".alert-warning").Should().BeEmpty();
    }
}
