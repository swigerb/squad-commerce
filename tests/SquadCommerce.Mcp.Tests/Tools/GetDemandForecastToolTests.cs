using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Contracts.Models;
using SquadCommerce.Mcp.Tools;

namespace SquadCommerce.Mcp.Tests.Tools;

public class GetDemandForecastToolTests
{
    private readonly Mock<IInventoryRepository> _mockRepo = new();

    private GetDemandForecastTool CreateTool(string? dbName = null)
    {
        var context = DbContextTestHelper.CreateSeededContext(dbName ?? Guid.NewGuid().ToString());
        return new GetDemandForecastTool(_mockRepo.Object, context, NullLogger<GetDemandForecastTool>.Instance);
    }

    [Fact]
    public async Task Should_ReturnForecast_When_ValidSkuProvided()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetInventoryLevelsAsync("SKU-3001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventorySnapshot>
            {
                new() { StoreId = "SEA-001", Sku = "SKU-3001", UnitsOnHand = 50, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow },
                new() { StoreId = "PDX-002", Sku = "SKU-3001", UnitsOnHand = 30, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow }
            });

        var tool = CreateTool();

        // Act
        var result = await tool.ExecuteAsync("SKU-3001", null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        GetProp<string>(result, "Sku").Should().Be("SKU-3001");
        GetProp<int>(result, "TotalCurrentInventory").Should().Be(80);
        var storeForecast = GetProp<object[]>(result, "StoreForecast");
        storeForecast.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_ReturnError_When_SkuMissing()
    {
        // Arrange
        var tool = CreateTool();

        // Act
        var result = await tool.ExecuteAsync("", null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
        GetProp<string>(result, "Error").Should().Contain("sku is required");
    }

    [Fact]
    public async Task Should_ReturnError_When_SkuIsNull()
    {
        // Arrange
        var tool = CreateTool();

        // Act
        var result = await tool.ExecuteAsync(null!, null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
    }

    [Fact]
    public async Task Should_ReturnError_When_NoInventoryFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetInventoryLevelsAsync("NO-INV", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventorySnapshot>());

        var tool = CreateTool();

        // Act
        var result = await tool.ExecuteAsync("NO-INV", null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
        GetProp<string>(result, "Error").Should().Contain("No inventory data found");
    }

    [Fact]
    public async Task Should_CalculateHighDemandMultiplier_When_HighVelocity()
    {
        // Arrange — SKU-3001 sentiment velocities: 4.5, 3.2, 0.8 → avg ~2.83
        _mockRepo.Setup(r => r.GetInventoryLevelsAsync("SKU-3001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventorySnapshot>
            {
                new() { StoreId = "SEA-001", Sku = "SKU-3001", UnitsOnHand = 50, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow }
            });

        var tool = CreateTool();

        // Act
        var result = await tool.ExecuteAsync("SKU-3001", null, CancellationToken.None);

        // Assert
        GetProp<double>(result, "DemandMultiplier").Should().BeGreaterThan(2.0);
    }

    [Fact]
    public async Task Should_UseMinimumMultiplier_When_NoSentimentData()
    {
        // Arrange — Use a SKU with no sentiment data
        _mockRepo.Setup(r => r.GetInventoryLevelsAsync("SKU-9999", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventorySnapshot>
            {
                new() { StoreId = "SEA-001", Sku = "SKU-9999", UnitsOnHand = 50, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow }
            });

        var tool = CreateTool();

        // Act
        var result = await tool.ExecuteAsync("SKU-9999", null, CancellationToken.None);

        // Assert — default velocity is 1.0, so multiplier = max(1.0, 1.0) = 1.0
        GetProp<double>(result, "DemandMultiplier").Should().Be(1.0);
    }

    [Fact]
    public async Task Should_FilterByRegion_When_RegionProvided()
    {
        // Arrange — SKU-3001 Northeast has velocities 4.5 and 3.2 → avg 3.85
        _mockRepo.Setup(r => r.GetInventoryLevelsAsync("SKU-3001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventorySnapshot>
            {
                new() { StoreId = "SEA-001", Sku = "SKU-3001", UnitsOnHand = 50, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow }
            });

        var tool = CreateTool();

        // Act
        var result = await tool.ExecuteAsync("SKU-3001", "Northeast", CancellationToken.None);

        // Assert — Higher multiplier when filtering to Northeast only (3.85 vs 2.83)
        GetProp<double>(result, "DemandMultiplier").Should().BeGreaterThan(3.0);
    }

    [Fact]
    public async Task Should_IncludeRecommendedActions_When_ForecastGenerated()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetInventoryLevelsAsync("SKU-3001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventorySnapshot>
            {
                new() { StoreId = "SEA-001", Sku = "SKU-3001", UnitsOnHand = 50, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow }
            });

        var tool = CreateTool();

        // Act
        var result = await tool.ExecuteAsync("SKU-3001", null, CancellationToken.None);

        // Assert
        var actions = GetProp<string[]>(result, "RecommendedActions");
        actions.Should().NotBeEmpty();
        actions.Should().Contain(a => a.Contains("Monitor social sentiment"));
    }

    [Fact]
    public async Task Should_HandleException_When_RepositoryThrows()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetInventoryLevelsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Connection timed out"));

        var tool = CreateTool();

        // Act
        var result = await tool.ExecuteAsync("SKU-3001", null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
        GetProp<string>(result, "Error").Should().Contain("Connection timed out");
    }

    [Fact]
    public async Task Should_IdentifyStockoutRisk_When_LowInventory()
    {
        // Arrange — Very low stock with high demand
        _mockRepo.Setup(r => r.GetInventoryLevelsAsync("SKU-3001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventorySnapshot>
            {
                new() { StoreId = "SEA-001", Sku = "SKU-3001", UnitsOnHand = 3, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow }
            });

        var tool = CreateTool();

        // Act
        var result = await tool.ExecuteAsync("SKU-3001", null, CancellationToken.None);

        // Assert
        GetProp<int>(result, "CriticalStoreCount").Should().BeGreaterThan(0);
    }

    private static T GetProp<T>(object obj, string name) =>
        (T)obj.GetType().GetProperty(name)!.GetValue(obj)!;
}
