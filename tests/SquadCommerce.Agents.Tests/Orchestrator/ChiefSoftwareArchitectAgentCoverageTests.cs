using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using SquadCommerce.Agents.Orchestrator;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Mcp.Data;
using SquadCommerce.A2A;
using SquadCommerce.A2A.Validation;
using SquadCommerce.Contracts.Models;

namespace SquadCommerce.Agents.Tests.Orchestrator;

/// <summary>
/// Coverage gap tests for ChiefSoftwareArchitectAgent orchestrator.
/// These tests cover edge cases and error scenarios not yet tested.
/// </summary>
public class ChiefSoftwareArchitectAgentCoverageTests
{
    [Fact]
    public async Task Should_HandleAllAgentsSuccess_When_WorkflowCompletes()
    {
        // Arrange
        var inventoryRepo = new InMemoryInventoryRepository();
        var pricingRepo = new InMemoryPricingRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);

        var inventoryAgent = new InventoryAgent(inventoryRepo, NullLogger<InventoryAgent>.Instance);
        var pricingAgent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);
        var marketIntelAgent = new MarketIntelAgent(a2aClient, validator, NullLogger<MarketIntelAgent>.Instance);

        var orchestrator = new ChiefSoftwareArchitectAgent(
            inventoryAgent,
            pricingAgent,
            marketIntelAgent,
            NullLogger<ChiefSoftwareArchitectAgent>.Instance);

        // Act
        var result = await orchestrator.ProcessCompetitorPriceDropAsync("SKU-1008", 329.99m, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.AgentResults.Should().HaveCount(3);
        result.AgentResults.All(r => r.Success).Should().BeTrue("all agents should succeed in happy path");
        result.WorkflowDuration.Should().BeGreaterThan(TimeSpan.Zero);
        result.ExecutiveSummary.Should().Contain("SKU-1008");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Should_CaptureWorkflowDuration_When_OrchestrationCompletes()
    {
        // Arrange
        var inventoryRepo = new InMemoryInventoryRepository();
        var pricingRepo = new InMemoryPricingRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);

        var inventoryAgent = new InventoryAgent(inventoryRepo, NullLogger<InventoryAgent>.Instance);
        var pricingAgent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);
        var marketIntelAgent = new MarketIntelAgent(a2aClient, validator, NullLogger<MarketIntelAgent>.Instance);

        var orchestrator = new ChiefSoftwareArchitectAgent(
            inventoryAgent,
            pricingAgent,
            marketIntelAgent,
            NullLogger<ChiefSoftwareArchitectAgent>.Instance);

        // Act
        var startTime = DateTimeOffset.UtcNow;
        var result = await orchestrator.ProcessCompetitorPriceDropAsync("SKU-1004", 74.99m, CancellationToken.None);
        var endTime = DateTimeOffset.UtcNow;

        // Assert
        result.WorkflowDuration.Should().BeGreaterThan(TimeSpan.Zero);
        result.WorkflowDuration.Should().BeLessThan(endTime - startTime + TimeSpan.FromSeconds(1));
        result.Timestamp.Should().BeAfter(startTime);
        result.Timestamp.Should().BeOnOrBefore(endTime);
    }

    [Fact]
    public async Task Should_IncludeAllAgentResultsInSynthesis_When_Summarizing()
    {
        // Arrange
        var inventoryRepo = new InMemoryInventoryRepository();
        var pricingRepo = new InMemoryPricingRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);

        var inventoryAgent = new InventoryAgent(inventoryRepo, NullLogger<InventoryAgent>.Instance);
        var pricingAgent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);
        var marketIntelAgent = new MarketIntelAgent(a2aClient, validator, NullLogger<MarketIntelAgent>.Instance);

        var orchestrator = new ChiefSoftwareArchitectAgent(
            inventoryAgent,
            pricingAgent,
            marketIntelAgent,
            NullLogger<ChiefSoftwareArchitectAgent>.Instance);

        // Act
        var result = await orchestrator.ProcessCompetitorPriceDropAsync("SKU-1005", 112.99m, CancellationToken.None);

        // Assert - Executive summary should reference all agent outputs
        result.ExecutiveSummary.Should().Contain("Recommendation");
        result.ExecutiveSummary.Should().Contain("SKU-1005");
        result.ExecutiveSummary.Should().NotBeEmpty();
        result.AgentResults.Should().HaveCount(3);
    }
}
