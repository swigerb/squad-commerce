using Xunit;
using FluentAssertions;
using SquadCommerce.Mcp.Data;

namespace SquadCommerce.Mcp.Tests.Data;

public class InventoryRepositoryTests
{
    private readonly InventoryRepository _repository;

    public InventoryRepositoryTests()
    {
        _repository = new InventoryRepository();
    }

    [Fact]
    public async Task Should_ReturnInventoryForSku_When_SkuExists()
    {
        // Arrange
        var sku = "SKU-1001";

        // Act
        var result = await _repository.GetInventoryLevelsAsync(sku);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(inv => inv.Sku.Should().Be(sku));
        result.Should().AllSatisfy(inv => inv.UnitsOnHand.Should().BeGreaterThanOrEqualTo(0));
    }

    [Fact]
    public async Task Should_ReturnInventoryForStore_When_StoreAndSkuExist()
    {
        // Arrange
        var storeId = "SEA-001";
        var sku = "SKU-1001";

        // Act
        var result = await _repository.GetInventoryForStoreAsync(storeId, sku);

        // Assert
        result.Should().NotBeNull();
        result!.StoreId.Should().Be(storeId);
        result.Sku.Should().Be(sku);
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_SkuDoesNotExist()
    {
        // Arrange
        var invalidSku = "INVALID-SKU-999";

        // Act
        var result = await _repository.GetInventoryLevelsAsync(invalidSku);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReturnNull_When_StoreAndSkuNotFound()
    {
        // Arrange
        var invalidStoreId = "INVALID-STORE-999";
        var sku = "SKU-1001";

        // Act
        var result = await _repository.GetInventoryForStoreAsync(invalidStoreId, sku);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Should_ReturnConsistentData_When_QueriedMultipleTimes()
    {
        // Arrange
        var sku = "SKU-1002";

        // Act
        var result1 = await _repository.GetInventoryLevelsAsync(sku);
        var result2 = await _repository.GetInventoryLevelsAsync(sku);

        // Assert
        result1.Should().HaveCount(result2.Count);
        result1.Should().BeEquivalentTo(result2);
    }

    [Fact]
    public async Task Should_IncludeReorderThreshold_When_InventoryReturned()
    {
        // Arrange
        var sku = "SKU-1003";

        // Act
        var result = await _repository.GetInventoryLevelsAsync(sku);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(inv => inv.ReorderPoint.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task Should_ReturnMultipleStores_When_SkuIsStockedWide()
    {
        // Arrange
        var sku = "SKU-1001";

        // Act
        var result = await _repository.GetInventoryLevelsAsync(sku);

        // Assert
        result.Should().HaveCountGreaterThan(1);
        result.Select(inv => inv.StoreId).Distinct().Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task Should_IncludeTimestamp_When_InventoryReturned()
    {
        // Arrange
        var sku = "SKU-1004";

        // Act
        var result = await _repository.GetInventoryLevelsAsync(sku);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(inv => inv.LastUpdated.Should().BeOnOrBefore(DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task Should_UseInMemoryStore_When_RepositoryInstantiated()
    {
        // Arrange - Repository uses in-memory data

        // Act
        var result = await _repository.GetInventoryLevelsAsync("SKU-1001");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(inv => inv.Should().NotBeNull());
    }
}

