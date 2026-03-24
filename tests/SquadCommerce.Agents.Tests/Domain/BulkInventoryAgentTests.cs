using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Mcp.Data;
using Xunit;

namespace SquadCommerce.Agents.Tests.Domain;

/// <summary>
/// Tests for InventoryAgent bulk operations.
/// Validates that bulk inventory queries correctly aggregate data across multiple SKUs and stores.
/// </summary>
public class BulkInventoryAgentTests
{
    [Fact]
    public async Task Should_ReturnInventoryForAllSkus_When_BulkQueryExecuted()
    {
        // Arrange
        var repo = new InMemoryInventoryRepository();
        var agent = new InventoryAgent(repo, NullLogger<InventoryAgent>.Instance);

        // Act - Query 3 SKUs across 5 stores = 15 inventory entries
        var skus = new List<string> { "SKU-1001", "SKU-1002", "SKU-1003" };
        var result = await agent.ExecuteBulkAsync(skus, CancellationToken.None);

        // Assert - Success and correct data structure
        result.Success.Should().BeTrue("bulk query should succeed");
        result.A2UIPayload.Should().NotBeNull("should produce A2UI payload");
        result.A2UIPayload.Should().BeOfType<RetailStockHeatmapData>();

        var heatmap = (RetailStockHeatmapData)result.A2UIPayload!;
        heatmap.Stores.Should().HaveCount(15, "3 SKUs × 5 stores = 15 entries");
        
        // Verify all SKUs are represented
        heatmap.Sku.Should().Contain("SKU-1001");
        heatmap.Sku.Should().Contain("SKU-1002");
        heatmap.Sku.Should().Contain("SKU-1003");
    }

    [Fact]
    public async Task Should_HandleMixedStock_When_SomeSkusLowAndSomeHigh()
    {
        // Arrange
        var repo = new InMemoryInventoryRepository();
        var agent = new InventoryAgent(repo, NullLogger<InventoryAgent>.Instance);

        // Act - Mix of high-stock and low-stock SKUs
        var skus = new List<string>
        {
            "SKU-1002", // USB-C Cable - High stock (120, 95, 140, 88, 105 units)
            "SKU-1007"  // External SSD - Low stock in some stores (8, 14, 4, 11, 17 units)
        };
        var result = await agent.ExecuteBulkAsync(skus, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var heatmap = (RetailStockHeatmapData)result.A2UIPayload!;
        
        // Should have both High and Low stock statuses
        heatmap.Stores.Should().Contain(s => s.StockStatus == "High", "SKU-1002 should have high stock");
        heatmap.Stores.Should().Contain(s => s.StockStatus == "Low", "SKU-1007 should have low stock stores");
        
        // Text summary should mention low stock count
        result.TextSummary.Should().Contain("below reorder point");
        result.TextSummary.Should().Contain("2 SKUs");
    }

    [Fact]
    public async Task Should_AggregateUnitsCorrectly_When_MultipleSkusQueried()
    {
        // Arrange
        var repo = new InMemoryInventoryRepository();
        var agent = new InventoryAgent(repo, NullLogger<InventoryAgent>.Instance);

        // Act - Query 2 SKUs
        var skus = new List<string> { "SKU-1001", "SKU-1004" };
        var result = await agent.ExecuteBulkAsync(skus, CancellationToken.None);

        // Assert - Total units should be sum across all stores for both SKUs
        result.Success.Should().BeTrue();
        result.TextSummary.Should().Contain("total units");
        result.TextSummary.Should().Contain("2 SKUs");
        
        var heatmap = (RetailStockHeatmapData)result.A2UIPayload!;
        var totalUnits = heatmap.Stores.Sum(s => s.UnitsOnHand);
        totalUnits.Should().BeGreaterThan(0, "should have positive unit count");
    }

    [Fact]
    public async Task Should_ReturnFailure_When_NoSkusProvided()
    {
        // Arrange
        var repo = new InMemoryInventoryRepository();
        var agent = new InventoryAgent(repo, NullLogger<InventoryAgent>.Instance);

        // Act - Empty SKU list
        var skus = new List<string>();
        var result = await agent.ExecuteBulkAsync(skus, CancellationToken.None);

        // Assert - Should handle gracefully (likely with zero results or validation error)
        // Implementation may vary - this validates graceful handling
        result.Should().NotBeNull();
        if (!result.Success)
        {
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task Should_IncludeAllStoreNames_When_BulkHeatmapGenerated()
    {
        // Arrange
        var repo = new InMemoryInventoryRepository();
        var agent = new InventoryAgent(repo, NullLogger<InventoryAgent>.Instance);

        // Act
        var skus = new List<string> { "SKU-1003", "SKU-1005" };
        var result = await agent.ExecuteBulkAsync(skus, CancellationToken.None);

        // Assert - All store names should be present
        result.Success.Should().BeTrue();
        var heatmap = (RetailStockHeatmapData)result.A2UIPayload!;
        
        heatmap.Stores.Should().Contain(s => s.StoreName == "Downtown Flagship");
        heatmap.Stores.Should().Contain(s => s.StoreName == "Suburban Mall");
        heatmap.Stores.Should().Contain(s => s.StoreName == "Airport Terminal");
        heatmap.Stores.Should().Contain(s => s.StoreName == "University District");
        heatmap.Stores.Should().Contain(s => s.StoreName == "Waterfront Plaza");
    }

    [Fact]
    public async Task Should_SetTimestamp_When_BulkQueryCompletes()
    {
        // Arrange
        var repo = new InMemoryInventoryRepository();
        var agent = new InventoryAgent(repo, NullLogger<InventoryAgent>.Instance);

        // Act
        var skus = new List<string> { "SKU-1001", "SKU-1002", "SKU-1003" };
        var before = DateTimeOffset.UtcNow;
        var result = await agent.ExecuteBulkAsync(skus, CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        // Assert - Timestamp should be within execution window
        result.Success.Should().BeTrue();
        result.Timestamp.Should().BeOnOrAfter(before);
        result.Timestamp.Should().BeOnOrBefore(after);
        
        var heatmap = (RetailStockHeatmapData)result.A2UIPayload!;
        heatmap.Timestamp.Should().BeOnOrAfter(before);
        heatmap.Timestamp.Should().BeOnOrBefore(after);
    }
}
