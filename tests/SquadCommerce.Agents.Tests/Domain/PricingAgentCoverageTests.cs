using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Mcp.Data;

namespace SquadCommerce.Agents.Tests.Domain;

/// <summary>
/// Coverage gap tests for PricingAgent edge cases.
/// </summary>
public class PricingAgentCoverageTests
{
    [Fact]
    public async Task Should_CalculateFourScenarios_When_ProposingPricing()
    {
        // Arrange
        var pricingRepo = new PricingRepository();
        var inventoryRepo = new InventoryRepository();
        var agent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1005", 114.99m, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var chart = (PricingImpactChartData)result.A2UIPayload!;
        
        chart.Scenarios.Should().HaveCount(4);
        chart.Scenarios.Should().Contain(s => s.ScenarioName == "Current Pricing");
        chart.Scenarios.Should().Contain(s => s.ScenarioName == "Match Competitor");
        chart.Scenarios.Should().Contain(s => s.ScenarioName == "Beat by 5%");
        chart.Scenarios.Should().Contain(s => s.ScenarioName == "Split Difference");
    }

    [Fact]
    public async Task Should_CalculateMarginForEachScenario_When_AnalyzingPricing()
    {
        // Arrange
        var pricingRepo = new PricingRepository();
        var inventoryRepo = new InventoryRepository();
        var agent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1003", 47.99m, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var chart = (PricingImpactChartData)result.A2UIPayload!;
        
        // All scenarios should have calculated margins
        chart.Scenarios.All(s => s.EstimatedMargin > 0).Should().BeTrue("all scenarios should have positive margins");
        chart.Scenarios.All(s => s.Price > 0).Should().BeTrue("all scenarios should have valid prices");
        chart.Scenarios.All(s => s.ProjectedUnitsSold > 0).Should().BeTrue("all scenarios should project units sold");
    }

    [Fact]
    public async Task Should_CalculateRevenueImpact_When_ProjectingScenarios()
    {
        // Arrange
        var pricingRepo = new PricingRepository();
        var inventoryRepo = new InventoryRepository();
        var agent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1008", 339.99m, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var chart = (PricingImpactChartData)result.A2UIPayload!;
        
        // Revenue should be calculated as Price × ProjectedUnitsSold
        foreach (var scenario in chart.Scenarios)
        {
            var expectedRevenue = scenario.Price * scenario.ProjectedUnitsSold;
            scenario.EstimatedRevenue.Should().BeGreaterThan(0);
            // Allow for rounding differences
            scenario.EstimatedRevenue.Should().BeApproximately(expectedRevenue, 100);
        }
    }

    [Theory]
    [InlineData("SKU-1001", 26.99)]
    [InlineData("SKU-1002", 11.99)]
    [InlineData("SKU-1006", 189.99)]
    public async Task Should_IncludeCompetitorPriceInPayload_When_Calculating(string sku, double competitorPriceDouble)
    {
        // Arrange
        var pricingRepo = new PricingRepository();
        var inventoryRepo = new InventoryRepository();
        var agent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);

        var competitorPrice = (decimal)competitorPriceDouble;

        // Act
        var result = await agent.ExecuteAsync(sku, competitorPrice, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var chart = (PricingImpactChartData)result.A2UIPayload!;
        
        chart.ProposedPrice.Should().Be(competitorPrice);
        chart.Sku.Should().Be(sku);
        chart.CurrentPrice.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Should_IncludeMarginDeltaInSummary_When_Analyzing()
    {
        // Arrange
        var pricingRepo = new PricingRepository();
        var inventoryRepo = new InventoryRepository();
        var agent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1007", 84.99m, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.TextSummary.Should().Contain("margin");
        result.TextSummary.Should().Contain("Margin impact");
        result.TextSummary.Should().Contain("%");
    }

    [Fact]
    public async Task Should_ProjectVolumeUplift_When_LoweringPrice()
    {
        // Arrange
        var pricingRepo = new PricingRepository();
        var inventoryRepo = new InventoryRepository();
        var agent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1004", 69.99m, CancellationToken.None); // Lower than current

        // Assert
        result.Success.Should().BeTrue();
        var chart = (PricingImpactChartData)result.A2UIPayload!;
        
        // "Beat by 5%" scenario should have highest volume projection
        var beatScenario = chart.Scenarios.First(s => s.ScenarioName == "Beat by 5%");
        var currentScenario = chart.Scenarios.First(s => s.ScenarioName == "Current Pricing");
        
        beatScenario.ProjectedUnitsSold.Should().BeGreaterThan(currentScenario.ProjectedUnitsSold,
            "lower price should project higher volume");
    }
}
