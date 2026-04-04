using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Mcp.Data.Entities;
using Xunit;

namespace SquadCommerce.Agents.Tests.Domain;

public class MerchandisingAgentTests
{
    [Fact]
    public void Should_ThrowArgumentNullException_When_DbContextIsNull()
    {
        var act = () => new MerchandisingAgent(null!, NullLogger<MerchandisingAgent>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContext");
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        var act = () => new MerchandisingAgent(CreateEmptyDbContext(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Should_HaveCorrectAgentName()
    {
        var agent = new MerchandisingAgent(CreateEmptyDbContext(), NullLogger<MerchandisingAgent>.Instance);
        agent.AgentName.Should().Be("MerchandisingAgent");
    }

    [Fact]
    public async Task Should_ReturnFloorplan_When_StoreLayoutExists()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new MerchandisingAgent(dbContext, NullLogger<MerchandisingAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SEA-001", "Electronics", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.A2UIPayload.Should().BeOfType<InteractiveFloorplanData>();
        var floorplan = (InteractiveFloorplanData)result.A2UIPayload!;
        floorplan.StoreId.Should().Be("SEA-001");
        floorplan.FocusSection.Should().Be("Electronics");
        floorplan.Sections.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_SuggestPlacement_When_AnalyzingTrafficPatterns()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new MerchandisingAgent(dbContext, NullLogger<MerchandisingAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SEA-001", "Electronics", CancellationToken.None);

        // Assert
        var floorplan = (InteractiveFloorplanData)result.A2UIPayload!;
        floorplan.Sections.Should().AllSatisfy(s =>
        {
            s.SuggestedPlacement.Should().BeOneOf("Front", "EndCap", "Middle", "Back");
            s.TrafficIntensity.Should().BeInRange(0.0, 1.0);
            s.OptimizationStatus.Should().BeOneOf("Optimal", "NeedsAdjustment", "Critical");
        });
    }

    [Fact]
    public async Task Should_ReturnFailure_When_StoreNotFound()
    {
        // Arrange
        var dbContext = CreateEmptyDbContext();
        var agent = new MerchandisingAgent(dbContext, NullLogger<MerchandisingAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("INVALID-STORE", "Electronics", CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("INVALID-STORE");
    }

    [Fact]
    public async Task Should_IdentifyAdjustmentsNeeded_When_PlacementsSuboptimal()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new MerchandisingAgent(dbContext, NullLogger<MerchandisingAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SEA-001", "Electronics", CancellationToken.None);

        // Assert
        result.TextSummary.Should().Contain("section(s) need planogram adjustments");
        result.TextSummary.Should().Contain("Electronics");
    }

    [Fact]
    public async Task Should_IncludeStoreName_When_GeneratingPayload()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new MerchandisingAgent(dbContext, NullLogger<MerchandisingAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SEA-001", "Electronics", CancellationToken.None);

        // Assert
        var floorplan = (InteractiveFloorplanData)result.A2UIPayload!;
        floorplan.StoreName.Should().Be("Downtown Flagship");
    }

    private static SquadCommerceDbContext CreateEmptyDbContext()
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"MerchandisingTest_{Guid.NewGuid()}")
            .Options;
        return new SquadCommerceDbContext(options);
    }

    private static SquadCommerceDbContext CreateSeededDbContext()
    {
        var dbContext = CreateEmptyDbContext();

        dbContext.StoreLayouts.AddRange(
            new StoreLayoutEntity
            {
                StoreId = "SEA-001", StoreName = "Downtown Flagship", Section = "Electronics",
                SquareFootage = 1200, ShelfCount = 24, AvgHourlyTraffic = 180.0,
                OptimalPlacement = "Front"
            },
            new StoreLayoutEntity
            {
                StoreId = "SEA-001", StoreName = "Downtown Flagship", Section = "Grocery",
                SquareFootage = 800, ShelfCount = 16, AvgHourlyTraffic = 120.0,
                OptimalPlacement = "Middle"
            },
            new StoreLayoutEntity
            {
                StoreId = "SEA-001", StoreName = "Downtown Flagship", Section = "Home",
                SquareFootage = 600, ShelfCount = 12, AvgHourlyTraffic = 40.0,
                OptimalPlacement = "Back"
            });

        dbContext.SaveChanges();
        return dbContext;
    }
}
