using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Mcp.Data;

namespace SquadCommerce.Agents.Tests.Domain;

/// <summary>
/// Coverage gap tests for InventoryAgent edge cases.
/// </summary>
public class InventoryAgentCoverageTests
{
    [Fact]
    public async Task Should_CalculateStockStatus_When_UnitsAboveReorderPoint()
    {
        // Arrange
        var repo = new InventoryRepository();
        var agent = new InventoryAgent(repo, NullLogger<InventoryAgent>.Instance);

        // Act - Query SKU with high stock
        var result = await agent.ExecuteAsync("SKU-1002", CancellationToken.None); // USB-C Cable has 120 units

        // Assert
        result.Success.Should().BeTrue();
        var heatmap = (RetailStockHeatmapData)result.A2UIPayload!;
        heatmap.Stores.Should().Contain(s => s.StockStatus == "High", "should have high stock stores");
        heatmap.Stores.Should().Contain(s => s.StockStatus == "Normal", "should have normal stock stores");
    }

    [Fact]
    public async Task Should_IdentifyLowStock_When_BelowReorderPoint()
    {
        // Arrange
        var repo = new InventoryRepository();
        var agent = new InventoryAgent(repo, NullLogger<InventoryAgent>.Instance);

        // Act - Query SKU with some low stock stores
        var result = await agent.ExecuteAsync("SKU-1007", CancellationToken.None); // External SSD has low stock

        // Assert
        result.Success.Should().BeTrue();
        var heatmap = (RetailStockHeatmapData)result.A2UIPayload!;
        
        // Should have at least one low stock store
        var lowStockStores = heatmap.Stores.Where(s => s.StockStatus == "Low").ToList();
        result.TextSummary.Should().Contain("store(s) below reorder point");
    }

    [Theory]
    [InlineData("SKU-1001")]
    [InlineData("SKU-1003")]
    [InlineData("SKU-1006")]
    public async Task Should_ReturnFiveStores_When_ValidSkuProvided(string sku)
    {
        // Arrange
        var repo = new InventoryRepository();
        var agent = new InventoryAgent(repo, NullLogger<InventoryAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync(sku, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var heatmap = (RetailStockHeatmapData)result.A2UIPayload!;
        heatmap.Stores.Should().HaveCount(5, "all SKUs should be in 5 stores");
        heatmap.Sku.Should().Be(sku);
        heatmap.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Should_IncludeStoreNames_When_GeneratingHeatmap()
    {
        // Arrange
        var repo = new InventoryRepository();
        var agent = new InventoryAgent(repo, NullLogger<InventoryAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1004", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var heatmap = (RetailStockHeatmapData)result.A2UIPayload!;
        
        heatmap.Stores.Should().Contain(s => s.StoreName == "Downtown Flagship");
        heatmap.Stores.Should().Contain(s => s.StoreName == "Suburban Mall");
        heatmap.Stores.Should().Contain(s => s.StoreName == "Airport Terminal");
        heatmap.Stores.Should().Contain(s => s.StoreName == "University District");
        heatmap.Stores.Should().Contain(s => s.StoreName == "Waterfront Plaza");
    }

    [Fact]
    public async Task Should_CalculateTotalUnits_When_SummarizingInventory()
    {
        // Arrange
        var repo = new InventoryRepository();
        var agent = new InventoryAgent(repo, NullLogger<InventoryAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1001", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.TextSummary.Should().Contain("total units");
        result.TextSummary.Should().Contain("across");
        
        var heatmap = (RetailStockHeatmapData)result.A2UIPayload!;
        var totalUnits = heatmap.Stores.Sum(s => s.UnitsOnHand);
        totalUnits.Should().BeGreaterThan(0);
    }
}
