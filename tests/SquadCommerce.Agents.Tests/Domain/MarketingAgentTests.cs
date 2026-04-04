using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Mcp.Data.Entities;
using Xunit;

namespace SquadCommerce.Agents.Tests.Domain;

public class MarketingAgentTests
{
    [Fact]
    public void Should_ThrowArgumentNullException_When_DbContextIsNull()
    {
        var act = () => new MarketingAgent(null!, Mock.Of<IPricingRepository>(), NullLogger<MarketingAgent>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContext");
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_PricingRepositoryIsNull()
    {
        var dbContext = CreateEmptyDbContext();
        var act = () => new MarketingAgent(dbContext, null!, NullLogger<MarketingAgent>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("pricingRepository");
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        var dbContext = CreateEmptyDbContext();
        var act = () => new MarketingAgent(dbContext, Mock.Of<IPricingRepository>(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Should_HaveCorrectAgentName()
    {
        var agent = new MarketingAgent(CreateEmptyDbContext(), Mock.Of<IPricingRepository>(), NullLogger<MarketingAgent>.Instance);
        agent.AgentName.Should().Be("MarketingAgent");
    }

    [Fact]
    public async Task Should_BuildCampaignPreview_When_ValidSkuProvided()
    {
        // Arrange
        var pricingRepo = new InMemoryPricingRepository();
        var agent = new MarketingAgent(CreateEmptyDbContext(), pricingRepo, NullLogger<MarketingAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1001", 4.0m, "West Coast", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.A2UIPayload.Should().BeOfType<CampaignPreviewData>();
        var campaign = (CampaignPreviewData)result.A2UIPayload!;
        campaign.Sku.Should().Be("SKU-1001");
        campaign.ProductName.Should().Be("Wireless Mouse");
        campaign.TargetRegion.Should().Be("West Coast");
    }

    [Fact]
    public async Task Should_CalculateFlashSaleDiscount_When_HighDemandMultiplier()
    {
        // Arrange
        var pricingRepo = new InMemoryPricingRepository();
        var agent = new MarketingAgent(CreateEmptyDbContext(), pricingRepo, NullLogger<MarketingAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1001", 4.0m, "West Coast", CancellationToken.None);

        // Assert
        var campaign = (CampaignPreviewData)result.A2UIPayload!;
        campaign.FlashSalePrice.Should().BeLessThan(campaign.OriginalPrice, "flash sale should discount the price");
        campaign.FlashSalePrice.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Should_ApplyLargerDiscount_When_LowerDemandMultiplier()
    {
        // Arrange
        var pricingRepo = new InMemoryPricingRepository();
        var agent = new MarketingAgent(CreateEmptyDbContext(), pricingRepo, NullLogger<MarketingAgent>.Instance);

        // Act — lower demand multiplier should get bigger discount
        var highDemandResult = await agent.ExecuteAsync("SKU-1001", 4.0m, "West Coast", CancellationToken.None);
        var lowDemandResult = await agent.ExecuteAsync("SKU-1001", 1.5m, "West Coast", CancellationToken.None);

        // Assert
        var highCampaign = (CampaignPreviewData)highDemandResult.A2UIPayload!;
        var lowCampaign = (CampaignPreviewData)lowDemandResult.A2UIPayload!;
        var highDiscount = 1 - (highCampaign.FlashSalePrice / highCampaign.OriginalPrice);
        var lowDiscount = 1 - (lowCampaign.FlashSalePrice / lowCampaign.OriginalPrice);
        lowDiscount.Should().BeGreaterThan(highDiscount, "lower demand should yield larger discount to drive sales");
    }

    [Fact]
    public async Task Should_GenerateEmailCopy_When_CampaignBuilt()
    {
        // Arrange
        var pricingRepo = new InMemoryPricingRepository();
        var agent = new MarketingAgent(CreateEmptyDbContext(), pricingRepo, NullLogger<MarketingAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1005", 3.0m, "Northeast", CancellationToken.None);

        // Assert
        var campaign = (CampaignPreviewData)result.A2UIPayload!;
        campaign.EmailSubjectLine.Should().Contain("Mechanical Keyboard");
        campaign.EmailSubjectLine.Should().Contain("Northeast");
        campaign.EmailBody.Should().Contain("Mechanical Keyboard");
        campaign.HeroBannerHeadline.Should().NotBeNullOrEmpty();
        campaign.CallToAction.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_IncludePricingInSummary_When_AnalysisCompletes()
    {
        // Arrange
        var pricingRepo = new InMemoryPricingRepository();
        var agent = new MarketingAgent(CreateEmptyDbContext(), pricingRepo, NullLogger<MarketingAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1001", 2.5m, "East Coast", CancellationToken.None);

        // Assert
        result.TextSummary.Should().Contain("Campaign preview");
        result.TextSummary.Should().Contain("Flash sale");
        result.TextSummary.Should().Contain("East Coast");
    }

    private static SquadCommerceDbContext CreateEmptyDbContext()
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"MarketingTest_{Guid.NewGuid()}")
            .Options;
        return new SquadCommerceDbContext(options);
    }
}
