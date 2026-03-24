using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Contracts.Models;
using SquadCommerce.Mcp.Tools;

namespace SquadCommerce.Mcp.Tests.Tools;

public class GetInventoryLevelsToolTests
{
    [Fact]
    public async Task Should_ReturnInventoryForSku_When_ValidSkuProvided()
    {
        // Arrange
        var mockRepo = new Mock<IInventoryRepository>();
        mockRepo.Setup(r => r.GetInventoryLevelsAsync("SKU-1001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<InventorySnapshot>
                {
                    new InventorySnapshot { StoreId = "SEA-001", Sku = "SKU-1001", UnitsOnHand = 45, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow },
                    new InventorySnapshot { StoreId = "PDX-002", Sku = "SKU-1001", UnitsOnHand = 32, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow }
                });

        var tool = new GetInventoryLevelsTool(mockRepo.Object, NullLogger<GetInventoryLevelsTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("SKU-1001", null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var skuProp = result.GetType().GetProperty("Sku");
        skuProp.Should().NotBeNull();
        var sku = (string)skuProp!.GetValue(result)!;
        sku.Should().Be("SKU-1001");
    }

    [Fact]
    public async Task Should_ReturnInventoryForStore_When_ValidStoreIdProvided()
    {
        // Arrange
        var mockRepo = new Mock<IInventoryRepository>();
        mockRepo.Setup(r => r.GetInventoryForStoreAsync("SEA-001", "SKU-1001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InventorySnapshot { StoreId = "SEA-001", Sku = "SKU-1001", UnitsOnHand = 45, ReorderPoint = 20, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow });
        mockRepo.Setup(r => r.GetInventoryForStoreAsync("SEA-001", "SKU-1002", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InventorySnapshot { StoreId = "SEA-001", Sku = "SKU-1002", UnitsOnHand = 120, ReorderPoint = 50, UnitsOnOrder = 0, LastUpdated = DateTimeOffset.UtcNow });

        var tool = new GetInventoryLevelsTool(mockRepo.Object, NullLogger<GetInventoryLevelsTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync(null, "SEA-001", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var storeIdProp = result.GetType().GetProperty("StoreId");
        storeIdProp.Should().NotBeNull();
        var storeId = (string)storeIdProp!.GetValue(result)!;
        storeId.Should().Be("SEA-001");
    }

    [Fact]
    public async Task Should_ReturnError_When_NoParametersProvided()
    {
        // Arrange
        var mockRepo = new Mock<IInventoryRepository>();

        var tool = new GetInventoryLevelsTool(mockRepo.Object, NullLogger<GetInventoryLevelsTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync(null, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var successProp = result.GetType().GetProperty("Success");
        var success = (bool)successProp!.GetValue(result)!;
        success.Should().BeFalse();
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_NoMatchingInventoryFound()
    {
        // Arrange
        var mockRepo = new Mock<IInventoryRepository>();
        mockRepo.Setup(r => r.GetInventoryLevelsAsync("INVALID-SKU", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<InventorySnapshot>());

        var tool = new GetInventoryLevelsTool(mockRepo.Object, NullLogger<GetInventoryLevelsTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("INVALID-SKU", null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var skuProp = result.GetType().GetProperty("Sku");
        var sku = (string)skuProp!.GetValue(result)!;
        sku.Should().Be("INVALID-SKU");
    }

    [Fact]
    public async Task Should_HandleCancellation_When_CancellationRequested()
    {
        // Arrange
        var mockRepo = new Mock<IInventoryRepository>();
        mockRepo.Setup(r => r.GetInventoryLevelsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

        var tool = new GetInventoryLevelsTool(mockRepo.Object, NullLogger<GetInventoryLevelsTool>.Instance);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - The tool catches exceptions and returns structured errors
        var result = await tool.ExecuteAsync("SKU-1001", null, cts.Token);

        // Assert
        result.Should().NotBeNull();
        var successProp = result.GetType().GetProperty("Success");
        var success = (bool)successProp!.GetValue(result)!;
        success.Should().BeFalse();
    }
}

