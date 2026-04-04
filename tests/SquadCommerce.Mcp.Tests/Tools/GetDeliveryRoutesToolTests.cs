using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Contracts.Models;
using SquadCommerce.Mcp.Tools;

namespace SquadCommerce.Mcp.Tests.Tools;

public class GetDeliveryRoutesToolTests
{
    private readonly Mock<IInventoryRepository> _mockRepo = new();

    private GetDeliveryRoutesTool CreateTool() =>
        new(_mockRepo.Object, NullLogger<GetDeliveryRoutesTool>.Instance);

    [Fact]
    public async Task Should_ReturnRoutes_When_SurplusAndAtRiskStoresExist()
    {
        // Arrange — surplus store (100 > 20*2=40) and at-risk store (5 <= 20)
        _mockRepo.Setup(r => r.GetInventoryLevelsAsync("SKU-2001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventorySnapshot>
            {
                new() { StoreId = "SEA-001", Sku = "SKU-2001", UnitsOnHand = 100, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow },
                new() { StoreId = "PDX-002", Sku = "SKU-2001", UnitsOnHand = 5, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow }
            });

        var tool = CreateTool();

        // Act
        var result = await tool.ExecuteAsync("SKU-2001", null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        GetProp<bool>(result, "Success").Should().BeTrue();
        var routes = GetProp<object[]>(result, "Routes");
        routes.Should().NotBeEmpty();
        GetProp<int>(result, "SurplusStoreCount").Should().BeGreaterThan(0);
        GetProp<int>(result, "AtRiskStoreCount").Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Should_ReturnError_When_SkuIsEmpty()
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
    public async Task Should_ReturnNoRoutes_When_NoSurplusStores()
    {
        // Arrange — all stores are at or below reorder point * 2
        _mockRepo.Setup(r => r.GetInventoryLevelsAsync("SKU-2001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventorySnapshot>
            {
                new() { StoreId = "SEA-001", Sku = "SKU-2001", UnitsOnHand = 30, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow },
                new() { StoreId = "PDX-002", Sku = "SKU-2001", UnitsOnHand = 5, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow }
            });

        var tool = CreateTool();

        // Act
        var result = await tool.ExecuteAsync("SKU-2001", null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var routes = GetProp<object[]>(result, "Routes");
        routes.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReturnNoRoutes_When_NoAtRiskStores()
    {
        // Arrange — all stores well above reorder point
        _mockRepo.Setup(r => r.GetInventoryLevelsAsync("SKU-2001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventorySnapshot>
            {
                new() { StoreId = "SEA-001", Sku = "SKU-2001", UnitsOnHand = 100, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow },
                new() { StoreId = "PDX-002", Sku = "SKU-2001", UnitsOnHand = 80, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow }
            });

        var tool = CreateTool();

        // Act
        var result = await tool.ExecuteAsync("SKU-2001", null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var routes = GetProp<object[]>(result, "Routes");
        routes.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReturnError_When_NoInventoryDataFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetInventoryLevelsAsync("UNKNOWN-SKU", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventorySnapshot>());

        var tool = CreateTool();

        // Act
        var result = await tool.ExecuteAsync("UNKNOWN-SKU", null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
        GetProp<string>(result, "Error").Should().Contain("No inventory data found");
    }

    [Fact]
    public async Task Should_HandleException_When_RepositoryThrows()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetInventoryLevelsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database unavailable"));

        var tool = CreateTool();

        // Act
        var result = await tool.ExecuteAsync("SKU-2001", null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
        GetProp<string>(result, "Error").Should().Contain("Database unavailable");
    }

    [Fact]
    public async Task Should_AssignCriticalPriority_When_StoreHasZeroStock()
    {
        // Arrange — at-risk store with 0 units
        _mockRepo.Setup(r => r.GetInventoryLevelsAsync("SKU-2001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventorySnapshot>
            {
                new() { StoreId = "SEA-001", Sku = "SKU-2001", UnitsOnHand = 100, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow },
                new() { StoreId = "PDX-002", Sku = "SKU-2001", UnitsOnHand = 0, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow }
            });

        var tool = CreateTool();

        // Act
        var result = await tool.ExecuteAsync("SKU-2001", null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var routes = GetProp<object[]>(result, "Routes");
        routes.Should().HaveCount(1);
        GetProp<string>(routes[0], "Priority").Should().Be("Critical");
    }

    private static T GetProp<T>(object obj, string name) =>
        (T)obj.GetType().GetProperty(name)!.GetValue(obj)!;
}
