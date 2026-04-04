using Bunit;
using FluentAssertions;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Web.Components.A2UI;

namespace SquadCommerce.Web.Tests.Components;

public class ProtocolBadgeTests : TestContext
{
    // ── Rendering ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("MCP", "🔧", "MCP", "badge-mcp")]
    [InlineData("A2A", "🤝", "A2A", "badge-a2a")]
    [InlineData("AGUI", "📡", "AG-UI", "badge-agui")]
    [InlineData("A2UI", "✨", "Generative UI", "badge-a2ui")]
    [InlineData("HITL", "⚠️", "Action Required", "badge-hitl")]
    public void Should_RenderCorrectIconAndLabel_When_ProtocolSet(
        string protocol, string expectedIcon, string expectedLabel, string expectedClass)
    {
        // Act
        var cut = Render<ProtocolBadge>(parameters =>
            parameters.Add(p => p.Protocol, protocol));

        // Assert
        var badge = cut.Find(".protocol-badge");
        badge.ClassList.Should().Contain(expectedClass);
        badge.QuerySelector(".badge-icon")!.TextContent.Should().Contain(expectedIcon);
        badge.QuerySelector(".badge-text")!.TextContent.Should().Contain(expectedLabel);
    }

    // ── Accessibility ───────────────────────────────────────────────────

    [Theory]
    [InlineData("MCP", "Model Context Protocol")]
    [InlineData("A2A", "Agent-to-Agent Protocol")]
    [InlineData("AGUI", "Agent-UI Streaming Protocol")]
    [InlineData("A2UI", "Agent-to-UI Generative Rendering")]
    [InlineData("HITL", "Human-in-the-Loop approval required")]
    public void Should_HaveCorrectAriaLabel_When_ProtocolSet(
        string protocol, string expectedAriaLabel)
    {
        var cut = Render<ProtocolBadge>(parameters =>
            parameters.Add(p => p.Protocol, protocol));

        var badge = cut.Find(".protocol-badge");
        badge.GetAttribute("aria-label").Should().Be(expectedAriaLabel);
        badge.GetAttribute("role").Should().Be("status");
    }

    // ── Case Insensitivity ──────────────────────────────────────────────

    [Theory]
    [InlineData("mcp")]
    [InlineData("Mcp")]
    [InlineData("MCP")]
    public void Should_HandleCaseVariations_When_ProtocolProvided(string protocol)
    {
        var cut = Render<ProtocolBadge>(parameters =>
            parameters.Add(p => p.Protocol, protocol));

        var badge = cut.Find(".protocol-badge");
        badge.ClassList.Should().Contain("badge-mcp");
    }

    // ── Animation Classes ───────────────────────────────────────────────

    [Fact]
    public void Should_HaveFlashAnimation_When_ProtocolIsA2UI()
    {
        var cut = Render<ProtocolBadge>(parameters =>
            parameters.Add(p => p.Protocol, "A2UI"));

        var badge = cut.Find(".protocol-badge");
        badge.ClassList.Should().Contain("badge-flash");
    }

    [Fact]
    public void Should_HavePulseAnimation_When_ProtocolIsHITL()
    {
        var cut = Render<ProtocolBadge>(parameters =>
            parameters.Add(p => p.Protocol, "HITL"));

        var badge = cut.Find(".protocol-badge");
        badge.ClassList.Should().Contain("badge-pulse");
    }

    [Fact]
    public void Should_NotHaveSpecialAnimation_When_ProtocolIsMCP()
    {
        var cut = Render<ProtocolBadge>(parameters =>
            parameters.Add(p => p.Protocol, "MCP"));

        var badge = cut.Find(".protocol-badge");
        badge.ClassList.Should().NotContain("badge-flash");
        badge.ClassList.Should().NotContain("badge-pulse");
    }

    // ── Unknown Protocol ────────────────────────────────────────────────

    [Fact]
    public void Should_RenderGearIcon_When_ProtocolUnknown()
    {
        var cut = Render<ProtocolBadge>(parameters =>
            parameters.Add(p => p.Protocol, "UNKNOWN"));

        var icon = cut.Find(".badge-icon");
        icon.TextContent.Should().Contain("⚙️");
    }

    [Fact]
    public void Should_RenderProtocolName_When_ProtocolUnknown()
    {
        var cut = Render<ProtocolBadge>(parameters =>
            parameters.Add(p => p.Protocol, "CUSTOM"));

        var text = cut.Find(".badge-text");
        text.TextContent.Should().Contain("CUSTOM");
    }

    [Fact]
    public void Should_HaveGenericAriaLabel_When_ProtocolUnknown()
    {
        var cut = Render<ProtocolBadge>(parameters =>
            parameters.Add(p => p.Protocol, "XPROTO"));

        var badge = cut.Find(".protocol-badge");
        badge.GetAttribute("aria-label").Should().Be("Protocol: XPROTO");
    }

    // ── All Protocols Have Enter Animation ──────────────────────────────

    [Theory]
    [InlineData("MCP")]
    [InlineData("A2A")]
    [InlineData("AGUI")]
    [InlineData("A2UI")]
    [InlineData("HITL")]
    public void Should_HaveEnterAnimation_When_AnyKnownProtocol(string protocol)
    {
        var cut = Render<ProtocolBadge>(parameters =>
            parameters.Add(p => p.Protocol, protocol));

        var badge = cut.Find(".protocol-badge");
        badge.ClassList.Should().Contain("badge-enter");
    }
}
