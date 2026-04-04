using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SquadCommerce.A2A;
using SquadCommerce.A2A.Validation;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Agents.Orchestrator;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Mcp.Data.Entities;
using Xunit;

namespace SquadCommerce.Agents.Tests.Orchestrator;

/// <summary>
/// Tests for the Supply Chain Shock orchestrator workflow.
/// Validates Logistics → Inventory → Redistribution pipeline.
/// </summary>
public class SupplyChainWorkflowTests
{
    [Fact]
    public async Task Should_CompleteSupplyChainAnalysis_When_ShipmentDelayed()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedShipments: true);

        // Act
        var result = await orchestrator.ProcessSupplyChainShockAsync(
            "SKU-1001", 5, "Port congestion", new[] { "Southeast" }, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AgentResults.Should().HaveCountGreaterThan(0);
        result.ExecutiveSummary.Should().Contain("SKU-1001");
    }

    [Fact]
    public async Task Should_IncludeLogisticsAnalysis_When_WorkflowCompletes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedShipments: true);

        // Act
        var result = await orchestrator.ProcessSupplyChainShockAsync(
            "SKU-1001", 3, "Weather", new[] { "Northwest" }, CancellationToken.None);

        // Assert
        result.AgentResults.Should().Contain(r =>
            r.TextSummary.Contains("delayed") || r.TextSummary.Contains("shipment") || r.TextSummary.Contains("risk"),
            "should include logistics delay analysis");
    }

    [Fact]
    public async Task Should_IncludeInventorySnapshot_When_WorkflowCompletes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedShipments: true);

        // Act
        var result = await orchestrator.ProcessSupplyChainShockAsync(
            "SKU-1001", 3, "Weather", new[] { "Northwest" }, CancellationToken.None);

        // Assert
        result.AgentResults.Should().Contain(r =>
            r.TextSummary.Contains("units") || r.TextSummary.Contains("stock") || r.TextSummary.Contains("inventory"),
            "should include inventory levels from InventoryAgent");
    }

    [Fact]
    public async Task Should_IncludeRedistributionPlan_When_WorkflowCompletes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedShipments: true);

        // Act
        var result = await orchestrator.ProcessSupplyChainShockAsync(
            "SKU-1001", 5, "Port congestion", new[] { "Southeast" }, CancellationToken.None);

        // Assert
        result.AgentResults.Should().Contain(r =>
            r.TextSummary.Contains("redistribution") || r.TextSummary.Contains("transfer") || r.TextSummary.Contains("surplus") || r.TextSummary.Contains("at-risk"),
            "should include redistribution plan from RedistributionAgent");
    }

    [Fact]
    public async Task Should_TrackWorkflowDuration_When_AnalysisCompletes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedShipments: true);

        // Act
        var result = await orchestrator.ProcessSupplyChainShockAsync(
            "SKU-1001", 3, "Test", new[] { "Northeast" }, CancellationToken.None);

        // Assert
        result.WorkflowDuration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task Should_HandleNoShipments_When_SkuHasNoData()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedShipments: false);

        // Act
        var result = await orchestrator.ProcessSupplyChainShockAsync(
            "NONEXISTENT-SKU", 5, "Test", new[] { "Southeast" }, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ExecutiveSummary.Should().NotBeNullOrEmpty();
    }

    private static ChiefSoftwareArchitectAgent CreateOrchestrator(bool seedShipments)
    {
        var inventoryRepo = new InMemoryInventoryRepository();
        var pricingRepo = new InMemoryPricingRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);

        var shipmentDb = CreateDbContext(seedShipments);

        var inventoryAgent = new InventoryAgent(inventoryRepo, NullLogger<InventoryAgent>.Instance);
        var pricingAgent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);
        var marketIntelAgent = new MarketIntelAgent(a2aClient, validator, CreateDbContext(false), NullLogger<MarketIntelAgent>.Instance);
        var marketingAgent = new MarketingAgent(CreateDbContext(false), pricingRepo, NullLogger<MarketingAgent>.Instance);
        var logisticsAgent = new LogisticsAgent(shipmentDb, NullLogger<LogisticsAgent>.Instance);
        var redistributionAgent = new RedistributionAgent(inventoryRepo, NullLogger<RedistributionAgent>.Instance);
        var trafficAgent = new TrafficAnalystAgent(CreateDbContext(false), NullLogger<TrafficAnalystAgent>.Instance);
        var merchandisingAgent = new MerchandisingAgent(CreateDbContext(false), NullLogger<MerchandisingAgent>.Instance);
        var managerAgent = new ManagerAgent(NullLogger<ManagerAgent>.Instance);
        var complianceAgent = new ComplianceAgent(CreateDbContext(false), NullLogger<ComplianceAgent>.Instance);
        var researchAgent = new ResearchAgent(CreateDbContext(false), NullLogger<ResearchAgent>.Instance);
        var procurementAgent = new ProcurementAgent(CreateDbContext(false), NullLogger<ProcurementAgent>.Instance);
        var auditRepo = CreateAuditRepository();

        return new ChiefSoftwareArchitectAgent(
            inventoryAgent, pricingAgent, marketIntelAgent, marketingAgent,
            logisticsAgent, redistributionAgent, trafficAgent, merchandisingAgent,
            managerAgent, complianceAgent, researchAgent, procurementAgent,
            auditRepo,
            Mock.Of<IThinkingStateNotifier>(),
            Mock.Of<IReasoningTraceEmitter>(),
            NullLogger<ChiefSoftwareArchitectAgent>.Instance);
    }

    private static SquadCommerceDbContext CreateDbContext(bool seedShipments)
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"SupplyChainTest_{Guid.NewGuid()}")
            .Options;
        var db = new SquadCommerceDbContext(options);

        if (seedShipments)
        {
            db.Shipments.AddRange(
                new ShipmentEntity
                {
                    ShipmentId = "SHP-001", Sku = "SKU-1001", ProductName = "Wireless Mouse",
                    OriginStoreId = "SEA-001", DestStoreId = "MIA-009", Status = "Delayed",
                    EstimatedArrival = DateTimeOffset.UtcNow.AddDays(5), DelayDays = 5,
                    DelayReason = "Port congestion", CreatedAt = DateTimeOffset.UtcNow
                },
                new ShipmentEntity
                {
                    ShipmentId = "SHP-002", Sku = "SKU-1001", ProductName = "Wireless Mouse",
                    OriginStoreId = "PDX-002", DestStoreId = "TPA-010", Status = "Delayed",
                    EstimatedArrival = DateTimeOffset.UtcNow.AddDays(4), DelayDays = 3,
                    DelayReason = "Weather", CreatedAt = DateTimeOffset.UtcNow
                },
                new ShipmentEntity
                {
                    ShipmentId = "SHP-003", Sku = "SKU-1001", ProductName = "Wireless Mouse",
                    OriginStoreId = "DEN-005", DestStoreId = "SFO-003", Status = "InTransit",
                    EstimatedArrival = DateTimeOffset.UtcNow.AddDays(1), DelayDays = 0,
                    CreatedAt = DateTimeOffset.UtcNow
                });

            db.Inventory.AddRange(
                new InventoryEntity
                {
                    StoreId = "MIA-009", StoreName = "Miami Flagship", Sku = "SKU-1001",
                    ProductName = "Wireless Mouse", QuantityOnHand = 3, ReorderThreshold = 15,
                    LastRestocked = DateTimeOffset.UtcNow.AddDays(-10)
                },
                new InventoryEntity
                {
                    StoreId = "TPA-010", StoreName = "Tampa Gateway", Sku = "SKU-1001",
                    ProductName = "Wireless Mouse", QuantityOnHand = 8, ReorderThreshold = 15,
                    LastRestocked = DateTimeOffset.UtcNow.AddDays(-5)
                });

            db.SaveChanges();
        }

        return db;
    }

    private static AuditRepository CreateAuditRepository()
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"AuditSupplyChain_{Guid.NewGuid()}")
            .Options;
        return new AuditRepository(new SquadCommerceDbContext(options), NullLogger<AuditRepository>.Instance);
    }
}
