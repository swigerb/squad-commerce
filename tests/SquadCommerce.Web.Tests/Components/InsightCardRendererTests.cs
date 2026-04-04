using System.Text.Json;
using Bunit;
using FluentAssertions;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Web.Components.A2UI;

namespace SquadCommerce.Web.Tests.Components;

public class InsightCardRendererTests : TestContext
{
    private static A2UIPayload CreateInsightPayload(
        string title = "Margin Impact",
        string keyMetric = "-12.5%",
        string metricLabel = "margin change",
        string trendDirection = "down",
        string summary = "Pricing drop impacts margins.",
        string? actionLabel = null,
        string? severity = "info")
    {
        var data = new InsightCardData
        {
            Title = title,
            KeyMetric = keyMetric,
            MetricLabel = metricLabel,
            TrendDirection = trendDirection,
            Summary = summary,
            ActionLabel = actionLabel,
            Severity = severity
        };

        // Serialize and re-parse so InsightCardRenderer sees a JsonElement (like real SSE pipeline)
        var json = JsonSerializer.Serialize(data);
        var jsonElement = JsonDocument.Parse(json).RootElement.Clone();

        return new A2UIPayload
        {
            Type = "insight",
            RenderAs = "InsightCard",
            Data = jsonElement
        };
    }

    // ── Basic Rendering ─────────────────────────────────────────────────

    [Fact]
    public void Should_RenderTitle_When_PayloadProvided()
    {
        var cut = Render<InsightCardRenderer>(p =>
            p.Add(x => x.Payload, CreateInsightPayload(title: "Revenue Alert")));

        cut.Find(".insight-title").TextContent.Should().Be("Revenue Alert");
    }

    [Fact]
    public void Should_RenderKeyMetric_When_PayloadProvided()
    {
        var cut = Render<InsightCardRenderer>(p =>
            p.Add(x => x.Payload, CreateInsightPayload(keyMetric: "$4,200")));

        cut.Find(".insight-key-metric").TextContent.Should().Contain("$4,200");
    }

    [Fact]
    public void Should_RenderMetricLabel_When_PayloadProvided()
    {
        var cut = Render<InsightCardRenderer>(p =>
            p.Add(x => x.Payload, CreateInsightPayload(metricLabel: "revenue delta")));

        cut.Find(".insight-metric-label").TextContent.Should().Contain("revenue delta");
    }

    [Fact]
    public void Should_RenderSummary_When_PayloadProvided()
    {
        var cut = Render<InsightCardRenderer>(p =>
            p.Add(x => x.Payload, CreateInsightPayload(summary: "Detailed analysis here.")));

        cut.Find(".insight-summary").TextContent.Should().Contain("Detailed analysis here.");
    }

    // ── Trend Direction ─────────────────────────────────────────────────

    [Theory]
    [InlineData("up", "▲", "trend-up")]
    [InlineData("down", "▼", "trend-down")]
    [InlineData("neutral", "─", "trend-neutral")]
    public void Should_RenderCorrectTrendArrow_When_TrendDirectionSet(
        string trend, string expectedArrow, string expectedClass)
    {
        var cut = Render<InsightCardRenderer>(p =>
            p.Add(x => x.Payload, CreateInsightPayload(trendDirection: trend)));

        var arrow = cut.Find(".trend-arrow");
        arrow.TextContent.Should().Contain(expectedArrow);
        arrow.ClassList.Should().Contain(expectedClass);
    }

    [Fact]
    public void Should_ApplyTrendClassToMetric_When_TrendDirectionSet()
    {
        var cut = Render<InsightCardRenderer>(p =>
            p.Add(x => x.Payload, CreateInsightPayload(trendDirection: "up")));

        cut.Find(".insight-key-metric").ClassList.Should().Contain("trend-up");
    }

    // ── Severity Classes ────────────────────────────────────────────────

    [Theory]
    [InlineData("info", "severity-info", "💡")]
    [InlineData("warning", "severity-warning", "⚠️")]
    [InlineData("critical", "severity-critical", "🚨")]
    [InlineData("success", "severity-success", "✅")]
    public void Should_RenderCorrectSeverity_When_SeveritySet(
        string severity, string expectedClass, string expectedIcon)
    {
        var cut = Render<InsightCardRenderer>(p =>
            p.Add(x => x.Payload, CreateInsightPayload(severity: severity)));

        var card = cut.Find(".insight-card");
        card.ClassList.Should().Contain(expectedClass);

        var icon = cut.Find(".insight-icon");
        icon.TextContent.Should().Contain(expectedIcon);
    }

    [Fact]
    public void Should_DefaultToInfo_When_SeverityNotProvided()
    {
        var cut = Render<InsightCardRenderer>(p =>
            p.Add(x => x.Payload, CreateInsightPayload(severity: null)));

        var card = cut.Find(".insight-card");
        card.ClassList.Should().Contain("severity-info");
    }

    // ── Action Button ───────────────────────────────────────────────────

    [Fact]
    public void Should_RenderActionButton_When_ActionLabelProvided()
    {
        var cut = Render<InsightCardRenderer>(p =>
            p.Add(x => x.Payload, CreateInsightPayload(actionLabel: "View Details")));

        var button = cut.Find(".insight-action-btn");
        button.TextContent.Should().Contain("View Details");
        button.GetAttribute("aria-label").Should().Be("View Details");
    }

    [Fact]
    public void Should_NotRenderActionButton_When_ActionLabelNull()
    {
        var cut = Render<InsightCardRenderer>(p =>
            p.Add(x => x.Payload, CreateInsightPayload(actionLabel: null)));

        cut.FindAll(".insight-action-btn").Should().BeEmpty();
    }

    [Theory]
    [InlineData("info", "btn-info")]
    [InlineData("warning", "btn-warning")]
    [InlineData("critical", "btn-critical")]
    [InlineData("success", "btn-success")]
    public void Should_ApplyCorrectButtonClass_When_SeveritySet(
        string severity, string expectedBtnClass)
    {
        var cut = Render<InsightCardRenderer>(p =>
            p.Add(x => x.Payload, CreateInsightPayload(
                actionLabel: "Click me", severity: severity)));

        var button = cut.Find(".insight-action-btn");
        button.ClassList.Should().Contain(expectedBtnClass);
    }

    // ── Null / Missing Payload ──────────────────────────────────────────

    [Fact]
    public void Should_ShowLoadingSpinner_When_PayloadIsNull()
    {
        var cut = Render<InsightCardRenderer>(p =>
            p.Add(x => x.Payload, (A2UIPayload?)null));

        cut.Find(".insight-placeholder").TextContent.Should().Contain("Loading insight");
        cut.FindAll(".spinner-border").Should().NotBeEmpty();
    }

    // ── Accessibility ───────────────────────────────────────────────────

    [Fact]
    public void Should_HaveArticleRole_When_Rendered()
    {
        var cut = Render<InsightCardRenderer>(p =>
            p.Add(x => x.Payload, CreateInsightPayload(title: "Test Card")));

        var card = cut.Find(".insight-card");
        card.GetAttribute("role").Should().Be("article");
    }

    [Fact]
    public void Should_HaveAriaLabel_When_Rendered()
    {
        var cut = Render<InsightCardRenderer>(p =>
            p.Add(x => x.Payload, CreateInsightPayload(title: "Stock Alert")));

        var card = cut.Find(".insight-card");
        card.GetAttribute("aria-label").Should().Contain("Stock Alert");
    }

    // ── Typed Data Binding (non-JsonElement path) ───────────────────────

    [Fact]
    public void Should_RenderCorrectly_When_DataIsTypedInsightCardData()
    {
        var payload = new A2UIPayload
        {
            Type = "insight",
            RenderAs = "InsightCard",
            Data = new InsightCardData
            {
                Title = "Typed Card",
                KeyMetric = "99%",
                MetricLabel = "uptime",
                TrendDirection = "up",
                Summary = "All systems operational."
            }
        };

        var cut = Render<InsightCardRenderer>(p =>
            p.Add(x => x.Payload, payload));

        cut.Find(".insight-title").TextContent.Should().Be("Typed Card");
        cut.Find(".insight-key-metric").TextContent.Should().Contain("99%");
    }
}
