using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Mcp.Tools;

namespace SquadCommerce.Mcp.Tests.Tools;

public class GetSocialSentimentToolTests
{
    [Fact]
    public async Task Should_ReturnSentiment_When_ValidSkuProvided()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSocialSentimentTool(context, NullLogger<GetSocialSentimentTool>.Instance);

        // Act — SKU-3001 has 3 sentiment records
        var result = await tool.ExecuteAsync(sku: "SKU-3001", platform: null, region: null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        GetProp<string>(result, "Sku").Should().Be("SKU-3001");
        var dataPoints = GetProp<object[]>(result, "DataPoints");
        dataPoints.Should().HaveCount(3);
    }

    [Fact]
    public async Task Should_FilterByPlatform_When_PlatformProvided()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSocialSentimentTool(context, NullLogger<GetSocialSentimentTool>.Instance);

        // Act — Only 1 TikTok record for SKU-3001
        var result = await tool.ExecuteAsync(sku: "SKU-3001", platform: "TikTok", region: null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var dataPoints = GetProp<object[]>(result, "DataPoints");
        dataPoints.Should().HaveCount(1);
    }

    [Fact]
    public async Task Should_FilterByRegion_When_RegionProvided()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSocialSentimentTool(context, NullLogger<GetSocialSentimentTool>.Instance);

        // Act — 2 Northeast records for SKU-3001
        var result = await tool.ExecuteAsync(sku: "SKU-3001", platform: null, region: "Northeast", CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var dataPoints = GetProp<object[]>(result, "DataPoints");
        dataPoints.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_NoMatchingSentiment()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSocialSentimentTool(context, NullLogger<GetSocialSentimentTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync(sku: "NONEXISTENT-SKU", platform: null, region: null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var dataPoints = GetProp<object[]>(result, "DataPoints");
        dataPoints.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReturnAllSentiment_When_NoFiltersProvided()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSocialSentimentTool(context, NullLogger<GetSocialSentimentTool>.Instance);

        // Act — 4 total sentiment records seeded
        var result = await tool.ExecuteAsync(sku: null, platform: null, region: null, CancellationToken.None);

        // Assert
        GetProp<bool>(result, "Success").Should().BeTrue();
        var dataPoints = GetProp<object[]>(result, "DataPoints");
        dataPoints.Should().HaveCount(4);
    }

    [Fact]
    public async Task Should_CalculateSurgingTrend_When_HighVelocity()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSocialSentimentTool(context, NullLogger<GetSocialSentimentTool>.Instance);

        // Act — SKU-3001 has avg velocity ~2.83 across all records -> "rising"
        //        but Northeast has velocity avg (4.5+3.2)/2 = 3.85 -> "surging"
        var result = await tool.ExecuteAsync(sku: "SKU-3001", platform: null, region: "Northeast", CancellationToken.None);

        // Assert
        GetProp<string>(result, "TrendDirection").Should().Be("surging");
    }

    [Fact]
    public async Task Should_CalculateDecliningTrend_When_NegativeVelocity()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSocialSentimentTool(context, NullLogger<GetSocialSentimentTool>.Instance);

        // Act — SKU-4001 has velocity -0.5 -> "declining"
        var result = await tool.ExecuteAsync(sku: "SKU-4001", platform: null, region: null, CancellationToken.None);

        // Assert
        GetProp<string>(result, "TrendDirection").Should().Be("declining");
    }

    [Fact]
    public async Task Should_IncludeAverageSentimentScore_When_DataExists()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateSeededContext();
        var tool = new GetSocialSentimentTool(context, NullLogger<GetSocialSentimentTool>.Instance);

        // Act
        var result = await tool.ExecuteAsync(sku: "SKU-3001", platform: null, region: null, CancellationToken.None);

        // Assert
        var avgScore = GetProp<double>(result, "AverageSentimentScore");
        avgScore.Should().BeInRange(0.0, 1.0);
    }

    private static T GetProp<T>(object obj, string name) =>
        (T)obj.GetType().GetProperty(name)!.GetValue(obj)!;
}
