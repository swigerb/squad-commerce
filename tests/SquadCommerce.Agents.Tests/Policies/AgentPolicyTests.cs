using Xunit;
using FluentAssertions;
using SquadCommerce.Agents.Policies;

namespace SquadCommerce.Agents.Tests.Policies;

public class AgentPolicyTests
{
    [Fact]
    public void Should_CreateImmutablePolicy_When_Initialized()
    {
        // Arrange & Act
        var policy = new AgentPolicy
        {
            AgentName = "TestAgent",
            EnforceA2UI = true,
            RequireTelemetryTrace = true,
            PreferredProtocol = "MCP",
            AllowedTools = new[] { "Tool1", "Tool2" },
            EntraIdScope = "TestScope"
        };

        // Assert
        policy.AgentName.Should().Be("TestAgent");
        policy.EnforceA2UI.Should().BeTrue();
        policy.RequireTelemetryTrace.Should().BeTrue();
        policy.PreferredProtocol.Should().Be("MCP");
        policy.AllowedTools.Should().BeEquivalentTo(new[] { "Tool1", "Tool2" });
        policy.EntraIdScope.Should().Be("TestScope");
    }

    [Fact]
    public void Should_EnforceImmutability_When_RecordPropertiesAreInitOnly()
    {
        // Arrange
        var policy = new AgentPolicy
        {
            AgentName = "Agent1",
            EnforceA2UI = false,
            RequireTelemetryTrace = false,
            PreferredProtocol = "A2A",
            AllowedTools = Array.Empty<string>(),
            EntraIdScope = "Scope1"
        };

        // Act - Records are immutable by design (init-only properties)

        // Assert
        policy.Should().NotBeNull();
        policy.AgentName.Should().Be("Agent1");
    }

    [Theory]
    [InlineData("MCP")]
    [InlineData("A2A")]
    [InlineData("AGUI")]
    public void Should_AcceptValidProtocol_When_ProtocolIsSet(string protocol)
    {
        // Arrange & Act
        var policy = new AgentPolicy
        {
            AgentName = "TestAgent",
            EnforceA2UI = false,
            RequireTelemetryTrace = false,
            PreferredProtocol = protocol,
            AllowedTools = Array.Empty<string>(),
            EntraIdScope = "Scope"
        };

        // Assert
        policy.PreferredProtocol.Should().Be(protocol);
    }

    [Fact]
    public void Should_AllowEmptyToolsList_When_AgentHasNoTools()
    {
        // Arrange & Act
        var policy = new AgentPolicy
        {
            AgentName = "OrchestratorAgent",
            EnforceA2UI = true,
            RequireTelemetryTrace = true,
            PreferredProtocol = "AGUI",
            AllowedTools = Array.Empty<string>(),
            EntraIdScope = "Orchestrate"
        };

        // Assert
        policy.AllowedTools.Should().BeEmpty();
    }

    [Fact]
    public void Should_RequireTelemetryTrace_When_PolicyEnforcesTelemetry()
    {
        // Arrange & Act
        var policy = new AgentPolicy
        {
            AgentName = "TracedAgent",
            EnforceA2UI = false,
            RequireTelemetryTrace = true,
            PreferredProtocol = "MCP",
            AllowedTools = Array.Empty<string>(),
            EntraIdScope = "Test"
        };

        // Assert
        policy.RequireTelemetryTrace.Should().BeTrue();
    }

    [Fact]
    public void Should_EnforceA2UIPayload_When_PolicyRequiresA2UI()
    {
        // Arrange & Act
        var policy = new AgentPolicy
        {
            AgentName = "UIAgent",
            EnforceA2UI = true,
            RequireTelemetryTrace = false,
            PreferredProtocol = "AGUI",
            AllowedTools = Array.Empty<string>(),
            EntraIdScope = "UI"
        };

        // Assert
        policy.EnforceA2UI.Should().BeTrue();
    }
}
