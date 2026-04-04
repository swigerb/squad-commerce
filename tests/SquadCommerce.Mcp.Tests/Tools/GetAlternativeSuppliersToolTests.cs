using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Mcp.Data.Entities;
using SquadCommerce.Mcp.Tools;

namespace SquadCommerce.Mcp.Tests.Tools;

public class GetAlternativeSuppliersToolTests
{
    [Fact]
    public async Task Should_ReturnCompliantSuppliers_When_ValidCategoryAndCertification()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetAlternativeSuppliersTool(context, NullLogger<GetAlternativeSuppliersTool>.Instance);

        // Act — Cocoa + FairTrade has 2 Compliant suppliers (SUP-001, SUP-004)
        var result = await tool.ExecuteAsync("Cocoa", "FairTrade", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        GetProp<string>(result, "Category").Should().Be("Cocoa");
        GetProp<string>(result, "Certification").Should().Be("FairTrade");
        var suppliers = GetProp<object[]>(result, "AlternativeSuppliers");
        suppliers.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_ReturnError_When_CategoryMissing()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetAlternativeSuppliersTool(context, NullLogger<GetAlternativeSuppliersTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("", "FairTrade", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
        GetProp<string>(result, "Error").Should().Contain("required");
    }

    [Fact]
    public async Task Should_ReturnError_When_CertificationMissing()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetAlternativeSuppliersTool(context, NullLogger<GetAlternativeSuppliersTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("Cocoa", null!, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
        GetProp<string>(result, "Error").Should().Contain("required");
    }

    [Fact]
    public async Task Should_ReturnError_When_BothParametersMissing()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetAlternativeSuppliersTool(context, NullLogger<GetAlternativeSuppliersTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("", "", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeFalse();
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_NoCompliantSuppliersFound()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetAlternativeSuppliersTool(context, NullLogger<GetAlternativeSuppliersTool>.Instance);

        // Act — No compliant "RainforestAlliance" suppliers seeded
        var result = await tool.ExecuteAsync("Cocoa", "RainforestAlliance", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var suppliers = GetProp<object[]>(result, "AlternativeSuppliers");
        suppliers.Should().BeEmpty();
        GetProp<string>(result, "Message").Should().Contain("No compliant");
    }

    [Fact]
    public async Task Should_ExcludeNonCompliantSuppliers_When_Querying()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetAlternativeSuppliersTool(context, NullLogger<GetAlternativeSuppliersTool>.Instance);

        // Act — Coffee + FairTrade: SUP-003 is NonCompliant, should be excluded
        var result = await tool.ExecuteAsync("Coffee", "FairTrade", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var suppliers = GetProp<object[]>(result, "AlternativeSuppliers");
        suppliers.Should().BeEmpty(); // SUP-003 is NonCompliant, not returned
    }

    [Fact]
    public async Task Should_IncludeDaysUntilExpiry_When_SuppliersReturned()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetAlternativeSuppliersTool(context, NullLogger<GetAlternativeSuppliersTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync("Cocoa", "FairTrade", CancellationToken.None);

        // Assert
        var suppliers = GetProp<object[]>(result, "AlternativeSuppliers");
        suppliers.Should().NotBeEmpty();
        foreach (var supplier in suppliers)
        {
            var daysUntilExpiry = GetProp<int>(supplier, "DaysUntilExpiry");
            daysUntilExpiry.Should().BeGreaterThan(0);
        }
    }

    private static T GetProp<T>(object obj, string name) =>
        (T)obj.GetType().GetProperty(name)!.GetValue(obj)!;
}
