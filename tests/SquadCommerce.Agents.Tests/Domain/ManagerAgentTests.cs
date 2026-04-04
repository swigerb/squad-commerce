using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SquadCommerce.Agents.Domain;
using Xunit;

namespace SquadCommerce.Agents.Tests.Domain;

public class ManagerAgentTests
{
    [Fact]
    public void Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        var act = () => new ManagerAgent(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Should_HaveCorrectAgentName()
    {
        var agent = new ManagerAgent(NullLogger<ManagerAgent>.Instance);
        agent.AgentName.Should().Be("ManagerAgent");
    }

    [Fact]
    public async Task Should_ApproveRecommendation_When_MerchandisingResultSucceeded()
    {
        // Arrange
        var agent = new ManagerAgent(NullLogger<ManagerAgent>.Instance);
        var merchandisingResult = new AgentResult
        {
            TextSummary = "Merchandising analysis complete",
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var result = await agent.ExecuteAsync("SEA-001", "Electronics", merchandisingResult, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.TextSummary.Should().Contain("Approved");
        result.TextSummary.Should().Contain("SEA-001");
        result.TextSummary.Should().Contain("Electronics");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Should_DeferRecommendation_When_MerchandisingResultFailed()
    {
        // Arrange
        var agent = new ManagerAgent(NullLogger<ManagerAgent>.Instance);
        var merchandisingResult = new AgentResult
        {
            TextSummary = "Merchandising analysis failed",
            Success = false,
            ErrorMessage = "Layout data missing",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var result = await agent.ExecuteAsync("PDX-002", "Grocery", merchandisingResult, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.TextSummary.Should().Contain("Deferred");
        result.ErrorMessage.Should().Contain("deferred");
    }

    [Fact]
    public async Task Should_HaveTimestamp_When_ReviewCompletes()
    {
        // Arrange
        var agent = new ManagerAgent(NullLogger<ManagerAgent>.Instance);
        var merchandisingResult = new AgentResult
        {
            TextSummary = "Test result", Success = true, Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var result = await agent.ExecuteAsync("SEA-001", "Electronics", merchandisingResult, CancellationToken.None);

        // Assert
        result.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Should_SimulateReviewDelay_When_Processing()
    {
        // Arrange
        var agent = new ManagerAgent(NullLogger<ManagerAgent>.Instance);
        var merchandisingResult = new AgentResult
        {
            TextSummary = "Test", Success = true, Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await agent.ExecuteAsync("SEA-001", "Electronics", merchandisingResult, CancellationToken.None);
        stopwatch.Stop();

        // Assert — simulated delay is 500ms
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(400, "should simulate manager review delay");
    }

    [Fact]
    public async Task Should_HandleCancellation_When_TokenCancelled()
    {
        // Arrange
        var agent = new ManagerAgent(NullLogger<ManagerAgent>.Instance);
        var merchandisingResult = new AgentResult
        {
            TextSummary = "Test", Success = true, Timestamp = DateTimeOffset.UtcNow
        };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act — ManagerAgent catches exceptions, so cancellation results in an error AgentResult
        var result = await agent.ExecuteAsync("SEA-001", "Electronics", merchandisingResult, cts.Token);

        // Assert — either throws or returns failure result
        result.Success.Should().BeFalse("cancellation should prevent successful completion");
    }
}
