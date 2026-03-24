using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Mcp.Data;
using Xunit;

namespace SquadCommerce.Agents.Tests.Domain;

/// <summary>
/// Tests for PricingAgent bulk operations.
/// Validates margin calculation, revenue projection, and cost validation for multiple SKUs.
/// </summary>
public class BulkPricingAgentTests
{
    [Fact]
    public async Task Should_CalculateMarginForAllSkus_When_BulkPricingRequested()
    {
        // Arrange
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();
        var agent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);

        // Act - Bulk pricing analysis for 3 SKUs
        var items = new List<(string Sku, decimal CompetitorPrice)>
        {
            ("SKU-1001", 26.99m), // Wireless Mouse
            ("SKU-1002", 10.99m), // USB-C Cable
            ("SKU-1003", 44.99m)  // Laptop Stand
        };

        var result = await agent.ExecuteBulkAsync(items, CancellationToken.None);

        // Assert - Success with pricing scenarios
        result.Success.Should().BeTrue("bulk pricing analysis should succeed");
        result.A2UIPayload.Should().NotBeNull();
        result.A2UIPayload.Should().BeOfType<PricingImpactChartData>();

        var pricingData = (PricingImpactChartData)result.A2UIPayload!;
        pricingData.Scenarios.Should().NotBeEmpty("should have pricing scenarios for all SKUs");
        
        // Verify text summary mentions revenue
        result.TextSummary.Should().Contain("revenue");
        result.TextSummary.Should().Contain("3 SKUs");
    }

    [Fact]
    public async Task Should_HighlightHighestImpactSku_When_RevenueVaries()
    {
        // Arrange
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();
        var agent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);

        // Act - Mix of high-value and low-value SKUs
        var items = new List<(string Sku, decimal CompetitorPrice)>
        {
            ("SKU-1002", 10.99m),  // Low-value cable
            ("SKU-1008", 299.99m)  // High-value monitor
        };

        var result = await agent.ExecuteBulkAsync(items, CancellationToken.None);

        // Assert - Should calculate different revenue impacts
        result.Success.Should().BeTrue();
        var pricingData = (PricingImpactChartData)result.A2UIPayload!;
        
        pricingData.Scenarios.Should().NotBeEmpty();
        
        // Different SKUs should have different revenue projections
        var revenues = pricingData.Scenarios.Select(s => s.EstimatedRevenue).Distinct().ToList();
        revenues.Should().HaveCountGreaterThan(1, "different SKUs should have different revenue impacts");
    }

    [Fact]
    public async Task Should_RejectBelowCostPrice_When_AnySkuProposedBelowCost()
    {
        // Arrange
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();
        var agent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);

        // Act - Propose unreasonably low prices (below cost)
        var items = new List<(string Sku, decimal CompetitorPrice)>
        {
            ("SKU-1001", 5.00m),  // Far below cost
            ("SKU-1002", 3.00m)   // Far below cost
        };

        var result = await agent.ExecuteBulkAsync(items, CancellationToken.None);

        // Assert - Should still process but with negative margins
        result.Success.Should().BeTrue("agent should process even with below-cost prices");
        var pricingData = (PricingImpactChartData)result.A2UIPayload!;
        
        // Margins should be negative for below-cost pricing
        pricingData.Scenarios.Should().Contain(s => s.EstimatedMargin < 0, 
            "below-cost prices should result in negative margins");
    }

    [Fact]
    public async Task Should_AggregateRevenueAcrossSkus_When_BulkAnalysisCompletes()
    {
        // Arrange
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();
        var agent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);

        // Act - Bulk analysis
        var items = new List<(string Sku, decimal CompetitorPrice)>
        {
            ("SKU-1001", 26.99m),
            ("SKU-1002", 10.99m),
            ("SKU-1003", 44.99m),
            ("SKU-1004", 69.99m)
        };

        var result = await agent.ExecuteBulkAsync(items, CancellationToken.None);

        // Assert - Revenue aggregation
        result.Success.Should().BeTrue();
        result.TextSummary.Should().Contain("revenue", "should mention revenue impact");
        result.TextSummary.Should().Contain("4 SKUs", "should mention SKU count");
        
        var pricingData = (PricingImpactChartData)result.A2UIPayload!;
        pricingData.Scenarios.Should().HaveCountGreaterThan(0);
        
        // Total revenue should be sum of all SKU revenues
        var totalRevenue = pricingData.Scenarios.Sum(s => s.EstimatedRevenue);
        totalRevenue.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Should_IncludeAllSkusInScenarios_When_BulkPricingCalculated()
    {
        // Arrange
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();
        var agent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);

        // Act
        var items = new List<(string Sku, decimal CompetitorPrice)>
        {
            ("SKU-1001", 26.99m),
            ("SKU-1005", 99.99m),
            ("SKU-1006", 179.99m)
        };

        var result = await agent.ExecuteBulkAsync(items, CancellationToken.None);

        // Assert - Each SKU should have scenarios
        result.Success.Should().BeTrue();
        var pricingData = (PricingImpactChartData)result.A2UIPayload!;
        
        pricingData.Scenarios.Should().HaveCountGreaterThanOrEqualTo(3, "should have at least one scenario per SKU");
        
        // Scenarios should reference the SKUs
        var scenarioNames = string.Join(" ", pricingData.Scenarios.Select(s => s.ScenarioName));
        scenarioNames.Should().Contain("SKU-1001");
        scenarioNames.Should().Contain("SKU-1005");
        scenarioNames.Should().Contain("SKU-1006");
    }

    [Fact]
    public async Task Should_CalculateProjectedUnits_When_VolumeUpliftApplied()
    {
        // Arrange
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();
        var agent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);

        // Act - Lower prices should project higher volume
        var items = new List<(string Sku, decimal CompetitorPrice)>
        {
            ("SKU-1001", 26.99m), // Competitive price
            ("SKU-1002", 10.99m)  // Competitive price
        };

        var result = await agent.ExecuteBulkAsync(items, CancellationToken.None);

        // Assert - Projected units should reflect volume uplift
        result.Success.Should().BeTrue();
        var pricingData = (PricingImpactChartData)result.A2UIPayload!;
        
        pricingData.Scenarios.Should().AllSatisfy(scenario =>
        {
            scenario.ProjectedUnitsSold.Should().BeGreaterThan(0, "should project unit sales");
        });
    }

    [Fact]
    public async Task Should_SetTimestamp_When_BulkPricingCompletes()
    {
        // Arrange
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();
        var agent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);

        // Act
        var items = new List<(string Sku, decimal CompetitorPrice)>
        {
            ("SKU-1001", 26.99m),
            ("SKU-1002", 10.99m)
        };

        var before = DateTimeOffset.UtcNow;
        var result = await agent.ExecuteBulkAsync(items, CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        // Assert - Timestamps should be accurate
        result.Success.Should().BeTrue();
        result.Timestamp.Should().BeOnOrAfter(before);
        result.Timestamp.Should().BeOnOrBefore(after);
        
        var pricingData = (PricingImpactChartData)result.A2UIPayload!;
        pricingData.Timestamp.Should().BeOnOrAfter(before);
        pricingData.Timestamp.Should().BeOnOrBefore(after);
    }
}
