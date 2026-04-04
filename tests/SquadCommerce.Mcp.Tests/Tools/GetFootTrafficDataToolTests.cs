using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Mcp.Tools;

namespace SquadCommerce.Mcp.Tests.Tools;

public class GetFootTrafficDataToolTests
{
    [Fact]
    public async Task Should_ReturnTrafficData_When_ValidStoreIdProvided()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetFootTrafficDataTool(context, NullLogger<GetFootTrafficDataTool>.Instance);

        // Act — SEA-001 has 3 layout sections
        var result = await tool.ExecuteAsync("SEA-001", null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        GetProp<string>(result, "StoreId").Should().Be("SEA-001");
        GetProp<string>(result, "StoreName").Should().Be("Downtown Flagship");
        var sections = GetProp<object[]>(result, "Sections");
        sections.Should().HaveCount(3);
    }

    [Fact]
    public async Task Should_ReturnError_When_StoreIdMissing()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetFootTrafficDataTool(context, NullLogger<GetFootTrafficDataTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("", null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
        GetProp<string>(result, "Error").Should().Contain("storeId is required");
    }

    [Fact]
    public async Task Should_ReturnError_When_StoreIdIsNull()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetFootTrafficDataTool(context, NullLogger<GetFootTrafficDataTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync(null!, null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_NoMatchingLayoutData()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetFootTrafficDataTool(context, NullLogger<GetFootTrafficDataTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("NONEXISTENT-STORE", null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var sections = GetProp<object[]>(result, "Sections");
        sections.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_FilterBySection_When_SectionProvided()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetFootTrafficDataTool(context, NullLogger<GetFootTrafficDataTool>.Instance);

        // Act — Only 1 Electronics section in SEA-001
        var result = await tool.ExecuteAsync("SEA-001", "Electronics", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var sections = GetProp<object[]>(result, "Sections");
        sections.Should().HaveCount(1);
    }

    [Fact]
    public async Task Should_CalculateTrafficIntensity_When_DataReturned()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetFootTrafficDataTool(context, NullLogger<GetFootTrafficDataTool>.Instance);

        // Act — Grocery is max traffic (200), so Electronics (150) intensity = 150/200 = 0.75
        var result = await tool.ExecuteAsync("SEA-001", null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var sections = GetProp<object[]>(result, "Sections");

        // All traffic intensities should be between 0.0 and 1.0
        foreach (var section in sections)
        {
            var intensity = GetProp<double>(section, "TrafficIntensity");
            intensity.Should().BeInRange(0.0, 1.0);
        }
    }

    [Fact]
    public async Task Should_CalculateTotalSquareFootage_When_DataReturned()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetFootTrafficDataTool(context, NullLogger<GetFootTrafficDataTool>.Instance);

        // Act — SEA-001: 2500 + 4000 + 1800 = 8300
        var result = await tool.ExecuteAsync("SEA-001", null, CancellationToken.None);

        // Assert
        GetProp<int>(result, "TotalSquareFootage").Should().Be(8300);
    }

    [Fact]
    public async Task Should_ReturnEmptyForSection_When_SectionNotInStore()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetFootTrafficDataTool(context, NullLogger<GetFootTrafficDataTool>.Instance);

        // Act — PDX-002 has no "Grocery" section seeded
        var result = await tool.ExecuteAsync("PDX-002", "Grocery", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var sections = GetProp<object[]>(result, "Sections");
        sections.Should().BeEmpty();
    }

    private static T GetProp<T>(object obj, string name) =>
        (T)obj.GetType().GetProperty(name)!.GetValue(obj)!;
}
