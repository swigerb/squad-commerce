using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using SquadCommerce.Agents.Domain;
using SquadCommerce.A2A;
using SquadCommerce.A2A.Validation;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Mcp.Data;

namespace SquadCommerce.Agents.Tests.Domain;

/// <summary>
/// Coverage gap tests for MarketIntelAgent edge cases.
/// </summary>
public class MarketIntelAgentCoverageTests
{
    [Fact]
    public async Task Should_ValidateAllCompetitorPrices_When_Querying()
    {
        // Arrange
        var pricingRepo = new PricingRepository();
        var inventoryRepo = new InventoryRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);
        
        var agent = new MarketIntelAgent(a2aClient, validator, NullLogger<MarketIntelAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1001", 29.99m, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var grid = (MarketComparisonGridData)result.A2UIPayload!;
        
        // All returned prices should be validated (High or Medium confidence)
        grid.Competitors.Should().NotBeEmpty();
        grid.Competitors.All(c => c.Verified).Should().BeTrue("all should be validated");
    }

    [Fact]
    public async Task Should_IncludeOurPriceInComparison_When_Generating()
    {
        // Arrange
        var pricingRepo = new PricingRepository();
        var inventoryRepo = new InventoryRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);
        
        var agent = new MarketIntelAgent(a2aClient, validator, NullLogger<MarketIntelAgent>.Instance);

        var ourPrice = 49.99m;

        // Act
        var result = await agent.ExecuteAsync("SKU-1003", ourPrice, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var grid = (MarketComparisonGridData)result.A2UIPayload!;
        
        grid.OurPrice.Should().Be(ourPrice);
        grid.ProductName.Should().Be("Laptop Stand");
        grid.Sku.Should().Be("SKU-1003");
    }

    [Fact]
    public async Task Should_CalculatePriceDelta_When_ComparingToCompetitors()
    {
        // Arrange
        var pricingRepo = new PricingRepository();
        var inventoryRepo = new InventoryRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);
        
        var agent = new MarketIntelAgent(a2aClient, validator, NullLogger<MarketIntelAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1005", 119.99m, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.TextSummary.Should().Contain("vs lowest");
        result.TextSummary.Should().Contain("%");
        result.TextSummary.Should().Contain("Lowest:");
        result.TextSummary.Should().Contain("Avg:");
    }

    [Theory]
    [InlineData("SKU-1001", "Wireless Mouse")]
    [InlineData("SKU-1002", "USB-C Cable 6ft")]
    [InlineData("SKU-1004", "Webcam 1080p")]
    [InlineData("SKU-1008", "Monitor 27-inch")]
    public async Task Should_MapSkuToProductName_When_GeneratingGrid(string sku, string expectedName)
    {
        // Arrange
        var pricingRepo = new PricingRepository();
        var inventoryRepo = new InventoryRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);
        
        var agent = new MarketIntelAgent(a2aClient, validator, NullLogger<MarketIntelAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync(sku, 99.99m, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var grid = (MarketComparisonGridData)result.A2UIPayload!;
        grid.ProductName.Should().Be(expectedName);
    }

    [Fact]
    public async Task Should_IdentifyLowestCompetitorPrice_When_Analyzing()
    {
        // Arrange
        var pricingRepo = new PricingRepository();
        var inventoryRepo = new InventoryRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);
        
        var agent = new MarketIntelAgent(a2aClient, validator, NullLogger<MarketIntelAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1006", 199.99m, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var grid = (MarketComparisonGridData)result.A2UIPayload!;
        
        var lowestPrice = grid.Competitors.Min(c => c.Price);
        result.TextSummary.Should().Contain($"${lowestPrice:F2}");
        
        // Should identify which competitor has the lowest price
        var lowestCompetitor = grid.Competitors.First(c => c.Price == lowestPrice);
        result.TextSummary.Should().Contain(lowestCompetitor.CompetitorName);
    }

    [Fact]
    public async Task Should_IncludeValidationNote_When_PricesVerified()
    {
        // Arrange
        var pricingRepo = new PricingRepository();
        var inventoryRepo = new InventoryRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);
        
        var agent = new MarketIntelAgent(a2aClient, validator, NullLogger<MarketIntelAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1007", 89.99m, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.TextSummary.Should().Contain("validated");
        result.TextSummary.Should().Contain("ExternalDataValidator");
        result.TextSummary.Should().Contain("A2A protocol");
    }
}
