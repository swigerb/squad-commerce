using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SquadCommerce.A2A;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Contracts.Models;

namespace SquadCommerce.A2A.Tests;

public class A2AServerTests
{
    private readonly A2AServer _server;
    private readonly Mock<IInventoryRepository> _inventoryRepo;
    private readonly Mock<IPricingRepository> _pricingRepo;

    public A2AServerTests()
    {
        _inventoryRepo = new Mock<IInventoryRepository>();
        _pricingRepo = new Mock<IPricingRepository>();

        // Default: return empty results so handlers don't throw
        _inventoryRepo
            .Setup(r => r.GetInventoryLevelsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<InventorySnapshot>());
        _pricingRepo
            .Setup(r => r.GetCurrentPriceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal?)null);

        _server = new A2AServer(
            _inventoryRepo.Object,
            _pricingRepo.Object,
            Mock.Of<ILogger<A2AServer>>());
    }

    [Fact]
    public async Task Should_ReturnError_When_UnknownCapabilityRequested()
    {
        // Arrange
        var request = new A2ARequest
        {
            AgentId = "test.agent",
            RequestId = Guid.NewGuid().ToString(),
            Capability = "UnknownCapability",
            Parameters = new Dictionary<string, object>(),
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var response = await _server.HandleRequest(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.ErrorMessage.Should().Contain("Unknown capability");
    }

    [Fact]
    public async Task Should_EchoRequestId_When_HandlingRequest()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();
        var request = new A2ARequest
        {
            AgentId = "test.agent",
            RequestId = requestId,
            Capability = "GetInventoryLevels",
            Parameters = new Dictionary<string, object>(),
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var response = await _server.HandleRequest(request, CancellationToken.None);

        // Assert
        response.RequestId.Should().Be(requestId);
    }

    [Fact]
    public async Task Should_RouteToGetInventoryLevels_When_CapabilityMatches()
    {
        // Arrange
        var request = new A2ARequest
        {
            AgentId = "external.agent",
            RequestId = Guid.NewGuid().ToString(),
            Capability = "GetInventoryLevels",
            Parameters = new Dictionary<string, object>
            {
                ["sku"] = "MOUSE-001"
            },
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var response = await _server.HandleRequest(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        // Note: Stub implementation returns placeholder response
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_RouteToGetStorePricing_When_CapabilityMatches()
    {
        // Arrange
        var request = new A2ARequest
        {
            AgentId = "external.agent",
            RequestId = Guid.NewGuid().ToString(),
            Capability = "GetStorePricing",
            Parameters = new Dictionary<string, object>
            {
                ["sku"] = "CABLE-002"
            },
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var response = await _server.HandleRequest(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_IncludeMetadata_When_ResponseReturned()
    {
        // Arrange
        var request = new A2ARequest
        {
            AgentId = "test.agent",
            RequestId = Guid.NewGuid().ToString(),
            Capability = "GetInventoryLevels",
            Parameters = new Dictionary<string, object>(),
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var response = await _server.HandleRequest(request, CancellationToken.None);

        // Assert
        response.Metadata.Should().NotBeNull();
        response.Metadata!.Timestamp.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Should_HandleCancellation_When_CancellationRequested()
    {
        // Arrange
        var request = new A2ARequest
        {
            AgentId = "test.agent",
            RequestId = Guid.NewGuid().ToString(),
            Capability = "GetInventoryLevels",
            Parameters = new Dictionary<string, object>(),
            Timestamp = DateTimeOffset.UtcNow
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        // The server handles the request but returns it synchronously since it's a stub
        var response = await _server.HandleRequest(request, cts.Token);

        // Assert - The server returns a response even when cancelled
        response.Should().NotBeNull();
    }
}

