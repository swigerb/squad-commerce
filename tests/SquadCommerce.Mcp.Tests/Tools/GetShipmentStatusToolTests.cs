using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Mcp.Tools;

namespace SquadCommerce.Mcp.Tests.Tools;

public class GetShipmentStatusToolTests
{
    [Fact]
    public async Task Should_ReturnShipments_When_ValidSkuProvided()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetShipmentStatusTool(context, NullLogger<GetShipmentStatusTool>.Instance);

        // Act — SKU-2001 has 2 shipments (SHP-001, SHP-002)
        var result = await tool.ExecuteAsync(sku: "SKU-2001", shipmentId: null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        GetProp<int>(result, "TotalCount").Should().Be(2);
    }

    [Fact]
    public async Task Should_ReturnShipment_When_ValidShipmentIdProvided()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetShipmentStatusTool(context, NullLogger<GetShipmentStatusTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync(sku: null, shipmentId: "SHP-001", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        GetProp<int>(result, "TotalCount").Should().Be(1);
    }

    [Fact]
    public async Task Should_ReturnError_When_NoParametersProvided()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetShipmentStatusTool(context, NullLogger<GetShipmentStatusTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync(sku: null, shipmentId: null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
        GetProp<string>(result, "Error").Should().Contain("sku");
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_NoMatchingShipments()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetShipmentStatusTool(context, NullLogger<GetShipmentStatusTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync(sku: "NONEXISTENT-SKU", shipmentId: null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var shipments = GetProp<object[]>(result, "Shipments");
        shipments.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_IdentifyDelayedShipments_When_StatusIsDelayed()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetShipmentStatusTool(context, NullLogger<GetShipmentStatusTool>.Instance);

        // Act — SKU-2001 has 1 delayed shipment (SHP-002)
        var result = await tool.ExecuteAsync(sku: "SKU-2001", shipmentId: null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        GetProp<int>(result, "DelayedCount").Should().Be(1);
    }

    [Fact]
    public async Task Should_FilterByBothSkuAndShipmentId_When_BothProvided()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetShipmentStatusTool(context, NullLogger<GetShipmentStatusTool>.Instance);

        // Act — SHP-003 is SKU-3001, not SKU-2001
        var result = await tool.ExecuteAsync(sku: "SKU-2001", shipmentId: "SHP-003", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var shipments = GetProp<object[]>(result, "Shipments");
        shipments.Should().BeEmpty(); // No match for both filters
    }

    [Fact]
    public async Task Should_ReturnError_When_BothParametersEmpty()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetShipmentStatusTool(context, NullLogger<GetShipmentStatusTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync(sku: "", shipmentId: "", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
    }

    private static T GetProp<T>(object obj, string name) =>
        (T)obj.GetType().GetProperty(name)!.GetValue(obj)!;
}
