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
/// Tests for the Viral Spike orchestrator workflow.
/// Validates Sentiment → Pricing → Marketing pipeline.
/// </summary>
public class ViralSpikeWorkflowTests
{
    [Fact]
    public async Task Should_CompleteViralSpikeAnalysis_When_DemandSpikes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedSentiment: true);

        // Act
        var result = await orchestrator.ProcessViralSpikeAsync(
            "SKU-1001", 4.0m, "West Coast", "TikTok", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AgentResults.Should().HaveCountGreaterThan(0);
        result.ExecutiveSummary.Should().Contain("SKU-1001");
    }

    [Fact]
    public async Task Should_IncludeSentimentAnalysis_When_WorkflowCompletes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedSentiment: true);

        // Act
        var result = await orchestrator.ProcessViralSpikeAsync(
            "SKU-1001", 3.5m, "West Coast", "TikTok", CancellationToken.None);

        // Assert
        result.AgentResults.Should().Contain(r =>
            r.TextSummary.Contains("sentiment") || r.TextSummary.Contains("social") || r.TextSummary.Contains("trending"),
            "should include social sentiment analysis");
    }

    [Fact]
    public async Task Should_IncludePricingAnalysis_When_WorkflowCompletes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedSentiment: true);

        // Act
        var result = await orchestrator.ProcessViralSpikeAsync(
            "SKU-1001", 4.0m, "West Coast", "TikTok", CancellationToken.None);

        // Assert
        result.AgentResults.Should().Contain(r =>
            r.TextSummary.Contains("pricing") || r.TextSummary.Contains("flash") || r.TextSummary.Contains("sale") || r.TextSummary.Contains("margin"),
            "should include flash sale pricing analysis");
    }

    [Fact]
    public async Task Should_IncludeMarketingCampaign_When_WorkflowCompletes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedSentiment: true);

        // Act
        var result = await orchestrator.ProcessViralSpikeAsync(
            "SKU-1001", 4.0m, "West Coast", "TikTok", CancellationToken.None);

        // Assert
        result.AgentResults.Should().Contain(r =>
            r.TextSummary.Contains("Campaign") || r.TextSummary.Contains("campaign") || r.TextSummary.Contains("Flash sale"),
            "should include campaign preview from MarketingAgent");
    }

    [Fact]
    public async Task Should_TrackWorkflowDuration_When_AnalysisCompletes()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedSentiment: true);

        // Act
        var result = await orchestrator.ProcessViralSpikeAsync(
            "SKU-1001", 4.0m, "West Coast", "TikTok", CancellationToken.None);

        // Assert
        result.WorkflowDuration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task Should_HandleNoSentimentData_When_SkuHasNoSocialPresence()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(seedSentiment: false);

        // Act
        var result = await orchestrator.ProcessViralSpikeAsync(
            "SKU-1001", 2.0m, "Midwest", "Instagram", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ExecutiveSummary.Should().NotBeNullOrEmpty();
    }

    private static ChiefSoftwareArchitectAgent CreateOrchestrator(bool seedSentiment)
    {
        var inventoryRepo = new InMemoryInventoryRepository();
        var pricingRepo = new InMemoryPricingRepository();
        var a2aClient = new A2AClient(new HttpClient(), NullLogger<A2AClient>.Instance);
        var validator = new ExternalDataValidator(pricingRepo, inventoryRepo, NullLogger<ExternalDataValidator>.Instance);

        var sentimentDb = CreateDbContext(seedSentiment);

        var inventoryAgent = new InventoryAgent(inventoryRepo, NullLogger<InventoryAgent>.Instance);
        var pricingAgent = new PricingAgent(pricingRepo, inventoryRepo, NullLogger<PricingAgent>.Instance);
        var marketIntelAgent = new MarketIntelAgent(a2aClient, validator, sentimentDb, NullLogger<MarketIntelAgent>.Instance);
        var marketingAgent = new MarketingAgent(sentimentDb, pricingRepo, NullLogger<MarketingAgent>.Instance);
        var logisticsAgent = new LogisticsAgent(CreateDbContext(false), NullLogger<LogisticsAgent>.Instance);
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

    private static SquadCommerceDbContext CreateDbContext(bool seedSentiment)
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"ViralSpikeTest_{Guid.NewGuid()}")
            .Options;
        var db = new SquadCommerceDbContext(options);

        if (seedSentiment)
        {
            db.SocialSentiment.AddRange(
                new SocialSentimentEntity
                {
                    Sku = "SKU-1001", ProductName = "Wireless Mouse", Platform = "TikTok",
                    SentimentScore = 0.92, Velocity = 4.5, Region = "West Coast",
                    DetectedAt = DateTimeOffset.UtcNow.AddHours(-2)
                },
                new SocialSentimentEntity
                {
                    Sku = "SKU-1001", ProductName = "Wireless Mouse", Platform = "Instagram",
                    SentimentScore = 0.85, Velocity = 3.2, Region = "West Coast",
                    DetectedAt = DateTimeOffset.UtcNow.AddHours(-1)
                });
            db.SaveChanges();
        }

        return db;
    }

    private static AuditRepository CreateAuditRepository()
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"AuditViralSpike_{Guid.NewGuid()}")
            .Options;
        return new AuditRepository(new SquadCommerceDbContext(options), NullLogger<AuditRepository>.Instance);
    }
}
