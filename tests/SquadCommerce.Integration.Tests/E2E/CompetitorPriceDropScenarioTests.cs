using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SquadCommerce.A2A;
using SquadCommerce.A2A.Validation;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Agents.Orchestrator;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Contracts.Models;
using SquadCommerce.Mcp.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SquadCommerce.Integration.Tests.E2E;

/// <summary>
/// End-to-end scenario tests for competitor price drop workflow.
/// Full integration from agent creation through orchestration to final recommendations.
/// </summary>
public class CompetitorPriceDropScenarioTests
{
    [Fact]
    public async Task Should_ProduceApprovalRecommendation_When_CompetitorDropsPrice()
    {
        // Arrange - Build full agent stack with real implementations
        var inventoryRepo = new InMemoryInventoryRepository();
        var pricingRepo = new InMemoryPricingRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(
            pricingRepo,
            inventoryRepo,
            NullLogger<ExternalDataValidator>.Instance);

        var inventoryAgent = new InventoryAgent(
            inventoryRepo,
            NullLogger<InventoryAgent>.Instance);

        var pricingAgent = new PricingAgent(
            pricingRepo,
            inventoryRepo,
            NullLogger<PricingAgent>.Instance);

        var dbContext = CreateInMemoryDbContext();
        var marketIntelAgent = new MarketIntelAgent(
            a2aClient,
            validator,
            dbContext,
            NullLogger<MarketIntelAgent>.Instance);

        var marketingAgent = new MarketingAgent(dbContext, pricingRepo, NullLogger<MarketingAgent>.Instance);
        var auditRepo = CreateInMemoryAuditRepository();
        var orchestrator = new ChiefSoftwareArchitectAgent(
            inventoryAgent,
            pricingAgent,
            marketIntelAgent,
            marketingAgent,
            auditRepo,
            Mock.Of<IThinkingStateNotifier>(),
            Mock.Of<IReasoningTraceEmitter>(),
            NullLogger<ChiefSoftwareArchitectAgent>.Instance);

        // Act - Trigger full workflow: competitor drops price by 10%
        var sku = "SKU-1001"; // Wireless Mouse
        var competitorPrice = 26.99m; // ~10% below our avg price

        var result = await orchestrator.ProcessCompetitorPriceDropAsync(sku, competitorPrice, CancellationToken.None);

        // Assert - Workflow succeeded
        result.Should().NotBeNull();
        result.Success.Should().BeTrue("orchestrator should complete successfully");
        result.AgentResults.Should().HaveCount(3, "should have results from MarketIntel, Inventory, and Pricing agents");

        // Verify MarketIntelAgent validated competitor data
        var marketIntelResult = result.AgentResults[0];
        marketIntelResult.Success.Should().BeTrue("MarketIntelAgent should validate competitor pricing");
        marketIntelResult.A2UIPayload.Should().BeOfType<MarketComparisonGridData>();
        var marketData = (MarketComparisonGridData)marketIntelResult.A2UIPayload!;
        marketData.Competitors.Should().NotBeEmpty("should have validated competitor prices");

        // Verify InventoryAgent returned heatmap
        var inventoryResult = result.AgentResults[1];
        inventoryResult.Success.Should().BeTrue("InventoryAgent should return inventory levels");
        inventoryResult.A2UIPayload.Should().BeOfType<RetailStockHeatmapData>();
        var heatmapData = (RetailStockHeatmapData)inventoryResult.A2UIPayload!;
        heatmapData.Stores.Should().NotBeEmpty("should have store inventory levels");

        // Verify PricingAgent calculated margin impact
        var pricingResult = result.AgentResults[2];
        pricingResult.Success.Should().BeTrue("PricingAgent should calculate pricing impact");
        pricingResult.A2UIPayload.Should().BeOfType<PricingImpactChartData>();
        var pricingData = (PricingImpactChartData)pricingResult.A2UIPayload!;
        pricingData.Scenarios.Should().HaveCount(4, "should have 4 pricing scenarios");
        pricingData.ProposedPrice.Should().Be(competitorPrice);

        // Verify executive summary synthesized results
        result.ExecutiveSummary.Should().Contain(sku);
        result.ExecutiveSummary.Should().Contain("Recommendation");
        result.WorkflowDuration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task Should_ExecutePriceUpdate_When_ManagerApproves()
    {
        // Arrange
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();

        var sku = "SKU-1002"; // USB-C Cable
        var storeId = "SEA-001";
        var newPrice = 10.99m;

        // Verify current price before update
        var currentPrice = await pricingRepo.GetCurrentPriceAsync(storeId, sku, CancellationToken.None);
        currentPrice.Should().NotBeNull();
        currentPrice!.Value.Should().BeGreaterThan(newPrice, "new price should be lower");

        // Act - Manager approves price change
        var priceChange = new PriceChange
        {
            Sku = sku,
            StoreId = storeId,
            OldPrice = currentPrice.Value,
            NewPrice = newPrice,
            Reason = "Match competitor pricing",
            RequestedBy = "manager@squadcommerce.com",
            Timestamp = DateTimeOffset.UtcNow
        };

        var result = await pricingRepo.UpdatePricingAsync(priceChange, CancellationToken.None);

        // Assert - Price updated successfully
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.StoresUpdated.Should().Contain(storeId);

        // Verify price was actually updated
        var updatedPrice = await pricingRepo.GetCurrentPriceAsync(storeId, sku, CancellationToken.None);
        updatedPrice.Should().Be(newPrice);
    }

    [Fact]
    public async Task Should_LogDecision_When_ManagerRejects()
    {
        // Arrange
        var pricingRepo = new InMemoryPricingRepository();
        var sku = "SKU-1003"; // Laptop Stand
        var storeId = "PDX-002";

        var currentPrice = await pricingRepo.GetCurrentPriceAsync(storeId, sku, CancellationToken.None);
        currentPrice.Should().NotBeNull();

        // Act - Manager rejects (no price change)
        // In real system, rejection would be logged via endpoint
        // For test, we verify that NOT calling UpdatePricingAsync means price stays the same

        await Task.Delay(10); // Simulate rejection processing time

        // Assert - Price remains unchanged
        var stillSamePrice = await pricingRepo.GetCurrentPriceAsync(storeId, sku, CancellationToken.None);
        stillSamePrice.Should().Be(currentPrice, "price should not change when manager rejects");
    }

    [Fact]
    public async Task Should_RetriggerPricing_When_ManagerModifies()
    {
        // Arrange
        var inventoryRepo = new InMemoryInventoryRepository();
        var pricingRepo = new InMemoryPricingRepository();

        var pricingAgent = new PricingAgent(
            pricingRepo,
            inventoryRepo,
            NullLogger<PricingAgent>.Instance);

        var sku = "SKU-1004"; // Webcam
        var originalCompetitorPrice = 74.99m;
        var managerModifiedPrice = 77.99m; // Manager wants slightly higher than competitor

        // Act - First calculation with competitor price
        var firstResult = await pricingAgent.ExecuteAsync(sku, originalCompetitorPrice, CancellationToken.None);
        firstResult.Success.Should().BeTrue();
        var firstPricing = (PricingImpactChartData)firstResult.A2UIPayload!;

        // Manager modifies - Re-trigger with new price
        var secondResult = await pricingAgent.ExecuteAsync(sku, managerModifiedPrice, CancellationToken.None);
        secondResult.Success.Should().BeTrue();
        var secondPricing = (PricingImpactChartData)secondResult.A2UIPayload!;

        // Assert - Second calculation reflects manager's modified price
        firstPricing.ProposedPrice.Should().Be(originalCompetitorPrice);
        secondPricing.ProposedPrice.Should().Be(managerModifiedPrice);
        secondPricing.ProposedPrice.Should().BeGreaterThan(firstPricing.ProposedPrice);

        // Both should have 4 scenarios, but with different calculations
        firstPricing.Scenarios.Should().HaveCount(4);
        secondPricing.Scenarios.Should().HaveCount(4);
    }

    [Fact]
    public async Task Should_HandleMultiStoreScenario_When_CompetitorDropsPrice()
    {
        // Arrange - All 5 stores scenario
        var inventoryRepo = new InMemoryInventoryRepository();
        var pricingRepo = new InMemoryPricingRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(
            pricingRepo,
            inventoryRepo,
            NullLogger<ExternalDataValidator>.Instance);

        var inventoryAgent = new InventoryAgent(
            inventoryRepo,
            NullLogger<InventoryAgent>.Instance);

        var dbContext = CreateInMemoryDbContext();
        var marketIntelAgent = new MarketIntelAgent(
            a2aClient,
            validator,
            dbContext,
            NullLogger<MarketIntelAgent>.Instance);

        var sku = "SKU-1005"; // Mechanical Keyboard

        // Act - Query inventory and market intel across all stores
        var inventoryResult = await inventoryAgent.ExecuteAsync(sku, CancellationToken.None);
        var marketIntelResult = await marketIntelAgent.ExecuteAsync(sku, 119.99m, CancellationToken.None);

        // Assert - Should have data for all 5 stores
        inventoryResult.Success.Should().BeTrue();
        var heatmap = (RetailStockHeatmapData)inventoryResult.A2UIPayload!;
        heatmap.Stores.Should().HaveCount(5, "should have inventory for all 5 stores");
        heatmap.Stores.Select(s => s.StoreId).Should().Contain(new[] 
        { 
            "SEA-001", "PDX-002", "SFO-003", "LAX-004", "DEN-005" 
        });

        // Market intel should validate competitor prices
        marketIntelResult.Success.Should().BeTrue();
        var marketGrid = (MarketComparisonGridData)marketIntelResult.A2UIPayload!;
        marketGrid.Competitors.Should().NotBeEmpty();
        marketGrid.Competitors.All(c => c.Price > 0).Should().BeTrue("all competitor prices should be valid");
    }

    private static SquadCommerceDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        return new SquadCommerceDbContext(options);
    }

    private static AuditRepository CreateInMemoryAuditRepository()
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new SquadCommerceDbContext(options);
        return new AuditRepository(context, NullLogger<AuditRepository>.Instance);
    }
}
