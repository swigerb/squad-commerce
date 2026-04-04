using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Mcp.Tools;

namespace SquadCommerce.Mcp.Tests.Tools;

public class GetSupplierCertificationsToolTests
{
    [Fact]
    public async Task Should_ReturnAllSuppliers_When_NoFilters()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSupplierCertificationsTool(context, NullLogger<GetSupplierCertificationsTool>.Instance);

        // Act — 5 total suppliers seeded
        var result = await tool.ExecuteAsync(category: null, certification: null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var suppliers = GetProp<object[]>(result, "Suppliers");
        suppliers.Should().HaveCount(5);
    }

    [Fact]
    public async Task Should_FilterByCategory_When_CategoryProvided()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSupplierCertificationsTool(context, NullLogger<GetSupplierCertificationsTool>.Instance);

        // Act — 2 Cocoa suppliers (SUP-001, SUP-004)
        var result = await tool.ExecuteAsync(category: "Cocoa", certification: null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var suppliers = GetProp<object[]>(result, "Suppliers");
        suppliers.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_FilterByCertification_When_CertificationProvided()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSupplierCertificationsTool(context, NullLogger<GetSupplierCertificationsTool>.Instance);

        // Act — 2 Organic suppliers (SUP-002, SUP-005)
        var result = await tool.ExecuteAsync(category: null, certification: "Organic", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var suppliers = GetProp<object[]>(result, "Suppliers");
        suppliers.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_FilterByCategoryAndCertification_When_BothProvided()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSupplierCertificationsTool(context, NullLogger<GetSupplierCertificationsTool>.Instance);

        // Act — 2 Cocoa + FairTrade suppliers
        var result = await tool.ExecuteAsync(category: "Cocoa", certification: "FairTrade", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var suppliers = GetProp<object[]>(result, "Suppliers");
        suppliers.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_NoMatchingSuppliers()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSupplierCertificationsTool(context, NullLogger<GetSupplierCertificationsTool>.Instance);

        // Act — No "Electronics" category suppliers
        var result = await tool.ExecuteAsync(category: "Electronics", certification: null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var suppliers = GetProp<object[]>(result, "Suppliers");
        suppliers.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_IncludeComplianceBreakdown_When_SuppliersReturned()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSupplierCertificationsTool(context, NullLogger<GetSupplierCertificationsTool>.Instance);

        // Act — All 5 suppliers: 3 Compliant, 1 AtRisk, 1 NonCompliant
        var result = await tool.ExecuteAsync(category: null, certification: null, CancellationToken.None);

        // Assert
        GetProp<int>(result, "TotalCompliant").Should().Be(3);
        GetProp<int>(result, "TotalAtRisk").Should().Be(1);
        GetProp<int>(result, "TotalNonCompliant").Should().Be(1);
    }

    [Fact]
    public async Task Should_IncludeDaysUntilExpiry_When_SuppliersReturned()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSupplierCertificationsTool(context, NullLogger<GetSupplierCertificationsTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync(category: null, certification: null, CancellationToken.None);

        // Assert
        var suppliers = GetProp<object[]>(result, "Suppliers");
        foreach (var supplier in suppliers)
        {
            // DaysUntilExpiry property should exist on every supplier result
            supplier.GetType().GetProperty("DaysUntilExpiry").Should().NotBeNull();
        }
    }

    private static T GetProp<T>(object obj, string name) =>
        (T)obj.GetType().GetProperty(name)!.GetValue(obj)!;
}
