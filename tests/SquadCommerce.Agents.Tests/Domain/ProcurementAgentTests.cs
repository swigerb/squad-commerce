using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Mcp.Data.Entities;
using Xunit;

namespace SquadCommerce.Agents.Tests.Domain;

public class ProcurementAgentTests
{
    [Fact]
    public void Should_ThrowArgumentNullException_When_DbContextIsNull()
    {
        var act = () => new ProcurementAgent(null!, NullLogger<ProcurementAgent>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContext");
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        var act = () => new ProcurementAgent(CreateEmptyDbContext(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Should_HaveCorrectAgentName()
    {
        var agent = new ProcurementAgent(CreateEmptyDbContext(), NullLogger<ProcurementAgent>.Instance);
        agent.AgentName.Should().Be("ProcurementAgent");
    }

    [Fact]
    public async Task Should_FindAlternativeSuppliers_When_NonCompliantExist()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new ProcurementAgent(dbContext, NullLogger<ProcurementAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("Cocoa", "FairTrade", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.TextSummary.Should().Contain("need replacement");
        result.TextSummary.Should().Contain("alternative(s) available");
    }

    [Fact]
    public async Task Should_ReportFullCompliance_When_AllSuppliersCompliant()
    {
        // Arrange
        var dbContext = CreateFullyCompliantDbContext();
        var agent = new ProcurementAgent(dbContext, NullLogger<ProcurementAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("Coffee", "Organic", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.TextSummary.Should().Contain("fully compliant");
    }

    [Fact]
    public async Task Should_ListReplacementRecommendations_When_NonCompliantFound()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new ProcurementAgent(dbContext, NullLogger<ProcurementAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("Cocoa", "FairTrade", CancellationToken.None);

        // Assert
        result.TextSummary.Should().Contain("Replace");
        result.TextSummary.Should().Contain("Alternatives:");
    }

    [Fact]
    public async Task Should_IncludeSupplierCountInSummary_When_AnalyzingCategory()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new ProcurementAgent(dbContext, NullLogger<ProcurementAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("Cocoa", "FairTrade", CancellationToken.None);

        // Assert
        result.TextSummary.Should().Contain("Procurement for Cocoa (FairTrade)");
        result.TextSummary.Should().Contain("supplier(s) need replacement");
    }

    [Fact]
    public async Task Should_HaveTimestamp_When_ResultReturned()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new ProcurementAgent(dbContext, NullLogger<ProcurementAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("Cocoa", "FairTrade", CancellationToken.None);

        // Assert
        result.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    private static SquadCommerceDbContext CreateEmptyDbContext()
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"ProcurementTest_{Guid.NewGuid()}")
            .Options;
        return new SquadCommerceDbContext(options);
    }

    private static SquadCommerceDbContext CreateSeededDbContext()
    {
        var dbContext = CreateEmptyDbContext();

        dbContext.Suppliers.AddRange(
            new SupplierEntity
            {
                SupplierId = "SUP-001", Name = "GoodCocoa Ltd", Category = "Cocoa", Country = "Ghana",
                Certification = "FairTrade", CertificationExpiry = DateTimeOffset.UtcNow.AddYears(1),
                Status = "Compliant"
            },
            new SupplierEntity
            {
                SupplierId = "SUP-002", Name = "BadCocoa Inc", Category = "Cocoa", Country = "Nigeria",
                Certification = "None", CertificationExpiry = DateTimeOffset.UtcNow.AddDays(-30),
                Status = "NonCompliant", WatchlistNotes = "No certification"
            },
            new SupplierEntity
            {
                SupplierId = "SUP-003", Name = "RiskyCocoa Corp", Category = "Cocoa", Country = "Brazil",
                Certification = "FairTrade", CertificationExpiry = DateTimeOffset.UtcNow.AddDays(15),
                Status = "AtRisk", WatchlistNotes = "Cert expiring soon"
            });

        dbContext.SaveChanges();
        return dbContext;
    }

    private static SquadCommerceDbContext CreateFullyCompliantDbContext()
    {
        var dbContext = CreateEmptyDbContext();

        dbContext.Suppliers.AddRange(
            new SupplierEntity
            {
                SupplierId = "SUP-010", Name = "CoffeeCo A", Category = "Coffee", Country = "Colombia",
                Certification = "Organic", CertificationExpiry = DateTimeOffset.UtcNow.AddYears(2),
                Status = "Compliant"
            },
            new SupplierEntity
            {
                SupplierId = "SUP-011", Name = "CoffeeCo B", Category = "Coffee", Country = "Ethiopia",
                Certification = "Organic", CertificationExpiry = DateTimeOffset.UtcNow.AddYears(1),
                Status = "Compliant"
            });

        dbContext.SaveChanges();
        return dbContext;
    }
}
