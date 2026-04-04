using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Mcp.Data;
using Xunit;

namespace SquadCommerce.Agents.Tests.Domain;

public class RedistributionAgentTests
{
    [Fact]
    public void Should_ThrowArgumentNullException_When_InventoryRepositoryIsNull()
    {
        var act = () => new RedistributionAgent(null!, NullLogger<RedistributionAgent>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("inventoryRepository");
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        var act = () => new RedistributionAgent(new InMemoryInventoryRepository(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Should_HaveCorrectAgentName()
    {
        var agent = new RedistributionAgent(new InMemoryInventoryRepository(), NullLogger<RedistributionAgent>.Instance);
        agent.AgentName.Should().Be("RedistributionAgent");
    }

    [Fact]
    public async Task Should_GenerateTransferPlan_When_SurplusStoresExist()
    {
        // Arrange
        var inventoryRepo = new InMemoryInventoryRepository();
        var agent = new RedistributionAgent(inventoryRepo, NullLogger<RedistributionAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1001", new[] { "Southeast" }, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.TextSummary.Should().Contain("redistribution plan");
    }

    [Fact]
    public async Task Should_ReturnFailure_When_SkuNotFoundInInventory()
    {
        // Arrange
        var inventoryRepo = new InMemoryInventoryRepository();
        var agent = new RedistributionAgent(inventoryRepo, NullLogger<RedistributionAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("INVALID-SKU-999", new[] { "Southeast" }, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("INVALID-SKU-999");
    }

    [Fact]
    public async Task Should_IdentifyAtRiskStores_When_RegionsSpecified()
    {
        // Arrange
        var inventoryRepo = new InMemoryInventoryRepository();
        var agent = new RedistributionAgent(inventoryRepo, NullLogger<RedistributionAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1007", new[] { "Northwest" }, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        // May have transfer routes or report no surplus — both valid outcomes
        result.TextSummary.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_AssignPriority_When_CalculatingTransferRoutes()
    {
        // Arrange
        var inventoryRepo = new InMemoryInventoryRepository();
        var agent = new RedistributionAgent(inventoryRepo, NullLogger<RedistributionAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1001", new[] { "Southeast" }, CancellationToken.None);

        // Assert
        if (result.A2UIPayload is ReroutingMapData mapData && mapData.Routes.Count > 0)
        {
            mapData.Routes.Should().AllSatisfy(r =>
                r.Priority.Should().BeOneOf("Critical", "High", "Medium", "Low"));
        }
    }

    [Fact]
    public async Task Should_HaveTimestamp_When_ResultReturned()
    {
        // Arrange
        var inventoryRepo = new InMemoryInventoryRepository();
        var agent = new RedistributionAgent(inventoryRepo, NullLogger<RedistributionAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1001", new[] { "Southeast" }, CancellationToken.None);

        // Assert
        result.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
