using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Mcp.Data.Entities;
using Xunit;

namespace SquadCommerce.Agents.Tests.Domain;

public class TrafficAnalystAgentTests
{
    [Fact]
    public void Should_ThrowArgumentNullException_When_DbContextIsNull()
    {
        var act = () => new TrafficAnalystAgent(null!, NullLogger<TrafficAnalystAgent>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContext");
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        var act = () => new TrafficAnalystAgent(CreateEmptyDbContext(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Should_HaveCorrectAgentName()
    {
        var agent = new TrafficAnalystAgent(CreateEmptyDbContext(), NullLogger<TrafficAnalystAgent>.Instance);
        agent.AgentName.Should().Be("TrafficAnalystAgent");
    }

    [Fact]
    public async Task Should_ReturnFloorplan_When_StoreExists()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new TrafficAnalystAgent(dbContext, NullLogger<TrafficAnalystAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SEA-001", "Electronics", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.A2UIPayload.Should().BeOfType<InteractiveFloorplanData>();
        var floorplan = (InteractiveFloorplanData)result.A2UIPayload!;
        floorplan.StoreId.Should().Be("SEA-001");
        floorplan.FocusSection.Should().Be("Electronics");
    }

    [Fact]
    public async Task Should_IdentifyHighAndLowTrafficZones_When_Analyzing()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new TrafficAnalystAgent(dbContext, NullLogger<TrafficAnalystAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SEA-001", "Electronics", CancellationToken.None);

        // Assert
        result.TextSummary.Should().Contain("high-traffic zone(s)");
        result.TextSummary.Should().Contain("low-traffic zone(s)");
    }

    [Fact]
    public async Task Should_CalculateTrafficIntensity_When_BuildingFloorplan()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new TrafficAnalystAgent(dbContext, NullLogger<TrafficAnalystAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SEA-001", "Electronics", CancellationToken.None);

        // Assert
        var floorplan = (InteractiveFloorplanData)result.A2UIPayload!;
        floorplan.Sections.Should().AllSatisfy(s =>
        {
            s.TrafficIntensity.Should().BeInRange(0.0, 1.0);
            s.SuggestedPlacement.Should().BeOneOf("Front", "EndCap", "Middle", "Back");
        });

        // Highest-traffic section should have intensity 1.0
        floorplan.Sections.Should().Contain(s => s.TrafficIntensity == 1.0);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_StoreNotFound()
    {
        // Arrange
        var dbContext = CreateEmptyDbContext();
        var agent = new TrafficAnalystAgent(dbContext, NullLogger<TrafficAnalystAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("NONEXISTENT", "Electronics", CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("NONEXISTENT");
    }

    [Fact]
    public async Task Should_IncludeFocusSectionIntensity_When_Summarizing()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new TrafficAnalystAgent(dbContext, NullLogger<TrafficAnalystAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SEA-001", "Electronics", CancellationToken.None);

        // Assert
        result.TextSummary.Should().Contain("Electronics");
        result.TextSummary.Should().Contain("traffic intensity");
    }

    [Fact]
    public async Task Should_DetermineOptimizationStatus_When_ComparingPlacements()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new TrafficAnalystAgent(dbContext, NullLogger<TrafficAnalystAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SEA-001", "Electronics", CancellationToken.None);

        // Assert
        var floorplan = (InteractiveFloorplanData)result.A2UIPayload!;
        floorplan.Sections.Should().AllSatisfy(s =>
            s.OptimizationStatus.Should().BeOneOf("Optimal", "NeedsAdjustment", "Critical"));
    }

    private static SquadCommerceDbContext CreateEmptyDbContext()
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"TrafficTest_{Guid.NewGuid()}")
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
                StoreId = "SEA-001", StoreName = "Downtown Flagship", Section = "Apparel",
                SquareFootage = 600, ShelfCount = 12, AvgHourlyTraffic = 80.0,
                OptimalPlacement = "Middle"
            },
            new StoreLayoutEntity
            {
                StoreId = "SEA-001", StoreName = "Downtown Flagship", Section = "Home",
                SquareFootage = 400, ShelfCount = 8, AvgHourlyTraffic = 30.0,
                OptimalPlacement = "Back"
            });

        dbContext.SaveChanges();
        return dbContext;
    }
}
