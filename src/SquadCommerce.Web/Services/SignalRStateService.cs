using Microsoft.AspNetCore.SignalR.Client;

namespace SquadCommerce.Web.Services;

public class SignalRStateService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly IConfiguration _configuration;

    public event Action<string>? OnStatusUpdate;
    public event Action<string, string>? OnUrgencyBadge;

    public SignalRStateService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task StartAsync()
    {
        if (_hubConnection != null)
            return;

        var hubUrl = _configuration["SignalR:HubUrl"] ?? "/hubs/state";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string>("StatusUpdate", (status) =>
        {
            OnStatusUpdate?.Invoke(status);
        });

        _hubConnection.On<string, string>("UrgencyBadge", (level, message) =>
        {
            OnUrgencyBadge?.Invoke(level, message);
        });

        try
        {
            await _hubConnection.StartAsync();
        }
        catch (Exception)
        {
            // SignalR connection failed - service will attempt automatic reconnect
            // This is non-fatal for the application
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        GC.SuppressFinalize(this);
    }
}
