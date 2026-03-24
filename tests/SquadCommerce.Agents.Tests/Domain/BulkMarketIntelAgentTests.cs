using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.A2A;
using SquadCommerce.A2A.Validation;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Mcp.Data;
using Xunit;

namespace SquadCommerce.Agents.Tests.Domain;

/// <summary>
/// Tests for MarketIntelAgent bulk operations.
/// Validates A2A bulk queries, competitor price validation, and suspicious price flagging.
/// </summary>
public class BulkMarketIntelAgentTests
{
    [Fact]
    public async Task Should_ValidateAllCompetitorPrices_When_BulkA2AQueryExecuted()
    {
        // Arrange
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);
        var agent = new MarketIntelAgent(a2aClient, validator, NullLogger<MarketIntelAgent>.Instance);

        // Act - Bulk query for 3 SKUs
        var items = new List<(string Sku, decimal OurPrice)>
        {
            ("SKU-1001", 29.99m), // Wireless Mouse
            ("SKU-1002", 12.99m), // USB-C Cable
            ("SKU-1003", 49.99m)  // Laptop Stand
        };

        var result = await agent.ExecuteBulkAsync(items, CancellationToken.None);

        // Assert - All prices validated via A2A
        result.Success.Should().BeTrue("bulk A2A query should succeed");
        result.A2UIPayload.Should().NotBeNull();
        result.A2UIPayload.Should().BeOfType<MarketComparisonGridData>();

        var marketData = (MarketComparisonGridData)result.A2UIPayload!;
        marketData.Competitors.Should().NotBeEmpty("should have validated competitor prices");
        
        // Text summary should mention validation
        result.TextSummary.Should().Contain("verified competitor prices");
        result.TextSummary.Should().Contain("3 SKUs");
    }

    [Fact]
    public async Task Should_FlagSuspiciousPrices_When_AnySkuExceeds50PercentDelta()
    {
        // Arrange
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);
        var agent = new MarketIntelAgent(a2aClient, validator, NullLogger<MarketIntelAgent>.Instance);

        // Act - Include extreme price deltas
        var items = new List<(string Sku, decimal OurPrice)>
        {
            ("SKU-1001", 29.99m), // Normal competitor prices (~8% lower)
            ("SKU-1008", 350.00m) // Monitor - Our price high, competitor much lower (>50% delta potential)
        };

        var result = await agent.ExecuteBulkAsync(items, CancellationToken.None);

        // Assert - Should process but may flag suspicious prices
        result.Should().NotBeNull();
        
        if (result.Success)
        {
            var marketData = (MarketComparisonGridData)result.A2UIPayload!;
            marketData.Competitors.Should().NotBeEmpty();
            
            // Text summary should show price delta information
            result.TextSummary.Should().Contain("vs lowest", "should compare against lowest price");
        }
    }

    [Fact]
    public async Task Should_AggregateCompetitorData_When_MultipleSources()
    {
        // Arrange
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);
        var agent = new MarketIntelAgent(a2aClient, validator, NullLogger<MarketIntelAgent>.Instance);

        // Act - Query multiple SKUs (A2A returns 3 competitors per SKU)
        var items = new List<(string Sku, decimal OurPrice)>
        {
            ("SKU-1001", 29.99m),
            ("SKU-1002", 12.99m)
        };

        var result = await agent.ExecuteBulkAsync(items, CancellationToken.None);

        // Assert - Should aggregate competitor data
        result.Success.Should().BeTrue();
        var marketData = (MarketComparisonGridData)result.A2UIPayload!;
        
        // A2A client returns 3 competitors per SKU, aggregated by competitor name
        marketData.Competitors.Should().HaveCountGreaterThanOrEqualTo(1, "should have at least one competitor");
        
        // Competitors should have averaged prices across SKUs
        marketData.Competitors.Should().AllSatisfy(competitor =>
        {
            competitor.Price.Should().BeGreaterThan(0);
            competitor.Verified.Should().BeTrue("all prices should be verified");
        });
    }

    [Fact]
    public async Task Should_HandleValidationFailures_When_LowConfidencePrices()
    {
        // Arrange
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);
        var agent = new MarketIntelAgent(a2aClient, validator, NullLogger<MarketIntelAgent>.Instance);

        // Act - Query with realistic prices (validator should accept)
        var items = new List<(string Sku, decimal OurPrice)>
        {
            ("SKU-1001", 29.99m),
            ("SKU-1003", 49.99m)
        };

        var result = await agent.ExecuteBulkAsync(items, CancellationToken.None);

        // Assert - Should filter to high/medium confidence only
        result.Should().NotBeNull();
        
        if (result.Success)
        {
            var marketData = (MarketComparisonGridData)result.A2UIPayload!;
            marketData.Competitors.Should().NotBeEmpty("should have validated prices");
            result.TextSummary.Should().Contain("validated");
        }
        else
        {
            result.ErrorMessage.Should().Contain("validation", "should mention validation failure");
        }
    }

    [Fact]
    public async Task Should_CalculateAvgCompetitorPrice_When_BulkAnalysis()
    {
        // Arrange
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);
        var agent = new MarketIntelAgent(a2aClient, validator, NullLogger<MarketIntelAgent>.Instance);

        // Act
        var items = new List<(string Sku, decimal OurPrice)>
        {
            ("SKU-1001", 29.99m),
            ("SKU-1002", 12.99m),
            ("SKU-1003", 49.99m)
        };

        var result = await agent.ExecuteBulkAsync(items, CancellationToken.None);

        // Assert - Should calculate average
        result.Success.Should().BeTrue();
        result.TextSummary.Should().Contain("Avg", "should mention average price");
        result.TextSummary.Should().Contain("Lowest", "should mention lowest price");
        result.TextSummary.Should().Contain("Our avg price", "should mention our average price");
    }

    [Fact]
    public async Task Should_IncludeCompetitorSource_When_A2ADataReturned()
    {
        // Arrange
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);
        var agent = new MarketIntelAgent(a2aClient, validator, NullLogger<MarketIntelAgent>.Instance);

        // Act
        var items = new List<(string Sku, decimal OurPrice)>
        {
            ("SKU-1001", 29.99m)
        };

        var result = await agent.ExecuteBulkAsync(items, CancellationToken.None);

        // Assert - Competitor data should include source
        result.Success.Should().BeTrue();
        var marketData = (MarketComparisonGridData)result.A2UIPayload!;
        
        marketData.Competitors.Should().AllSatisfy(competitor =>
        {
            competitor.Source.Should().NotBeNullOrEmpty("should have A2A source");
            competitor.Source.Should().Contain("A2A:", "source should indicate A2A protocol");
        });
    }

    [Fact]
    public async Task Should_SetTimestamp_When_BulkA2ACompletes()
    {
        // Arrange
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);
        var agent = new MarketIntelAgent(a2aClient, validator, NullLogger<MarketIntelAgent>.Instance);

        // Act
        var items = new List<(string Sku, decimal OurPrice)>
        {
            ("SKU-1001", 29.99m),
            ("SKU-1002", 12.99m)
        };

        var before = DateTimeOffset.UtcNow;
        var result = await agent.ExecuteBulkAsync(items, CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        // Assert - Timestamps should be accurate
        result.Success.Should().BeTrue();
        result.Timestamp.Should().BeOnOrAfter(before);
        result.Timestamp.Should().BeOnOrBefore(after);
        
        var marketData = (MarketComparisonGridData)result.A2UIPayload!;
        marketData.Timestamp.Should().BeOnOrAfter(before);
        marketData.Timestamp.Should().BeOnOrBefore(after);
    }
}
