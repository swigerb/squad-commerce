using Xunit;
using FluentAssertions;
using SquadCommerce.Agents.Policies;

namespace SquadCommerce.Agents.Tests.Policies;

public class AgentPolicyRegistryTests
{
    [Fact]
    public void Should_ReturnOrchestratorPolicy_When_QueryingChiefSoftwareArchitect()
    {
        // Arrange & Act
        var policy = AgentPolicyRegistry.GetPolicyByName("ChiefSoftwareArchitect");

        // Assert
        policy.Should().NotBeNull();
        policy!.AgentName.Should().Be("ChiefSoftwareArchitect");
        policy.PreferredProtocol.Should().Be("AGUI");
        policy.AllowedTools.Should().BeEmpty();
        policy.EntraIdScope.Should().Be("SquadCommerce.Orchestrate");
        policy.EnforceA2UI.Should().BeTrue();
        policy.RequireTelemetryTrace.Should().BeTrue();
    }

    [Fact]
    public void Should_ReturnInventoryPolicy_When_QueryingInventoryAgent()
    {
        // Arrange & Act
        var policy = AgentPolicyRegistry.GetPolicyByName("InventoryAgent");

        // Assert
        policy.Should().NotBeNull();
        policy!.AgentName.Should().Be("InventoryAgent");
        policy.PreferredProtocol.Should().Be("MCP");
        policy.AllowedTools.Should().Contain("GetInventoryLevels");
        policy.EntraIdScope.Should().Be("SquadCommerce.Inventory.Read");
    }

    [Fact]
    public void Should_ReturnPricingPolicy_When_QueryingPricingAgent()
    {
        // Arrange & Act
        var policy = AgentPolicyRegistry.GetPolicyByName("PricingAgent");

        // Assert
        policy.Should().NotBeNull();
        policy!.AgentName.Should().Be("PricingAgent");
        policy.PreferredProtocol.Should().Be("MCP");
        policy.AllowedTools.Should().Contain("GetInventoryLevels");
        policy.AllowedTools.Should().Contain("UpdateStorePricing");
        policy.EntraIdScope.Should().Be("SquadCommerce.Pricing.ReadWrite");
    }

    [Fact]
    public void Should_ReturnMarketIntelPolicy_When_QueryingMarketIntelAgent()
    {
        // Arrange & Act
        var policy = AgentPolicyRegistry.GetPolicyByName("MarketIntelAgent");

        // Assert
        policy.Should().NotBeNull();
        policy!.AgentName.Should().Be("MarketIntelAgent");
        policy.PreferredProtocol.Should().Be("A2A");
        policy.AllowedTools.Should().BeEmpty(); // Uses A2A, not MCP tools
        policy.EntraIdScope.Should().Be("SquadCommerce.MarketIntel.Read");
    }

    [Fact]
    public void Should_ReturnNull_When_QueryingUnknownAgent()
    {
        // Arrange & Act
        var policy = AgentPolicyRegistry.GetPolicyByName("UnknownAgent");

        // Assert
        policy.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Should_ReturnNull_When_QueryingWithInvalidAgentName(string? agentName)
    {
        // Arrange & Act
        var policy = AgentPolicyRegistry.GetPolicyByName(agentName!);

        // Assert
        policy.Should().BeNull();
    }

    [Fact]
    public void Should_ReturnAllFourPolicies_When_RegistryIsQueried()
    {
        // Arrange
        var expectedAgents = new[]
        {
            "ChiefSoftwareArchitect",
            "InventoryAgent",
            "PricingAgent",
            "MarketIntelAgent"
        };

        // Act
        var policies = expectedAgents.Select(name => AgentPolicyRegistry.GetPolicyByName(name)).ToList();

        // Assert
        policies.Should().AllSatisfy(p => p.Should().NotBeNull());
        policies.Select(p => p!.AgentName).Should().BeEquivalentTo(expectedAgents);
    }

    [Fact]
    public void Should_AllPoliciesEnforceA2UI_When_RegistryIsQueried()
    {
        // Arrange
        var agentNames = new[]
        {
            "ChiefSoftwareArchitect",
            "InventoryAgent",
            "PricingAgent",
            "MarketIntelAgent"
        };

        // Act
        var policies = agentNames.Select(name => AgentPolicyRegistry.GetPolicyByName(name)).ToList();

        // Assert
        policies.Should().AllSatisfy(p => p!.EnforceA2UI.Should().BeTrue());
    }

    [Fact]
    public void Should_AllPoliciesRequireTelemetry_When_RegistryIsQueried()
    {
        // Arrange
        var agentNames = new[]
        {
            "ChiefSoftwareArchitect",
            "InventoryAgent",
            "PricingAgent",
            "MarketIntelAgent"
        };

        // Act
        var policies = agentNames.Select(name => AgentPolicyRegistry.GetPolicyByName(name)).ToList();

        // Assert
        policies.Should().AllSatisfy(p => p!.RequireTelemetryTrace.Should().BeTrue());
    }
}
