using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Mcp.Data.Entities;
using Xunit;

namespace SquadCommerce.Agents.Tests.Domain;

public class LogisticsAgentTests
{
    [Fact]
    public void Should_ThrowArgumentNullException_When_DbContextIsNull()
    {
        var act = () => new LogisticsAgent(null!, NullLogger<LogisticsAgent>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContext");
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        var act = () => new LogisticsAgent(CreateEmptyDbContext(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Should_HaveCorrectAgentName()
    {
        var agent = new LogisticsAgent(CreateEmptyDbContext(), NullLogger<LogisticsAgent>.Instance);
        agent.AgentName.Should().Be("LogisticsAgent");
    }

    [Fact]
    public async Task Should_ReturnReroutingMap_When_DelayedShipmentsExist()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new LogisticsAgent(dbContext, NullLogger<LogisticsAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1001", 3, "Port congestion", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.A2UIPayload.Should().BeOfType<ReroutingMapData>();
        var mapData = (ReroutingMapData)result.A2UIPayload!;
        mapData.Sku.Should().Be("SKU-1001");
        mapData.DelayDays.Should().Be(3);
        mapData.DelayReason.Should().Be("Port congestion");
    }

    [Fact]
    public async Task Should_CalculateRiskScore_When_AnalyzingShipments()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new LogisticsAgent(dbContext, NullLogger<LogisticsAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1001", 5, "Weather delay", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var mapData = (ReroutingMapData)result.A2UIPayload!;
        mapData.OverallRiskScore.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_NoShipmentsFound()
    {
        // Arrange
        var dbContext = CreateEmptyDbContext();
        var agent = new LogisticsAgent(dbContext, NullLogger<LogisticsAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("INVALID-SKU", 3, "Test", CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("INVALID-SKU");
    }

    [Fact]
    public async Task Should_IncludeAffectedStoreCount_When_ReportingDelays()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new LogisticsAgent(dbContext, NullLogger<LogisticsAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1001", 3, "Port congestion", CancellationToken.None);

        // Assert
        result.TextSummary.Should().Contain("delayed shipment(s)");
        result.TextSummary.Should().Contain("store(s) affected");
    }

    [Fact]
    public async Task Should_AssignRoutePriority_When_BuildingImpactRoutes()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new LogisticsAgent(dbContext, NullLogger<LogisticsAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1001", 7, "Severe weather", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var mapData = (ReroutingMapData)result.A2UIPayload!;
        mapData.Routes.Should().NotBeEmpty();
        mapData.Routes.Should().AllSatisfy(r =>
            r.Priority.Should().BeOneOf("Critical", "High", "Medium", "Low"));
    }

    [Fact]
    public async Task Should_IncludeProductName_When_GeneratingPayload()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new LogisticsAgent(dbContext, NullLogger<LogisticsAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("SKU-1001", 2, "Test", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var mapData = (ReroutingMapData)result.A2UIPayload!;
        mapData.ProductName.Should().Be("Wireless Mouse");
    }

    private static SquadCommerceDbContext CreateEmptyDbContext()
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"LogisticsTest_{Guid.NewGuid()}")
            .Options;
        return new SquadCommerceDbContext(options);
    }

    private static SquadCommerceDbContext CreateSeededDbContext()
    {
        var dbContext = CreateEmptyDbContext();

        dbContext.Shipments.AddRange(
            new ShipmentEntity
            {
                ShipmentId = "SHP-001", Sku = "SKU-1001", ProductName = "Wireless Mouse",
                OriginStoreId = "SEA-001", DestStoreId = "PDX-002", Status = "Delayed",
                EstimatedArrival = DateTimeOffset.UtcNow.AddDays(5), DelayDays = 3,
                DelayReason = "Port congestion", CreatedAt = DateTimeOffset.UtcNow
            },
            new ShipmentEntity
            {
                ShipmentId = "SHP-002", Sku = "SKU-1001", ProductName = "Wireless Mouse",
                OriginStoreId = "SFO-003", DestStoreId = "LAX-004", Status = "Delayed",
                EstimatedArrival = DateTimeOffset.UtcNow.AddDays(4), DelayDays = 2,
                DelayReason = "Weather", CreatedAt = DateTimeOffset.UtcNow
            },
            new ShipmentEntity
            {
                ShipmentId = "SHP-003", Sku = "SKU-1001", ProductName = "Wireless Mouse",
                OriginStoreId = "DEN-005", DestStoreId = "SFO-003", Status = "InTransit",
                EstimatedArrival = DateTimeOffset.UtcNow.AddDays(1), DelayDays = 0,
                DelayReason = null, CreatedAt = DateTimeOffset.UtcNow
            });

        dbContext.Inventory.AddRange(
            new InventoryEntity
            {
                StoreId = "PDX-002", StoreName = "Suburban Mall", Sku = "SKU-1001",
                ProductName = "Wireless Mouse", QuantityOnHand = 5, ReorderThreshold = 15,
                LastRestocked = DateTimeOffset.UtcNow.AddDays(-7)
            },
            new InventoryEntity
            {
                StoreId = "LAX-004", StoreName = "University District", Sku = "SKU-1001",
                ProductName = "Wireless Mouse", QuantityOnHand = 3, ReorderThreshold = 15,
                LastRestocked = DateTimeOffset.UtcNow.AddDays(-5)
            });

        dbContext.SaveChanges();
        return dbContext;
    }
}
