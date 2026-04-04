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
/// Tests for the ESG Audit orchestrator workflow.
/// Validates Compliance → Research → Procurement pipeline.
/// </summary>
public class ESGAuditWorkflowTests
{
    [Fact]
    public async Task Should_CompleteESGAudit_When_SuppliersExist()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedSuppliers: true);
        var deadline = DateTimeOffset.UtcNow.AddDays(60);

        // Act
        var result = await orchestrator.ProcessESGAuditAsync("Cocoa", "FairTrade", deadline, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AgentResults.Should().HaveCountGreaterThan(0);
        result.ExecutiveSummary.Should().Contain("Cocoa");
    }

    [Fact]
    public async Task Should_IncludeComplianceResults_When_AuditCompletes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedSuppliers: true);

        // Act
        var result = await orchestrator.ProcessESGAuditAsync(
            "Cocoa", "FairTrade", DateTimeOffset.UtcNow.AddDays(30), CancellationToken.None);

        // Assert
        result.AgentResults.Should().Contain(r => r.TextSummary.Contains("compliance") || r.TextSummary.Contains("ESG"),
            "should contain compliance analysis from ComplianceAgent");
    }

    [Fact]
    public async Task Should_IncludeResearchResults_When_AuditCompletes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedSuppliers: true);

        // Act
        var result = await orchestrator.ProcessESGAuditAsync(
            "Cocoa", "FairTrade", DateTimeOffset.UtcNow.AddDays(30), CancellationToken.None);

        // Assert
        result.AgentResults.Should().Contain(r =>
            r.TextSummary.Contains("watchlist") || r.TextSummary.Contains("flagged") || r.TextSummary.Contains("compliant"),
            "should contain research findings from ResearchAgent");
    }

    [Fact]
    public async Task Should_IncludeProcurementResults_When_AuditCompletes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedSuppliers: true);

        // Act
        var result = await orchestrator.ProcessESGAuditAsync(
            "Cocoa", "FairTrade", DateTimeOffset.UtcNow.AddDays(30), CancellationToken.None);

        // Assert
        result.AgentResults.Should().Contain(r =>
            r.TextSummary.Contains("Procurement") || r.TextSummary.Contains("supplier") || r.TextSummary.Contains("replacement") || r.TextSummary.Contains("compliant"),
            "should contain procurement recommendations from ProcurementAgent");
    }

    [Fact]
    public async Task Should_TrackWorkflowDuration_When_AuditCompletes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedSuppliers: true);

        // Act
        var result = await orchestrator.ProcessESGAuditAsync(
            "Cocoa", "FairTrade", DateTimeOffset.UtcNow.AddDays(30), CancellationToken.None);

        // Assert
        result.WorkflowDuration.Should().BeGreaterThan(TimeSpan.Zero,
            "workflow should track execution time");
    }

    [Fact]
    public async Task Should_HandleMissingCategory_When_NoSuppliersExist()
    {
        // Arrange — empty DB, no suppliers
        var orchestrator = CreateOrchestrator(seedSuppliers: false);

        // Act
        var result = await orchestrator.ProcessESGAuditAsync(
            "NonExistent", "FairTrade", DateTimeOffset.UtcNow.AddDays(30), CancellationToken.None);

        // Assert — should complete (possibly with errors in individual agents)
        result.Should().NotBeNull();
        result.ExecutiveSummary.Should().NotBeNullOrEmpty();
    }

    private static ChiefSoftwareArchitectAgent CreateOrchestrator(bool seedSuppliers)
    {
        var inventoryRepo = new InMemoryInventoryRepository();
        var pricingRepo = new InMemoryPricingRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);

        var dbContext = CreateDbContext(seedSuppliers);

        var inventoryAgent = new InventoryAgent(inventoryRepo, NullLogger<InventoryAgent>.Instance);
        var pricingAgent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);
        var marketIntelAgent = new MarketIntelAgent(a2aClient, validator, dbContext, NullLogger<MarketIntelAgent>.Instance);
        var marketingAgent = new MarketingAgent(dbContext, pricingRepo, NullLogger<MarketingAgent>.Instance);
        var logisticsAgent = new LogisticsAgent(CreateDbContext(false), NullLogger<LogisticsAgent>.Instance);
        var redistributionAgent = new RedistributionAgent(inventoryRepo, NullLogger<RedistributionAgent>.Instance);
        var trafficAgent = new TrafficAnalystAgent(CreateDbContext(false), NullLogger<TrafficAnalystAgent>.Instance);
        var merchandisingAgent = new MerchandisingAgent(CreateDbContext(false), NullLogger<MerchandisingAgent>.Instance);
        var managerAgent = new ManagerAgent(NullLogger<ManagerAgent>.Instance);
        var complianceAgent = new ComplianceAgent(dbContext, NullLogger<ComplianceAgent>.Instance);
        var researchAgent = new ResearchAgent(dbContext, NullLogger<ResearchAgent>.Instance);
        var procurementAgent = new ProcurementAgent(dbContext, NullLogger<ProcurementAgent>.Instance);
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

    private static SquadCommerceDbContext CreateDbContext(bool seedSuppliers)
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"ESGTest_{Guid.NewGuid()}")
            .Options;
        var db = new SquadCommerceDbContext(options);

        if (seedSuppliers)
        {
            db.Suppliers.AddRange(
                new SupplierEntity
                {
                    SupplierId = "SUP-001", Name = "GhanaFarm", Category = "Cocoa", Country = "Ghana",
                    Certification = "FairTrade", CertificationExpiry = DateTimeOffset.UtcNow.AddYears(1),
                    Status = "Compliant"
                },
                new SupplierEntity
                {
                    SupplierId = "SUP-002", Name = "IvoryBad", Category = "Cocoa", Country = "Ivory Coast",
                    Certification = "None", CertificationExpiry = DateTimeOffset.UtcNow.AddDays(-30),
                    Status = "NonCompliant", WatchlistNotes = "Deforestation"
                },
                new SupplierEntity
                {
                    SupplierId = "SUP-003", Name = "EcuadorRisk", Category = "Cocoa", Country = "Ecuador",
                    Certification = "FairTrade", CertificationExpiry = DateTimeOffset.UtcNow.AddDays(20),
                    Status = "AtRisk", WatchlistNotes = "Cert expiring"
                });
            db.SaveChanges();
        }

        return db;
    }

    private static AuditRepository CreateAuditRepository()
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"AuditESG_{Guid.NewGuid()}")
            .Options;
        return new AuditRepository(new SquadCommerceDbContext(options), NullLogger<AuditRepository>.Instance);
    }
}
