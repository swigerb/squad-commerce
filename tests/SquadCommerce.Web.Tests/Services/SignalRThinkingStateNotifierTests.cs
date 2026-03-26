using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using SquadCommerce.Api.Hubs;
using SquadCommerce.Api.Services;

namespace SquadCommerce.Web.Tests.Services;

public class SignalRThinkingStateNotifierTests
{
    private readonly Mock<IHubContext<AgentHub>> _hubContextMock;
    private readonly Mock<IClientProxy> _groupProxyMock;
    private readonly SignalRThinkingStateNotifier _notifier;

    public SignalRThinkingStateNotifierTests()
    {
        _hubContextMock = new Mock<IHubContext<AgentHub>>();
        _groupProxyMock = new Mock<IClientProxy>();

        var hubClientsMock = new Mock<IHubClients>();
        hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_groupProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(hubClientsMock.Object);

        var logger = Mock.Of<ILogger<SignalRThinkingStateNotifier>>();
        _notifier = new SignalRThinkingStateNotifier(_hubContextMock.Object, logger);
    }

    [Fact]
    public async Task Should_BroadcastToSessionGroup_When_ThinkingStateTrue()
    {
        // Act
        await _notifier.SendThinkingStateAsync("session-abc", "PricingAgent", true);

        // Assert
        _hubContextMock.Verify(h => h.Clients.Group("session-abc"), Times.Once);
        _groupProxyMock.Verify(
            c => c.SendCoreAsync(
                "ThinkingState",
                It.Is<object?[]>(args =>
                    args.Length == 3 &&
                    (string)args[0]! == "session-abc" &&
                    (string)args[1]! == "PricingAgent" &&
                    (bool)args[2]! == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_BroadcastToSessionGroup_When_ThinkingStateFalse()
    {
        // Act
        await _notifier.SendThinkingStateAsync("session-xyz", "InventoryAgent", false);

        // Assert
        _hubContextMock.Verify(h => h.Clients.Group("session-xyz"), Times.Once);
        _groupProxyMock.Verify(
            c => c.SendCoreAsync(
                "ThinkingState",
                It.Is<object?[]>(args =>
                    args.Length == 3 &&
                    (string)args[0]! == "session-xyz" &&
                    (string)args[1]! == "InventoryAgent" &&
                    (bool)args[2]! == false),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_UseCorrectGroupName_When_DifferentSessionIds()
    {
        // Act
        await _notifier.SendThinkingStateAsync("session-1", "Agent1", true);
        await _notifier.SendThinkingStateAsync("session-2", "Agent2", false);

        // Assert
        _hubContextMock.Verify(h => h.Clients.Group("session-1"), Times.Once);
        _hubContextMock.Verify(h => h.Clients.Group("session-2"), Times.Once);
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_HubContextIsNull()
    {
        // Act
        var act = () => new SignalRThinkingStateNotifier(null!, Mock.Of<ILogger<SignalRThinkingStateNotifier>>());

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("hubContext");
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        // Act
        var act = () => new SignalRThinkingStateNotifier(Mock.Of<IHubContext<AgentHub>>(), null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}
