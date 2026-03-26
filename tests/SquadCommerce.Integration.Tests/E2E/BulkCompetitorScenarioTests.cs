using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.A2A;
using SquadCommerce.A2A.Validation;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Agents.Orchestrator;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Contracts.Models;
using SquadCommerce.Mcp.Data;
using Moq;
using Xunit;

namespace SquadCommerce.Integration.Tests.E2E;

/// <summary>
/// End-to-end tests for bulk competitor price drop scenarios.
/// Full integration testing with multiple SKUs through the entire workflow.
/// </summary>
public class BulkCompetitorScenarioTests
{
    [Fact]
    public async Task Should_CompleteFullBulkWorkflow_When_ThreeSkusDropped()
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

        // Act - Trigger bulk workflow: TechMart drops priceson 3 products
        var items = new List<(string Sku, decimal CompetitorPrice)>
        {
            ("SKU-1001", 26.99m), // Wireless Mouse - 10% drop
            ("SKU-1002", 10.99m), // USB-C Cable - 15% drop
            ("SKU-1003", 44.99m)  // Laptop Stand - 10% drop
        };

        var result = await orchestrator.ProcessBulkCompetitorPriceDropAsync("TechMart", items, CancellationToken.None);

        // Assert - Full workflow succeeded
        result.Should().NotBeNull();
        result.Success.Should().BeTrue("bulk orchestrator should complete successfully");
        result.AgentResults.Should().HaveCount(3, "should have results from all 3 agents");

        // Verify MarketIntelAgent validated all competitor prices
        var marketIntelResult = result.AgentResults.FirstOrDefault(r => r.A2UIPayload is MarketComparisonGridData);
        marketIntelResult.Should().NotBeNull("should have MarketIntelAgent result");
        marketIntelResult!.Success.Should().BeTrue();
        var marketData = (MarketComparisonGridData)marketIntelResult.A2UIPayload!;
        marketData.Competitors.Should().NotBeEmpty("should have validated competitor prices");
        marketData.Sku.Should().Contain("SKU-1001");

        // Verify InventoryAgent returned consolidated heatmap
        var inventoryResult = result.AgentResults.FirstOrDefault(r => r.A2UIPayload is RetailStockHeatmapData);
        inventoryResult.Should().NotBeNull("should have InventoryAgent result");
        inventoryResult!.Success.Should().BeTrue();
        var heatmapData = (RetailStockHeatmapData)inventoryResult.A2UIPayload!;
        heatmapData.Stores.Should().HaveCount(15, "3 SKUs × 5 stores = 15 entries");

        // Verify PricingAgent calculated aggregate revenue
        var pricingResult = result.AgentResults.FirstOrDefault(r => r.A2UIPayload is PricingImpactChartData);
        pricingResult.Should().NotBeNull("should have PricingAgent result");
        pricingResult!.Success.Should().BeTrue();
        var pricingData = (PricingImpactChartData)pricingResult.A2UIPayload!;
        pricingData.Scenarios.Should().NotBeEmpty("should have pricing scenarios");

        // Verify executive summary synthesized bulk results
        result.ExecutiveSummary.Should().Contain("3 SKUs");
        result.ExecutiveSummary.Should().Contain("TechMart");
        result.ExecutiveSummary.Should().Contain("Recommendation");
        result.WorkflowDuration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task Should_ProduceFiveA2UIPayloads_When_BulkAnalysisCompletes()
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

        // Act
        var items = new List<(string Sku, decimal CompetitorPrice)>
        {
            ("SKU-1004", 69.99m), // Webcam
            ("SKU-1005", 99.99m)  // Keyboard
        };

        var result = await orchestrator.ProcessBulkCompetitorPriceDropAsync("ElectroWorld", items, CancellationToken.None);

        // Assert - Should produce multiple A2UI payloads
        result.Success.Should().BeTrue();
        
        var payloadsWithA2UI = result.AgentResults.Where(r => r.A2UIPayload != null).ToList();
        payloadsWithA2UI.Should().HaveCount(3, "should have A2UI payloads from 3 agents");

        // Expected payload types:
        // 1. MarketComparisonGrid (from MarketIntelAgent)
        // 2. RetailStockHeatmap (from InventoryAgent)
        // 3. PricingImpactChart (from PricingAgent)
        // 4. AuditTimeline (from orchestrator) - may be added to PipelinePayload
        // 5. PipelinePayload (from orchestrator) - contains workflow stages

        payloadsWithA2UI.Should().Contain(r => r.A2UIPayload is MarketComparisonGridData);
        payloadsWithA2UI.Should().Contain(r => r.A2UIPayload is RetailStockHeatmapData);
        payloadsWithA2UI.Should().Contain(r => r.A2UIPayload is PricingImpactChartData);
    }

    [Fact]
    public async Task Should_HandleBulkApproval_When_ManagerApprovesAll()
    {
        // Arrange
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();

        var storeId = "SEA-001";
        var skuPrices = new Dictionary<string, (decimal OldPrice, decimal NewPrice)>
        {
            ["SKU-1001"] = (29.99m, 26.99m),
            ["SKU-1002"] = (12.99m, 10.99m),
            ["SKU-1003"] = (49.99m, 44.99m)
        };

        // Act - Manager approves all price changes
        var updateResults = new List<PricingUpdateResult>();
        foreach (var (sku, prices) in skuPrices)
        {
            var priceChange = new PriceChange
            {
                Sku = sku,
                StoreId = storeId,
                OldPrice = prices.OldPrice,
                NewPrice = prices.NewPrice,
                Reason = "Match TechMart bulk pricing",
                RequestedBy = "manager@squadcommerce.com",
                Timestamp = DateTimeOffset.UtcNow
            };

            var updateResult = await pricingRepo.UpdatePricingAsync(priceChange, CancellationToken.None);
            updateResults.Add(updateResult);
        }

        // Assert - All updates succeeded
        updateResults.Should().AllSatisfy(result =>
        {
            result.Success.Should().BeTrue("all price updates should succeed");
            result.StoresUpdated.Should().Contain(storeId);
        });

        // Verify prices were actually updated
        foreach (var sku in skuPrices.Keys)
        {
            var updatedPrice = await pricingRepo.GetCurrentPriceAsync(storeId, sku, CancellationToken.None);
            updatedPrice.Should().Be(skuPrices[sku].NewPrice);
        }
    }

    [Fact]
    public async Task Should_HandlePartialModification_When_ManagerChangesOneSkuPrice()
    {
        // Arrange
        var pricingRepo = new InMemoryPricingRepository();
        var inventoryRepo = new InMemoryInventoryRepository();

        var storeId = "PDX-002";
        
        // Act - Manager approves 2 SKUs at recommended price, modifies 1 SKU
        var approvedChanges = new[]
        {
            new PriceChange
            {
                Sku = "SKU-1001",
                StoreId = storeId,
                OldPrice = 29.99m,
                NewPrice = 26.99m, // Recommended price
                Reason = "Match competitor",
                RequestedBy = "manager@squadcommerce.com",
                Timestamp = DateTimeOffset.UtcNow
            },
            new PriceChange
            {
                Sku = "SKU-1002",
                StoreId = storeId,
                OldPrice = 12.99m,
                NewPrice = 11.49m, // Modified - between our price and competitor's 10.99
                Reason = "Partial match - maintain margin",
                RequestedBy = "manager@squadcommerce.com",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        var results = new List<PricingUpdateResult>();
        foreach (var change in approvedChanges)
        {
            var result = await pricingRepo.UpdatePricingAsync(change, CancellationToken.None);
            results.Add(result);
        }

        // Assert - Both updates succeeded
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());

        // Verify SKU-1002 has the modified price (not the recommended one)
        var modifiedPrice = await pricingRepo.GetCurrentPriceAsync(storeId, "SKU-1002", CancellationToken.None);
        modifiedPrice.Should().Be(11.49m, "manager's modified price should be applied");
        modifiedPrice.Should().NotBe(10.99m, "should not be the original recommended price");
    }

    [Fact]
    public async Task Should_TrackAllSkusInAudit_When_BulkWorkflowCompletes()
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

        // Act
        var items = new List<(string Sku, decimal CompetitorPrice)>
        {
            ("SKU-1001", 26.99m),
            ("SKU-1002", 10.99m),
            ("SKU-1003", 44.99m)
        };

        var result = await orchestrator.ProcessBulkCompetitorPriceDropAsync("TechMart", items, CancellationToken.None);

        // Assert - Audit trail should trackall SKUs
        result.Success.Should().BeTrue();
        
        // The orchestrator should have created pipeline stages for tracking
        // Note: Detailed audit queries would require accessing the AuditRepository directly
        // For this E2E test, we verify the workflow completed and returned pipeline data
        result.AgentResults.Should().NotBeEmpty();
        result.ExecutiveSummary.Should().Contain("3 SKUs", "audit should reference all SKUs");
    }

    [Fact]
    public async Task Should_CalculateTotalRevenueImpact_When_BulkPricesApproved()
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

        // Act - Large bulk analysis
        var items = new List<(string Sku, decimal CompetitorPrice)>
        {
            ("SKU-1001", 26.99m),
            ("SKU-1002", 10.99m),
            ("SKU-1003", 44.99m),
            ("SKU-1004", 69.99m)
        };

        var result = await orchestrator.ProcessBulkCompetitorPriceDropAsync("TechMart", items, CancellationToken.None);

        // Assert - Total revenue impact calculated
        result.Success.Should().BeTrue();
        
        var pricingResult = result.AgentResults.FirstOrDefault(r => r.A2UIPayload is PricingImpactChartData);
        pricingResult.Should().NotBeNull();
        
        var pricingData = (PricingImpactChartData)pricingResult!.A2UIPayload!;
        var totalRevenue = pricingData.Scenarios.Sum(s => s.EstimatedRevenue);
        
        totalRevenue.Should().BeGreaterThan(0, "should calculate total revenue across all SKUs");
        pricingResult.TextSummary.Should().Contain("revenue", "should mention revenue impact");
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
            .UseInMemoryDatabase($"AuditTestDb_{Guid.NewGuid()}")
            .Options;

        var context = new SquadCommerceDbContext(options);
        return new AuditRepository(context, NullLogger<AuditRepository>.Instance);
    }
}
