using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Contracts.Models;
using SquadCommerce.Mcp.Tools;

namespace SquadCommerce.Mcp.Tests.Tools;

public class UpdateStorePricingToolTests
{
    [Fact]
    public async Task Should_UpdatePricing_When_ValidParametersProvided()
    {
        // Arrange
        var mockRepo = new Mock<IPricingRepository>();
        mockRepo.Setup(r => r.GetCurrentPriceAsync("SEA-001", "SKU-1001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(29.99m);
        mockRepo.Setup(r => r.UpdatePricingAsync(It.IsAny<PriceChange>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PricingUpdateResult
                {
                    Sku = "SKU-1001",
                    StoresUpdated = new[] { "SEA-001" },
                    Success = true,
                    Timestamp = DateTimeOffset.UtcNow
                });

        var tool = new UpdateStorePricingTool(mockRepo.Object, NullLogger<UpdateStorePricingTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("SEA-001", "SKU-1001", 24.99m, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var successProp = result.GetType().GetProperty("Success");
        successProp.Should().NotBeNull();
        var success = (bool)successProp!.GetValue(result)!;
        success.Should().BeTrue();
    }

    [Fact]
    public async Task Should_RejectUpdate_When_PriceIsNegative()
    {
        // Arrange
        var mockRepo = new Mock<IPricingRepository>();
        var tool = new UpdateStorePricingTool(mockRepo.Object, NullLogger<UpdateStorePricingTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("SEA-001", "SKU-1001", -10.00m, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var successProp = result.GetType().GetProperty("Success");
        var success = (bool)successProp!.GetValue(result)!;
        success.Should().BeFalse();
    }

    [Fact]
    public async Task Should_RejectUpdate_When_PriceIsZero()
    {
        // Arrange
        var mockRepo = new Mock<IPricingRepository>();
        var tool = new UpdateStorePricingTool(mockRepo.Object, NullLogger<UpdateStorePricingTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("SEA-001", "SKU-1001", 0m, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var successProp = result.GetType().GetProperty("Success");
        var success = (bool)successProp!.GetValue(result)!;
        success.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "SKU-1001", 24.99)]
    [InlineData("SEA-001", "", 24.99)]
    [InlineData(null, "SKU-1001", 24.99)]
    [InlineData("SEA-001", null, 24.99)]
    public async Task Should_RejectUpdate_When_RequiredParametersMissing(string? storeId, string? sku, decimal price)
    {
        // Arrange
        var mockRepo = new Mock<IPricingRepository>();
        var tool = new UpdateStorePricingTool(mockRepo.Object, NullLogger<UpdateStorePricingTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync(storeId!, sku!, price, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var successProp = result.GetType().GetProperty("Success");
        var success = (bool)successProp!.GetValue(result)!;
        success.Should().BeFalse();
    }

    [Fact]
    public async Task Should_ReturnFailure_When_RepositoryUpdateFails()
    {
        // Arrange
        var mockRepo = new Mock<IPricingRepository>();
        mockRepo.Setup(r => r.GetCurrentPriceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(29.99m);
        mockRepo.Setup(r => r.UpdatePricingAsync(It.IsAny<PriceChange>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PricingUpdateResult
                {
                    Sku = "SKU-1001",
                    StoresUpdated = Array.Empty<string>(),
                    Success = false,
                    ErrorMessage = "Update failed",
                    Timestamp = DateTimeOffset.UtcNow
                });

        var tool = new UpdateStorePricingTool(mockRepo.Object, NullLogger<UpdateStorePricingTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("SEA-001", "SKU-1001", 24.99m, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var successProp = result.GetType().GetProperty("Success");
        var success = (bool)successProp!.GetValue(result)!;
        success.Should().BeFalse();
    }

    [Fact]
    public async Task Should_PersistChanges_When_RepositoryUpdateSucceeds()
    {
        // Arrange
        var mockRepo = new Mock<IPricingRepository>();
        mockRepo.Setup(r => r.GetCurrentPriceAsync("SEA-001", "SKU-1001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(24.99m);
        mockRepo.Setup(r => r.UpdatePricingAsync(It.IsAny<PriceChange>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PricingUpdateResult
                {
                    Sku = "SKU-1001",
                    StoresUpdated = new[] { "SEA-001" },
                    Success = true,
                    Timestamp = DateTimeOffset.UtcNow
                });

        var tool = new UpdateStorePricingTool(mockRepo.Object, NullLogger<UpdateStorePricingTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("SEA-001", "SKU-1001", 29.99m, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        mockRepo.Verify(r => r.UpdatePricingAsync(It.IsAny<PriceChange>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

