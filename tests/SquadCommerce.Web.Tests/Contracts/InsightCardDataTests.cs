using FluentAssertions;
using SquadCommerce.Contracts.A2UI;

namespace SquadCommerce.Web.Tests.Contracts;

public class InsightCardDataTests
{
    [Fact]
    public void Should_CreateRecord_When_AllFieldsProvided()
    {
        // Arrange & Act
        var card = new InsightCardData
        {
            Title = "Margin Impact",
            KeyMetric = "-12.5%",
            MetricLabel = "margin change",
            TrendDirection = "down",
            Summary = "Competitor price drop erodes margin by 12.5%.",
            ActionLabel = "Review Pricing",
            Severity = "warning"
        };

        // Assert
        card.Title.Should().Be("Margin Impact");
        card.KeyMetric.Should().Be("-12.5%");
        card.MetricLabel.Should().Be("margin change");
        card.TrendDirection.Should().Be("down");
        card.Summary.Should().Be("Competitor price drop erodes margin by 12.5%.");
        card.ActionLabel.Should().Be("Review Pricing");
        card.Severity.Should().Be("warning");
    }

    [Fact]
    public void Should_DefaultOptionalFields_When_NotProvided()
    {
        // Arrange & Act
        var card = new InsightCardData
        {
            Title = "Revenue Forecast",
            KeyMetric = "$4,200",
            MetricLabel = "revenue delta",
            TrendDirection = "up",
            Summary = "Revenue expected to increase."
        };

        // Assert
        card.ActionLabel.Should().BeNull();
        card.Severity.Should().BeNull();
    }

    [Theory]
    [InlineData("up")]
    [InlineData("down")]
    [InlineData("neutral")]
    public void Should_AcceptTrendDirectionValues_When_Set(string direction)
    {
        // Arrange & Act
        var card = new InsightCardData
        {
            Title = "Test",
            KeyMetric = "0",
            MetricLabel = "test",
            TrendDirection = direction,
            Summary = "Testing trend direction"
        };

        // Assert
        card.TrendDirection.Should().Be(direction);
    }

    [Theory]
    [InlineData("info")]
    [InlineData("warning")]
    [InlineData("critical")]
    [InlineData("success")]
    public void Should_AcceptSeverityValues_When_Set(string severity)
    {
        // Arrange & Act
        var card = new InsightCardData
        {
            Title = "Severity Test",
            KeyMetric = "42",
            MetricLabel = "count",
            TrendDirection = "neutral",
            Summary = "Testing severity level",
            Severity = severity
        };

        // Assert
        card.Severity.Should().Be(severity);
    }

    [Fact]
    public void Should_SupportValueEquality_When_ComparedAsRecords()
    {
        // Arrange
        var card1 = new InsightCardData
        {
            Title = "Test",
            KeyMetric = "100",
            MetricLabel = "units",
            TrendDirection = "up",
            Summary = "Equality test"
        };
        var card2 = new InsightCardData
        {
            Title = "Test",
            KeyMetric = "100",
            MetricLabel = "units",
            TrendDirection = "up",
            Summary = "Equality test"
        };

        // Assert
        card1.Should().Be(card2);
    }
}
