using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Mcp.Data.Entities;
using Xunit;

namespace SquadCommerce.Agents.Tests.Domain;

public class ComplianceAgentTests
{
    [Fact]
    public void Should_ThrowArgumentNullException_When_DbContextIsNull()
    {
        var act = () => new ComplianceAgent(null!, NullLogger<ComplianceAgent>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContext");
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        var dbContext = CreateSeededDbContext();
        var act = () => new ComplianceAgent(dbContext, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Should_HaveCorrectAgentName()
    {
        var agent = new ComplianceAgent(CreateSeededDbContext(), NullLogger<ComplianceAgent>.Instance);
        agent.AgentName.Should().Be("ComplianceAgent");
    }

    [Fact]
    public async Task Should_ReturnSupplierRiskMatrix_When_SuppliersExistForCategory()
    {
        // Arrange
        var dbContext = CreateSeededDbContext("Cocoa");
        var agent = new ComplianceAgent(dbContext, NullLogger<ComplianceAgent>.Instance);
        var deadline = DateTimeOffset.UtcNow.AddDays(30);

        // Act
        var result = await agent.ExecuteAsync("Cocoa", "FairTrade", deadline, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.A2UIPayload.Should().BeOfType<SupplierRiskMatrixData>();
        var matrix = (SupplierRiskMatrixData)result.A2UIPayload!;
        matrix.ProductCategory.Should().Be("Cocoa");
        matrix.CertificationRequired.Should().Be("FairTrade");
        matrix.Deadline.Should().Be(deadline);
        matrix.Suppliers.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_CountCompliantAndAtRiskSuppliers_When_Analyzing()
    {
        // Arrange
        var dbContext = CreateSeededDbContext("Cocoa");
        var agent = new ComplianceAgent(dbContext, NullLogger<ComplianceAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("Cocoa", "FairTrade", DateTimeOffset.UtcNow.AddDays(30), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var matrix = (SupplierRiskMatrixData)result.A2UIPayload!;
        (matrix.TotalCompliant + matrix.TotalAtRisk + matrix.TotalNonCompliant).Should().Be(matrix.Suppliers.Count);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_NoCategoryMatch()
    {
        // Arrange
        var dbContext = CreateSeededDbContext("Cocoa");
        var agent = new ComplianceAgent(dbContext, NullLogger<ComplianceAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("NonExistentCategory", "FairTrade",
            DateTimeOffset.UtcNow.AddDays(30), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("NonExistentCategory");
        result.A2UIPayload.Should().BeNull();
    }

    [Fact]
    public async Task Should_IncludeTextSummary_When_AnalysisCompletes()
    {
        // Arrange
        var dbContext = CreateSeededDbContext("Coffee");
        var agent = new ComplianceAgent(dbContext, NullLogger<ComplianceAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("Coffee", "Organic",
            DateTimeOffset.UtcNow.AddDays(60), CancellationToken.None);

        // Assert
        result.TextSummary.Should().Contain("Coffee");
        result.TextSummary.Should().Contain("Organic");
        result.TextSummary.Should().Contain("compliant");
    }

    [Fact]
    public async Task Should_HaveTimestamp_When_ResultReturned()
    {
        // Arrange
        var dbContext = CreateSeededDbContext("Cocoa");
        var agent = new ComplianceAgent(dbContext, NullLogger<ComplianceAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("Cocoa", "FairTrade",
            DateTimeOffset.UtcNow.AddDays(30), CancellationToken.None);

        // Assert
        result.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    private static SquadCommerceDbContext CreateSeededDbContext(string category = "Cocoa")
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"ComplianceTest_{Guid.NewGuid()}")
            .Options;
        var dbContext = new SquadCommerceDbContext(options);

        dbContext.Suppliers.AddRange(
            new SupplierEntity
            {
                SupplierId = "SUP-001", Name = "CocoaFarm A", Category = category, Country = "Ghana",
                Certification = "FairTrade", CertificationExpiry = DateTimeOffset.UtcNow.AddYears(1),
                Status = "Compliant", WatchlistNotes = null
            },
            new SupplierEntity
            {
                SupplierId = "SUP-002", Name = "CocoaFarm B", Category = category, Country = "Ivory Coast",
                Certification = "None", CertificationExpiry = DateTimeOffset.UtcNow.AddDays(-30),
                Status = "NonCompliant", WatchlistNotes = "Deforestation concerns"
            },
            new SupplierEntity
            {
                SupplierId = "SUP-003", Name = "CocoaFarm C", Category = category, Country = "Ecuador",
                Certification = "FairTrade", CertificationExpiry = DateTimeOffset.UtcNow.AddDays(45),
                Status = "AtRisk", WatchlistNotes = "Certification expiring soon"
            });

        if (category != "Coffee")
        {
            dbContext.Suppliers.Add(new SupplierEntity
            {
                SupplierId = "SUP-010", Name = "CoffeeCo A", Category = "Coffee", Country = "Colombia",
                Certification = "Organic", CertificationExpiry = DateTimeOffset.UtcNow.AddYears(2),
                Status = "Compliant", WatchlistNotes = null
            });
        }
        else
        {
            dbContext.Suppliers.Add(new SupplierEntity
            {
                SupplierId = "SUP-010", Name = "CoffeeCo A", Category = "Coffee", Country = "Colombia",
                Certification = "Organic", CertificationExpiry = DateTimeOffset.UtcNow.AddYears(2),
                Status = "Compliant", WatchlistNotes = null
            });
        }

        dbContext.SaveChanges();
        return dbContext;
    }
}
