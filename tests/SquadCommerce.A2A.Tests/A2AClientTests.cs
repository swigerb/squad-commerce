using Xunit;
using FluentAssertions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.A2A;
using SquadCommerce.Contracts.Models;

namespace SquadCommerce.A2A.Tests;

public class A2AClientTests
{
    [Fact]
    public async Task Should_QueryCompetitorPricing_When_ValidSkuProvided()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHandler.Object);
        var client = new A2AClient(httpClient, NullLogger<A2AClient>.Instance);

        // Act
        var result = await client.GetCompetitorPricingAsync("SKU-1001", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().AllSatisfy(p =>
        {
            p.Sku.Should().Be("SKU-1001");
            p.Price.Should().BeGreaterThan(0);
            p.CompetitorName.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public async Task Should_ValidateExternalData_When_CompetitorDataProvided()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHandler.Object);
        var client = new A2AClient(httpClient, NullLogger<A2AClient>.Instance);

        var competitorData = new CompetitorPricing
        {
            Sku = "SKU-1001",
            CompetitorName = "TechMart",
            Price = 24.99m,
            Source = "A2A",
            Verified = false,
            LastUpdated = DateTimeOffset.UtcNow
        };

        // Act
        var result = await client.ValidateExternalDataAsync(competitorData, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Should_ReturnMultipleCompetitors_When_QueryingCompetitorPricing()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHandler.Object);
        var client = new A2AClient(httpClient, NullLogger<A2AClient>.Instance);

        // Act
        var result = await client.GetCompetitorPricingAsync("SKU-1002", CancellationToken.None);

        // Assert
        result.Should().HaveCountGreaterThan(1);
        result.Select(c => c.CompetitorName).Distinct().Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task Should_IncludeVerifiedFlag_When_ReturningCompetitorPricing()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHandler.Object);
        var client = new A2AClient(httpClient, NullLogger<A2AClient>.Instance);

        // Act
        var result = await client.GetCompetitorPricingAsync("SKU-1003", CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(p => p.Verified.Should().BeTrue());
    }

    [Fact]
    public async Task Should_RejectInvalidPrice_When_ValidatingExternalData()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHandler.Object);
        var client = new A2AClient(httpClient, NullLogger<A2AClient>.Instance);

        var invalidData = new CompetitorPricing
        {
            Sku = "SKU-1001",
            CompetitorName = "BadCompetitor",
            Price = -5.00m, // Invalid negative price
            Source = "A2A",
            Verified = false,
            LastUpdated = DateTimeOffset.UtcNow
        };

        // Act
        var result = await client.ValidateExternalDataAsync(invalidData, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }
}

