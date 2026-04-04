using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Mcp.Tools;

namespace SquadCommerce.Mcp.Tests.Tools;

public class GetPlanogramDataToolTests
{
    [Fact]
    public async Task Should_ReturnPlanogramData_When_ValidStoreAndSection()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetPlanogramDataTool(context, NullLogger<GetPlanogramDataTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("SEA-001", "Electronics", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        GetProp<string>(result, "StoreId").Should().Be("SEA-001");
        GetProp<string>(result, "Section").Should().Be("Electronics");
        GetProp<string>(result, "StoreName").Should().Be("Downtown Flagship");
        GetProp<int>(result, "SquareFootage").Should().Be(2500);
        GetProp<int>(result, "ShelfCount").Should().Be(12);
    }

    [Fact]
    public async Task Should_ReturnError_When_StoreIdMissing()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetPlanogramDataTool(context, NullLogger<GetPlanogramDataTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("", "Electronics", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
        GetProp<string>(result, "Error").Should().Contain("required");
    }

    [Fact]
    public async Task Should_ReturnError_When_SectionMissing()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetPlanogramDataTool(context, NullLogger<GetPlanogramDataTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("SEA-001", "", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
        GetProp<string>(result, "Error").Should().Contain("required");
    }

    [Fact]
    public async Task Should_ReturnError_When_BothParametersMissing()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetPlanogramDataTool(context, NullLogger<GetPlanogramDataTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync(null!, null!, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_NoMatchingData()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetPlanogramDataTool(context, NullLogger<GetPlanogramDataTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("NONEXISTENT-STORE", "Electronics", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
        GetProp<string>(result, "Error").Should().Contain("No planogram data found");
    }

    [Fact]
    public async Task Should_CalculateTrafficIntensity_When_DataReturned()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetPlanogramDataTool(context, NullLogger<GetPlanogramDataTool>.Instance);

        // Act — Electronics (150) / max(200) = 0.75
        var result = await tool.ExecuteAsync("SEA-001", "Electronics", CancellationToken.None);

        // Assert
        GetProp<double>(result, "TrafficIntensity").Should().Be(0.75);
    }

    [Fact]
    public async Task Should_SuggestOptimization_When_PlacementNotOptimal()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetPlanogramDataTool(context, NullLogger<GetPlanogramDataTool>.Instance);

        // Act — Home section: traffic 50/200 = 0.25 -> suggested "Back", current "Back" -> Optimal
        //        Grocery: traffic 200/200 = 1.0 -> suggested "Front", current "Middle" -> not optimal
        var result = await tool.ExecuteAsync("SEA-001", "Grocery", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        GetProp<string>(result, "SuggestedPlacement").Should().Be("Front");
        GetProp<string>(result, "CurrentPlacement").Should().Be("Middle");
        GetProp<string>(result, "OptimizationStatus").Should().NotBe("Optimal");
        var suggestions = GetProp<string[]>(result, "Suggestions");
        suggestions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_ReturnOptimal_When_PlacementMatchesSuggestion()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetPlanogramDataTool(context, NullLogger<GetPlanogramDataTool>.Instance);

        // Act — Home section: traffic 50/200 = 0.25 -> suggested "Back", current "Back"
        var result = await tool.ExecuteAsync("SEA-001", "Home", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        GetProp<string>(result, "OptimizationStatus").Should().Be("Optimal");
    }

    private static T GetProp<T>(object obj, string name) =>
        (T)obj.GetType().GetProperty(name)!.GetValue(obj)!;
}
