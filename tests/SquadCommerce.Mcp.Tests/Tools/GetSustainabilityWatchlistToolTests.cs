using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Mcp.Data.Entities;
using SquadCommerce.Mcp.Tools;

namespace SquadCommerce.Mcp.Tests.Tools;

public class GetSustainabilityWatchlistToolTests
{
    [Fact]
    public async Task Should_ReturnFlaggedSuppliers_When_NoFilter()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSustainabilityWatchlistTool(context, NullLogger<GetSustainabilityWatchlistTool>.Instance);

        // Act — 2 flagged suppliers: SUP-002 (AtRisk), SUP-003 (NonCompliant)
        var result = await tool.ExecuteAsync(category: null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var flagged = GetProp<object[]>(result, "FlaggedSuppliers");
        flagged.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_FilterByCategory_When_CategoryProvided()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSustainabilityWatchlistTool(context, NullLogger<GetSustainabilityWatchlistTool>.Instance);

        // Act — Both flagged suppliers (SUP-002, SUP-003) are Coffee
        var result = await tool.ExecuteAsync(category: "Coffee", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var flagged = GetProp<object[]>(result, "FlaggedSuppliers");
        flagged.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_NoFlaggedSuppliers()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSustainabilityWatchlistTool(context, NullLogger<GetSustainabilityWatchlistTool>.Instance);

        // Act — No flagged Cocoa suppliers (all Cocoa suppliers are Compliant)
        var result = await tool.ExecuteAsync(category: "Cocoa", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var flagged = GetProp<object[]>(result, "FlaggedSuppliers");
        flagged.Should().BeEmpty();
        GetProp<string>(result, "Message").Should().Contain("No suppliers currently flagged");
    }

    [Fact]
    public async Task Should_ExcludeCompliantSuppliers_When_Querying()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSustainabilityWatchlistTool(context, NullLogger<GetSustainabilityWatchlistTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync(category: null, CancellationToken.None);

        // Assert
        var flagged = GetProp<object[]>(result, "FlaggedSuppliers");
        foreach (var supplier in flagged)
        {
            var status = GetProp<string>(supplier, "Status");
            status.Should().BeOneOf("AtRisk", "NonCompliant");
        }
    }

    [Fact]
    public async Task Should_IncludeRiskBreakdown_When_FlaggedSuppliersReturned()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSustainabilityWatchlistTool(context, NullLogger<GetSustainabilityWatchlistTool>.Instance);

        // Act — 1 AtRisk + 1 NonCompliant
        var result = await tool.ExecuteAsync(category: null, CancellationToken.None);

        // Assert
        GetProp<int>(result, "TotalAtRisk").Should().Be(1);
        GetProp<int>(result, "TotalNonCompliant").Should().Be(1);
    }

    [Fact]
    public async Task Should_IncludeWatchlistNotes_When_FlaggedSuppliersReturned()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSustainabilityWatchlistTool(context, NullLogger<GetSustainabilityWatchlistTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync(category: null, CancellationToken.None);

        // Assert
        var flagged = GetProp<object[]>(result, "FlaggedSuppliers");
        flagged.Should().Contain(s => GetProp<string?>(s, "WatchlistNotes") != null);
    }

    [Fact]
    public async Task Should_ReturnEmptyForNonexistentCategory_When_CategoryProvided()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSustainabilityWatchlistTool(context, NullLogger<GetSustainabilityWatchlistTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync(category: "NonexistentCategory", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var flagged = GetProp<object[]>(result, "FlaggedSuppliers");
        flagged.Should().BeEmpty();
    }

    private static T GetProp<T>(object obj, string name) =>
        (T)obj.GetType().GetProperty(name)!.GetValue(obj)!;
}
