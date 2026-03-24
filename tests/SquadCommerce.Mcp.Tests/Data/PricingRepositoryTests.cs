using Xunit;
using FluentAssertions;
using SquadCommerce.Contracts.Models;
using SquadCommerce.Mcp.Data;

namespace SquadCommerce.Mcp.Tests.Data;

public class PricingRepositoryTests
{
    private readonly InMemoryPricingRepository _repository;

    public PricingRepositoryTests()
    {
        _repository = new InMemoryPricingRepository();
    }

    [Fact]
    public async Task Should_ReturnCurrentPrice_When_StoreAndSkuExist()
    {
        // Arrange
        var storeId = "SEA-001";
        var sku = "SKU-1001";

        // Act
        var result = await _repository.GetCurrentPriceAsync(storeId, sku);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Should_ReturnNull_When_StoreOrSkuNotFound()
    {
        // Arrange
        var invalidStoreId = "INVALID-999";
        var sku = "SKU-1001";

        // Act
        var result = await _repository.GetCurrentPriceAsync(invalidStoreId, sku);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Should_UpdatePrice_When_ValidParametersProvided()
    {
        // Arrange
        var storeId = "SEA-001";
        var sku = "SKU-1001";
        var oldPrice = await _repository.GetCurrentPriceAsync(storeId, sku);
        var newPrice = 29.99m;

        var priceChange = new PriceChange
        {
            Sku = sku,
            StoreId = storeId,
            OldPrice = oldPrice!.Value,
            NewPrice = newPrice,
            Reason = "Test update",
            RequestedBy = "Test",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _repository.UpdatePricingAsync(priceChange);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.StoresUpdated.Should().Contain(storeId);
        
        // Verify the price was updated
        var updatedPrice = await _repository.GetCurrentPriceAsync(storeId, sku);
        updatedPrice.Should().Be(newPrice);
    }

    [Fact]
    public async Task Should_RejectUpdate_When_PriceBelowCost()
    {
        // Arrange
        var storeId = "SEA-001";
        var sku = "SKU-1001";
        var currentPrice = await _repository.GetCurrentPriceAsync(storeId, sku);
        var belowCostPrice = 5.00m; // Below the cost of $15

        var priceChange = new PriceChange
        {
            Sku = sku,
            StoreId = storeId,
            OldPrice = currentPrice!.Value,
            NewPrice = belowCostPrice,
            Reason = "Test",
            RequestedBy = "Test",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _repository.UpdatePricingAsync(priceChange);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("below cost");
    }

    [Fact]
    public async Task Should_ReturnFailure_When_SkuNotFound()
    {
        // Arrange
        var invalidSku = "INVALID-SKU-999";
        var priceChange = new PriceChange
        {
            Sku = invalidSku,
            StoreId = "SEA-001",
            OldPrice = 10m,
            NewPrice = 20m,
            Reason = "Test",
            RequestedBy = "Test",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _repository.UpdatePricingAsync(priceChange);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Should_HandleConcurrentUpdates_When_MultipleUpdatesOccur()
    {
        // Arrange
        var storeId = "LAX-004";
        var sku = "SKU-1005";
        var currentPrice = await _repository.GetCurrentPriceAsync(storeId, sku);

        // Act - Simulate concurrent updates
        var tasks = Enumerable.Range(1, 5).Select(async i =>
        {
            var price = 100m + i;
            var priceChange = new PriceChange
            {
                Sku = sku,
                StoreId = storeId,
                OldPrice = currentPrice!.Value,
                NewPrice = price,
                Reason = $"Test update {i}",
                RequestedBy = "Test",
                Timestamp = DateTimeOffset.UtcNow
            };
            return await _repository.UpdatePricingAsync(priceChange);
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        
        // Last update should win
        var finalPrice = await _repository.GetCurrentPriceAsync(storeId, sku);
        finalPrice.Should().BeInRange(101m, 105m);
    }
}
