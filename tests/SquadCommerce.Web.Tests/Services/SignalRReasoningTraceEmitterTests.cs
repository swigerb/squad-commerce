using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using SquadCommerce.Api.Hubs;
using SquadCommerce.Api.Services;
using SquadCommerce.Contracts;

namespace SquadCommerce.Web.Tests.Services;

public class SignalRReasoningTraceEmitterTests
{
    private readonly Mock<IHubContext<AgentHub>> _hubContextMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly SignalRReasoningTraceEmitter _emitter;

    public SignalRReasoningTraceEmitterTests()
    {
        _hubContextMock = new Mock<IHubContext<AgentHub>>();
        _clientProxyMock = new Mock<IClientProxy>();

        var hubClientsMock = new Mock<IHubClients>();
        hubClientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(hubClientsMock.Object);

        var logger = Mock.Of<ILogger<SignalRReasoningTraceEmitter>>();
        _emitter = new SignalRReasoningTraceEmitter(_hubContextMock.Object, logger);
    }

    [Fact]
    public async Task Should_BroadcastReasoningStep_When_EmitStepAsyncCalled()
    {
        // Arrange
        ReasoningStep? capturedStep = null;
        _clientProxyMock
            .Setup(c => c.SendCoreAsync("ReasoningStep", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) =>
            {
                capturedStep = args[0] as ReasoningStep;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _emitter.EmitStepAsync(
            sessionId: "session-123",
            agentName: "PricingAgent",
            stepType: ReasoningStepType.Thinking,
            content: "Calculating margin impact");

        // Assert
        capturedStep.Should().NotBeNull();
        capturedStep!.SessionId.Should().Be("session-123");
        capturedStep.AgentName.Should().Be("PricingAgent");
        capturedStep.StepType.Should().Be(ReasoningStepType.Thinking);
        capturedStep.Content.Should().Be("Calculating margin impact");
    }

    [Fact]
    public async Task Should_SetCorrectFields_When_AllParametersProvided()
    {
        // Arrange
        ReasoningStep? capturedStep = null;
        _clientProxyMock
            .Setup(c => c.SendCoreAsync("ReasoningStep", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) =>
            {
                capturedStep = args[0] as ReasoningStep;
            })
            .Returns(Task.CompletedTask);

        var metadata = new Dictionary<string, string> { ["tool"] = "GetInventory" };

        // Act
        await _emitter.EmitStepAsync(
            sessionId: "session-456",
            agentName: "InventoryAgent",
            stepType: ReasoningStepType.ToolCall,
            content: "Querying stock",
            parentStepId: "parent-001",
            durationMs: 250,
            metadata: metadata);

        // Assert
        capturedStep.Should().NotBeNull();
        capturedStep!.SessionId.Should().Be("session-456");
        capturedStep.AgentName.Should().Be("InventoryAgent");
        capturedStep.StepType.Should().Be(ReasoningStepType.ToolCall);
        capturedStep.Content.Should().Be("Querying stock");
        capturedStep.ParentStepId.Should().Be("parent-001");
        capturedStep.DurationMs.Should().Be(250);
        capturedStep.Metadata.Should().ContainKey("tool").WhoseValue.Should().Be("GetInventory");
        capturedStep.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Should_GenerateStepId_When_MetadataLacksStepId()
    {
        // Arrange
        ReasoningStep? capturedStep = null;
        _clientProxyMock
            .Setup(c => c.SendCoreAsync("ReasoningStep", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) =>
            {
                capturedStep = args[0] as ReasoningStep;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _emitter.EmitStepAsync("session-x", "Agent", ReasoningStepType.Observation, "data received");

        // Assert
        capturedStep.Should().NotBeNull();
        capturedStep!.StepId.Should().NotBeNullOrEmpty();
        Guid.TryParse(capturedStep.StepId, out _).Should().BeTrue("StepId should be a valid GUID");
    }

    [Fact]
    public async Task Should_UseMetadataStepId_When_MetadataContainsStepId()
    {
        // Arrange
        ReasoningStep? capturedStep = null;
        _clientProxyMock
            .Setup(c => c.SendCoreAsync("ReasoningStep", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) =>
            {
                capturedStep = args[0] as ReasoningStep;
            })
            .Returns(Task.CompletedTask);

        var metadata = new Dictionary<string, string> { ["StepId"] = "custom-step-id" };

        // Act
        await _emitter.EmitStepAsync("session-y", "Agent", ReasoningStepType.Decision, "approved", metadata: metadata);

        // Assert
        capturedStep.Should().NotBeNull();
        capturedStep!.StepId.Should().Be("custom-step-id");
    }
}
