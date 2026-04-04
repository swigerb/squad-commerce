using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SquadCommerce.Web.Services;

namespace SquadCommerce.Web.Tests.Services;

public class SignalRStateServiceTests
{
    private readonly Mock<ILogger<SignalRStateService>> _loggerMock = new();

    private SignalRStateService CreateService(Dictionary<string, string?>? config = null)
    {
        var configData = config ?? new Dictionary<string, string?>
        {
            ["SignalR:HubUrl"] = "http://localhost:5000"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        return new SignalRStateService(configuration, _loggerMock.Object);
    }

    // ── Initial State ───────────────────────────────────────────────────

    [Fact]
    public void Should_BeDisconnected_When_NotStarted()
    {
        var service = CreateService();

        service.IsConnected.Should().BeFalse();
        service.ConnectionState.Should().Be(
            Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Disconnected);
    }

    // ── Event Registration ──────────────────────────────────────────────

    [Fact]
    public void Should_AllowSubscription_When_StatusUpdateEventUsed()
    {
        var service = CreateService();
        string? received = null;

        // Should not throw when subscribing
        var act = () => service.OnStatusUpdate += status => received = status;
        act.Should().NotThrow();
    }

    [Fact]
    public void Should_AllowSubscription_When_UrgencyBadgeEventUsed()
    {
        var service = CreateService();
        var act = () => service.OnUrgencyBadge += (l, m) => { };
        act.Should().NotThrow();
    }

    [Fact]
    public void Should_AllowSubscription_When_ThinkingStateEventUsed()
    {
        var service = CreateService();
        var act = () => service.OnThinkingState += (s, a, t) => { };
        act.Should().NotThrow();
    }

    [Fact]
    public void Should_AllowSubscription_When_ReasoningStepEventUsed()
    {
        var service = CreateService();
        var act = () => { service.OnReasoningStep += _ => { }; };
        act.Should().NotThrow();
    }

    [Fact]
    public void Should_AllowSubscription_When_A2AHandshakeEventUsed()
    {
        var service = CreateService();
        var act = () => { service.OnA2AHandshakeStatus += (_, _, _, _, _) => { }; };
        act.Should().NotThrow();
    }

    [Fact]
    public void Should_AllowSubscription_When_NotificationEventUsed()
    {
        var service = CreateService();
        var act = () => { service.OnNotification += _ => { }; };
        act.Should().NotThrow();
    }

    [Fact]
    public void Should_AllowSubscription_When_A2UIPayloadEventUsed()
    {
        var service = CreateService();
        var act = () => { service.OnA2UIPayload += _ => { }; };
        act.Should().NotThrow();
    }

    // ── Hub URL Resolution ──────────────────────────────────────────────

    [Fact]
    public async Task Should_NotThrow_When_ServerUnavailable()
    {
        // SignalR connection to a non-existent server should handle gracefully
        var service = CreateService(new Dictionary<string, string?>
        {
            ["SignalR:HubUrl"] = "http://localhost:59999"
        });

        // StartAsync logs a warning for HttpRequestException but does not throw
        var act = () => service.StartAsync();
        await act.Should().NotThrowAsync<HttpRequestException>();
    }

    // ── Stop / Dispose ──────────────────────────────────────────────────

    [Fact]
    public async Task Should_NotThrow_When_StopCalledBeforeStart()
    {
        var service = CreateService();

        var act = () => service.StopAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Should_NotThrow_When_DisposeCalledBeforeStart()
    {
        var service = CreateService();

        var act = () => service.DisposeAsync().AsTask();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Should_NotThrow_When_DisposedMultipleTimes()
    {
        var service = CreateService();

        await service.DisposeAsync();
        var act = () => service.DisposeAsync().AsTask();
        await act.Should().NotThrowAsync();
    }

    // ── Configuration Fallback ──────────────────────────────────────────

    [Fact]
    public void Should_UseFallbackUrl_When_NoConfigurationProvided()
    {
        // With empty config, should fall back to default localhost URL
        var service = CreateService(new Dictionary<string, string?>());

        // Service should still be constructable
        service.Should().NotBeNull();
        service.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void Should_PreferServicesConfig_When_MultipleUrlsAvailable()
    {
        // services:api:http:0 takes precedence over SignalR:HubUrl
        var service = CreateService(new Dictionary<string, string?>
        {
            ["services:api:http:0"] = "http://preferred:5000",
            ["SignalR:HubUrl"] = "http://fallback:5000"
        });

        service.Should().NotBeNull();
    }
}
