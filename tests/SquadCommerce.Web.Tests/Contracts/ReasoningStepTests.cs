using FluentAssertions;
using SquadCommerce.Contracts;

namespace SquadCommerce.Web.Tests.Contracts;

public class ReasoningStepTests
{
    [Fact]
    public void Should_CreateRecord_When_AllRequiredFieldsProvided()
    {
        // Arrange & Act
        var timestamp = DateTimeOffset.UtcNow;
        var step = new ReasoningStep
        {
            StepId = "step-001",
            SessionId = "session-abc",
            AgentName = "PricingAgent",
            StepType = ReasoningStepType.Thinking,
            Content = "Analyzing margin impact",
            Timestamp = timestamp,
            DurationMs = 150,
            ParentStepId = "step-000",
            Metadata = new Dictionary<string, string> { ["source"] = "A2A" }
        };

        // Assert
        step.StepId.Should().Be("step-001");
        step.SessionId.Should().Be("session-abc");
        step.AgentName.Should().Be("PricingAgent");
        step.StepType.Should().Be(ReasoningStepType.Thinking);
        step.Content.Should().Be("Analyzing margin impact");
        step.Timestamp.Should().Be(timestamp);
        step.DurationMs.Should().Be(150);
        step.ParentStepId.Should().Be("step-000");
        step.Metadata.Should().ContainKey("source").WhoseValue.Should().Be("A2A");
    }

    [Fact]
    public void Should_DefaultOptionalFields_When_NotProvided()
    {
        // Arrange & Act
        var step = new ReasoningStep
        {
            StepId = "step-002",
            SessionId = "session-xyz",
            AgentName = "InventoryAgent",
            StepType = ReasoningStepType.ToolCall,
            Content = "Querying stock levels",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Assert
        step.DurationMs.Should().Be(0);
        step.ParentStepId.Should().BeNull();
        step.Metadata.Should().BeNull();
    }

    [Theory]
    [InlineData(ReasoningStepType.Thinking)]
    [InlineData(ReasoningStepType.ToolCall)]
    [InlineData(ReasoningStepType.A2AHandshake)]
    [InlineData(ReasoningStepType.Observation)]
    [InlineData(ReasoningStepType.Decision)]
    [InlineData(ReasoningStepType.Error)]
    public void Should_AcceptAllEnumValues_When_StepTypeSet(ReasoningStepType stepType)
    {
        // Arrange & Act
        var step = new ReasoningStep
        {
            StepId = "step-enum",
            SessionId = "session-enum",
            AgentName = "TestAgent",
            StepType = stepType,
            Content = $"Testing {stepType}",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Assert
        step.StepType.Should().Be(stepType);
    }

    [Fact]
    public void Should_HaveSixEnumValues_When_ReasoningStepTypeInspected()
    {
        // Arrange & Act
        var values = Enum.GetValues<ReasoningStepType>();

        // Assert
        values.Should().HaveCount(6);
        values.Should().Contain(new[]
        {
            ReasoningStepType.Thinking,
            ReasoningStepType.ToolCall,
            ReasoningStepType.A2AHandshake,
            ReasoningStepType.Observation,
            ReasoningStepType.Decision,
            ReasoningStepType.Error
        });
    }

    [Fact]
    public void Should_SupportValueEquality_When_ComparedAsRecords()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var step1 = new ReasoningStep
        {
            StepId = "step-eq",
            SessionId = "session-eq",
            AgentName = "Agent",
            StepType = ReasoningStepType.Decision,
            Content = "Approve",
            Timestamp = timestamp
        };
        var step2 = new ReasoningStep
        {
            StepId = "step-eq",
            SessionId = "session-eq",
            AgentName = "Agent",
            StepType = ReasoningStepType.Decision,
            Content = "Approve",
            Timestamp = timestamp
        };

        // Assert
        step1.Should().Be(step2);
    }
}
