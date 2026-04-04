using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Mcp.Data.Entities;
using Xunit;

namespace SquadCommerce.Agents.Tests.Domain;

public class ResearchAgentTests
{
    [Fact]
    public void Should_ThrowArgumentNullException_When_DbContextIsNull()
    {
        var act = () => new ResearchAgent(null!, NullLogger<ResearchAgent>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContext");
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        var act = () => new ResearchAgent(CreateEmptyDbContext(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Should_HaveCorrectAgentName()
    {
        var agent = new ResearchAgent(CreateEmptyDbContext(), NullLogger<ResearchAgent>.Instance);
        agent.AgentName.Should().Be("ResearchAgent");
    }

    [Fact]
    public async Task Should_FlagNonCompliantSuppliers_When_WatchlistChecked()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new ResearchAgent(dbContext, NullLogger<ResearchAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("Cocoa", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.TextSummary.Should().Contain("flagged");
        result.TextSummary.Should().Contain("non-compliant");
    }

    [Fact]
    public async Task Should_ReportCompliance_When_AllSuppliersClean()
    {
        // Arrange
        var dbContext = CreateCleanDbContext();
        var agent = new ResearchAgent(dbContext, NullLogger<ResearchAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("Coffee", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.TextSummary.Should().Contain("compliant");
    }

    [Fact]
    public async Task Should_IncludeConfidenceLevels_When_ReportingFindings()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new ResearchAgent(dbContext, NullLogger<ResearchAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("Cocoa", CancellationToken.None);

        // Assert
        // NonCompliant should have high confidence, AtRisk should have medium
        result.TextSummary.Should().Contain("high confidence");
        result.TextSummary.Should().Contain("medium confidence");
    }

    [Fact]
    public async Task Should_IncludeWatchlistNotes_When_Available()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new ResearchAgent(dbContext, NullLogger<ResearchAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("Cocoa", CancellationToken.None);

        // Assert
        result.TextSummary.Should().Contain("Deforestation");
    }

    [Fact]
    public async Task Should_HaveTimestamp_When_ResultReturned()
    {
        // Arrange
        var dbContext = CreateSeededDbContext();
        var agent = new ResearchAgent(dbContext, NullLogger<ResearchAgent>.Instance);

        // Act
        var result = await agent.ExecuteAsync("Cocoa", CancellationToken.None);

        // Assert
        result.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    private static SquadCommerceDbContext CreateEmptyDbContext()
    {
        var options = new DbContextOptionsBuilder<SquadCommerceDbContext>()
            .UseInMemoryDatabase($"ResearchTest_{Guid.NewGuid()}")
            .Options;
        return new SquadCommerceDbContext(options);
    }

    private static SquadCommerceDbContext CreateSeededDbContext()
    {
        var dbContext = CreateEmptyDbContext();

        dbContext.Suppliers.AddRange(
            new SupplierEntity
            {
                SupplierId = "SUP-001", Name = "CocoaGood", Category = "Cocoa", Country = "Ghana",
                Certification = "FairTrade", CertificationExpiry = DateTimeOffset.UtcNow.AddYears(1),
                Status = "Compliant"
            },
            new SupplierEntity
            {
                SupplierId = "SUP-002", Name = "CocoaBad", Category = "Cocoa", Country = "Ivory Coast",
                Certification = "None", CertificationExpiry = DateTimeOffset.UtcNow.AddDays(-60),
                Status = "NonCompliant", WatchlistNotes = "Deforestation concerns"
            },
            new SupplierEntity
            {
                SupplierId = "SUP-003", Name = "CocoaRisky", Category = "Cocoa", Country = "Ecuador",
                Certification = "FairTrade", CertificationExpiry = DateTimeOffset.UtcNow.AddDays(20),
                Status = "AtRisk", WatchlistNotes = "Cert renewal pending"
            });

        dbContext.SaveChanges();
        return dbContext;
    }

    private static SquadCommerceDbContext CreateCleanDbContext()
    {
        var dbContext = CreateEmptyDbContext();

        dbContext.Suppliers.Add(new SupplierEntity
        {
            SupplierId = "SUP-010", Name = "PureCoffee", Category = "Coffee", Country = "Colombia",
            Certification = "Organic", CertificationExpiry = DateTimeOffset.UtcNow.AddYears(2),
            Status = "Compliant"
        });

        dbContext.SaveChanges();
        return dbContext;
    }
}
