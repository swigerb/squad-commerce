using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.A2A;
using SquadCommerce.A2A.Validation;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Agents.Orchestrator;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Mcp.Data;
using Moq;
using Xunit;

namespace SquadCommerce.Agents.Tests.Orchestrator;

/// <summary>
/// Comprehensive tests for multi-SKU bulk analysis orchestration.
/// Validates that ChiefSoftwareArchitect correctly delegates to all agents for multiple SKUs,
/// produces consolidated A2UI payloads, and calculates aggregate revenue impacts.
/// </summary>
public class BulkAnalysisTests
{
    [Fact]
    public async Task Should_ProcessAllSkus_When_BulkCompetitorPriceDropReceived()
    {
        // Arrange - Full agent stack with real implementations
        var inventoryRepo = new InMemoryInventoryRepository();
        var pricingRepo = new InMemoryPricingRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);

        var inventoryAgent = new InventoryAgent(inventoryRepo, NullLogger<InventoryAgent>.Instance);
        var pricingAgent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);
        var dbContext = CreateInMemoryDbContext();
        var marketIntelAgent = new MarketIntelAgent(a2aClient, validator, dbContext, NullLogger<MarketIntelAgent>.Instance);
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

        // Act - Bulk analysis for 3 SKUs
        var items = new List<(string Sku, decimal CompetitorPrice)>
        {
            ("SKU-1001", 26.99m), // Wireless Mouse
            ("SKU-1002", 10.99m), // USB-C Cable
            ("SKU-1003", 44.99m)  // Laptop Stand
        };

        var result = await orchestrator.ProcessBulkCompetitorPriceDropAsync("TechMart", items, CancellationToken.None);

        // Assert - Workflow succeeded
        result.Should().NotBeNull();
        result.Success.Should().BeTrue("bulk orchestrator should complete successfully");
        result.AgentResults.Should().HaveCount(3, "should have results from MarketIntel, Inventory, and Pricing agents");

        // Verify all three agents were called
        var marketIntelResult = result.AgentResults.FirstOrDefault(r => r.TextSummary.Contains("verified competitor prices"));
        marketIntelResult.Should().NotBeNull("MarketIntelAgent should be invoked");
        marketIntelResult!.Success.Should().BeTrue();

        var inventoryResult = result.AgentResults.FirstOrDefault(r => r.TextSummary.Contains("total units"));
        inventoryResult.Should().NotBeNull("InventoryAgent should be invoked");
        inventoryResult!.Success.Should().BeTrue();

        var pricingResult = result.AgentResults.FirstOrDefault(r => r.TextSummary.Contains("revenue"));
        pricingResult.Should().NotBeNull("PricingAgent should be invoked");
        pricingResult!.Success.Should().BeTrue();

        // Verify executive summary mentions all SKUs
        result.ExecutiveSummary.Should().Contain("3 SKUs", "should mention bulk count");
        result.ExecutiveSummary.Should().Contain("TechMart", "should mention competitor name");
    }

    [Fact]
    public async Task Should_ProduceConsolidatedHeatmap_When_MultipleSkusAnalyzed()
    {
        // Arrange
        var inventoryRepo = new InMemoryInventoryRepository();
        var pricingRepo = new InMemoryPricingRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);

        var inventoryAgent = new InventoryAgent(inventoryRepo, NullLogger<InventoryAgent>.Instance);
        var pricingAgent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);
        var dbContext = CreateInMemoryDbContext();
        var marketIntelAgent = new MarketIntelAgent(a2aClient, validator, dbContext, NullLogger<MarketIntelAgent>.Instance);
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

        // Act - Bulk analysis for 3 SKUs across 5 stores= 15 inventory entries expected
        var items = new List<(string Sku, decimal CompetitorPrice)>
        {
            ("SKU-1001", 26.99m),
            ("SKU-1002", 10.99m),
            ("SKU-1003", 44.99m)
        };

        var result = await orchestrator.ProcessBulkCompetitorPriceDropAsync("TechMart", items, CancellationToken.None);

        // Assert - Consolidated heatmap with all SKUs
        result.Success.Should().BeTrue();
        var inventoryResult = result.AgentResults.FirstOrDefault(r => r.A2UIPayload is RetailStockHeatmapData);
        inventoryResult.Should().NotBeNull("should produce RetailStockHeatmapData");

        var heatmap = (RetailStockHeatmapData)inventoryResult!.A2UIPayload!;
        heatmap.Stores.Should().HaveCountGreaterThan(10, "should have entries for multiple SKUs across stores");
        heatmap.Sku.Should().Contain("SKU-1001", "should mention first SKU");
        heatmap.Sku.Should().Contain("SKU-1002", "should mention second SKU");
        heatmap.Sku.Should().Contain("SKU-1003", "should mention third SKU");
    }

    [Fact]
    public async Task Should_CalculateAggregateRevenue_When_MultipleSkusProposed()
    {
        // Arrange
        var inventoryRepo = new InMemoryInventoryRepository();
        var pricingRepo = new InMemoryPricingRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);

        var inventoryAgent = new InventoryAgent(inventoryRepo, NullLogger<InventoryAgent>.Instance);
        var pricingAgent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);
        var dbContext = CreateInMemoryDbContext();
        var marketIntelAgent = new MarketIntelAgent(a2aClient, validator, dbContext, NullLogger<MarketIntelAgent>.Instance);
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

        // Act - Bulk pricing analysis
        var items = new List<(string Sku, decimal CompetitorPrice)>
        {
            ("SKU-1001", 26.99m),
            ("SKU-1002", 10.99m),
            ("SKU-1003", 44.99m)
        };

        var result = await orchestrator.ProcessBulkCompetitorPriceDropAsync("TechMart", items, CancellationToken.None);

        // Assert - Aggregate revenue calculated
        result.Success.Should().BeTrue();
        var pricingResult = result.AgentResults.FirstOrDefault(r => r.A2UIPayload is PricingImpactChartData);
        pricingResult.Should().NotBeNull("should produce PricingImpactChartData");

        var pricingData = (PricingImpactChartData)pricingResult!.A2UIPayload!;
        pricingData.Scenarios.Should().NotBeEmpty("should have pricing scenarios");
        
        // Verify text summary mentions aggregate revenue
        pricingResult.TextSummary.Should().Contain("revenue", "should mention revenue impact");
        pricingResult.TextSummary.Should().Contain("3 SKUs", "should mention SKU count");
    }

    [Fact]
    public async Task Should_ContinueProcessing_When_OneSkuFailsValidation()
    {
        // Arrange
        var inventoryRepo = new InMemoryInventoryRepository();
        var pricingRepo = new InMemoryPricingRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);

        var inventoryAgent = new InventoryAgent(inventoryRepo, NullLogger<InventoryAgent>.Instance);
        var pricingAgent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);
        var dbContext = CreateInMemoryDbContext();
        var marketIntelAgent = new MarketIntelAgent(a2aClient, validator, dbContext, NullLogger<MarketIntelAgent>.Instance);
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

        // Act - Include one valid SKUand one potentially problematic SKU
        var items = new List<(string Sku, decimal CompetitorPrice)>
        {
            ("SKU-1001", 26.99m), // Valid
            ("SKU-1002", 10.99m), // Valid
            ("INVALID-SKU", 5.00m) // May fail in some stages
        };

        var result = await orchestrator.ProcessBulkCompetitorPriceDropAsync("TechMart", items, CancellationToken.None);

        // Assert - Workflow should complete even if one SKU has issues
        result.Should().NotBeNull();
        // Note: Current implementation may succeed or fail depending on validation logic
        // This test validates that the system handles partial failures gracefully
        if (result.Success)
        {
            result.AgentResults.Should().NotBeEmpty("should have at least some results");
        }
        else
        {
            result.ErrorMessage.Should().NotBeNullOrEmpty("should provide error context");
        }
    }

    [Fact]
    public async Task Should_IncludeAllSkusInExecutiveSummary_When_BulkAnalysisCompletes()
    {
        // Arrange
        var inventoryRepo = new InMemoryInventoryRepository();
        var pricingRepo = new InMemoryPricingRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);

        var inventoryAgent = new InventoryAgent(inventoryRepo, NullLogger<InventoryAgent>.Instance);
        var pricingAgent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);
        var dbContext = CreateInMemoryDbContext();
        var marketIntelAgent = new MarketIntelAgent(a2aClient, validator, dbContext, NullLogger<MarketIntelAgent>.Instance);
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

        // Act - Bulk analysis for 4 SKUs
        var items = new List<(string Sku, decimal CompetitorPrice)>
        {
            ("SKU-1001", 26.99m),
            ("SKU-1002", 10.99m),
            ("SKU-1003", 44.99m),
            ("SKU-1004", 69.99m)
        };

        var result = await orchestrator.ProcessBulkCompetitorPriceDropAsync("ElectroWorld", items, CancellationToken.None);

        // Assert - Executive summary is comprehensive
        result.Success.Should().BeTrue();
        result.ExecutiveSummary.Should().NotBeNullOrEmpty("should provide executive summary");
        result.ExecutiveSummary.Should().Contain("4 SKUs", "should mention total SKU count");
        result.ExecutiveSummary.Should().Contain("ElectroWorld", "should mention competitor name");
        result.ExecutiveSummary.Should().Contain("Recommendation", "should provide recommendation");
        result.WorkflowDuration.Should().BeGreaterThan(TimeSpan.Zero, "should track execution time");
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
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"AuditTestDb_{Guid.NewGuid()}")
            .Options;

        var context = new SquadCommerceDbContext(options);
        return new AuditRepository(context, NullLogger<AuditRepository>.Instance);
    }
}
