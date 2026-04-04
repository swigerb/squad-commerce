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
/// Tests for the Store Readiness orchestrator workflow.
/// Validates Traffic → Merchandising → ManagerHITL pipeline.
/// </summary>
public class StoreReadinessWorkflowTests
{
    [Fact]
    public async Task Should_CompleteStoreReadiness_When_StoreLayoutExists()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedLayouts: true);

        // Act
        var result = await orchestrator.ProcessStoreReadinessAsync(
            "SEA-001", "Electronics", DateTimeOffset.UtcNow.AddDays(30), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AgentResults.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task Should_IncludeTrafficAnalysis_When_WorkflowCompletes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedLayouts: true);

        // Act
        var result = await orchestrator.ProcessStoreReadinessAsync(
            "SEA-001", "Electronics", DateTimeOffset.UtcNow.AddDays(30), CancellationToken.None);

        // Assert
        result.AgentResults.Should().Contain(r =>
            r.TextSummary.Contains("traffic") || r.TextSummary.Contains("zone"),
            "should include traffic analysis from TrafficAnalystAgent");
    }

    [Fact]
    public async Task Should_IncludeMerchandisingAnalysis_When_WorkflowCompletes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedLayouts: true);

        // Act
        var result = await orchestrator.ProcessStoreReadinessAsync(
            "SEA-001", "Electronics", DateTimeOffset.UtcNow.AddDays(30), CancellationToken.None);

        // Assert
        result.AgentResults.Should().Contain(r =>
            r.TextSummary.Contains("planogram") || r.TextSummary.Contains("Merchandising") || r.TextSummary.Contains("section"),
            "should include merchandising optimization from MerchandisingAgent");
    }

    [Fact]
    public async Task Should_IncludeManagerApproval_When_WorkflowCompletes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedLayouts: true);

        // Act
        var result = await orchestrator.ProcessStoreReadinessAsync(
            "SEA-001", "Electronics", DateTimeOffset.UtcNow.AddDays(30), CancellationToken.None);

        // Assert
        result.AgentResults.Should().Contain(r =>
            r.TextSummary.Contains("Approved") || r.TextSummary.Contains("Deferred") || r.TextSummary.Contains("manager"),
            "should include manager HITL approval");
    }

    [Fact]
    public async Task Should_TrackDuration_When_WorkflowCompletes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedLayouts: true);

        // Act
        var result = await orchestrator.ProcessStoreReadinessAsync(
            "SEA-001", "Electronics", DateTimeOffset.UtcNow.AddDays(30), CancellationToken.None);

        // Assert
        result.WorkflowDuration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task Should_HandleMissingStore_When_NoLayoutExists()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedLayouts: false);

        // Act
        var result = await orchestrator.ProcessStoreReadinessAsync(
            "NONEXISTENT", "Electronics", DateTimeOffset.UtcNow.AddDays(30), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ExecutiveSummary.Should().NotBeNullOrEmpty();
    }

    private static ChiefSoftwareArchitectAgent CreateOrchestrator(bool seedLayouts)
    {
        var inventoryRepo = new InMemoryInventoryRepository();
        var pricingRepo = new InMemoryPricingRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);

        var layoutDb = CreateDbContext(seedLayouts);

        var inventoryAgent = new InventoryAgent(inventoryRepo, NullLogger<InventoryAgent>.Instance);
        var pricingAgent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);
        var marketIntelAgent = new MarketIntelAgent(a2aClient, validator, CreateDbContext(false), NullLogger<MarketIntelAgent>.Instance);
        var marketingAgent = new MarketingAgent(CreateDbContext(false), pricingRepo, NullLogger<MarketingAgent>.Instance);
        var logisticsAgent = new LogisticsAgent(CreateDbContext(false), NullLogger<LogisticsAgent>.Instance);
        var redistributionAgent = new RedistributionAgent(inventoryRepo, NullLogger<RedistributionAgent>.Instance);
        var trafficAgent = new TrafficAnalystAgent(layoutDb, NullLogger<TrafficAnalystAgent>.Instance);
        var merchandisingAgent = new MerchandisingAgent(layoutDb, NullLogger<MerchandisingAgent>.Instance);
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

    private static SquadCommerceDbContext CreateDbContext(bool seedLayouts)
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"StoreReadinessTest_{Guid.NewGuid()}")
            .Options;
        var db = new SquadCommerceDbContext(options);

        if (seedLayouts)
        {
            db.StoreLayouts.AddRange(
                new StoreLayoutEntity
                {
                    StoreId = "SEA-001", StoreName = "Downtown Flagship", Section = "Electronics",
                    SquareFootage = 1200, ShelfCount = 24, AvgHourlyTraffic = 200.0,
                    OptimalPlacement = "Front"
                },
                new StoreLayoutEntity
                {
                    StoreId = "SEA-001", StoreName = "Downtown Flagship", Section = "Grocery",
                    SquareFootage = 800, ShelfCount = 16, AvgHourlyTraffic = 120.0,
                    OptimalPlacement = "EndCap"
                },
                new StoreLayoutEntity
                {
                    StoreId = "SEA-001", StoreName = "Downtown Flagship", Section = "Home",
                    SquareFootage = 600, ShelfCount = 12, AvgHourlyTraffic = 40.0,
                    OptimalPlacement = "Back"
                });
            db.SaveChanges();
        }

        return db;
    }

    private static AuditRepository CreateAuditRepository()
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"AuditStoreReadiness_{Guid.NewGuid()}")
            .Options;
        return new AuditRepository(new SquadCommerceDbContext(options), NullLogger<AuditRepository>.Instance);
    }
}
