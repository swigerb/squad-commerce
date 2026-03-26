using Microsoft.AspNetCore.SignalR.Client;
using SquadCommerce.Contracts;

namespace SquadCommerce.Web.Services;

public class SignalRStateService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SignalRStateService> _logger;

    public event Action<string>? OnStatusUpdate;
    public event Action<string, string>? OnUrgencyBadge;
    public event Action<object>? OnA2UIPayload;
    public event Action<string>? OnNotification;
    public event Action<string, string, bool>? OnThinkingState;
    public event Action<ReasoningStep>? OnReasoningStep;
    public event Action<string, string, string, string, string>? OnA2AHandshakeStatus;

    public bool IsConnected=> _hubConnection?.State == HubConnectionState.Connected;
    public HubConnectionState ConnectionState => _hubConnection?.State ?? HubConnectionState.Disconnected;

    public SignalRStateService(IConfiguration configuration, ILogger<SignalRStateService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        if (_hubConnection != null)
        {
            _logger.LogInformation("SignalR connection already exists. State: {State}", _hubConnection.State);
            
            if (_hubConnection.State == HubConnectionState.Connected)
                return;
            
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    _logger.LogInformation("SignalR connection restarted successfully");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to restart existing SignalR connection");
                }
            }
        }

        var hubUrl = _configuration["services:api:http:0"] ?? _configuration["services:api:https:0"] ?? _configuration["SignalR:HubUrl"] ?? "http://localhost:5000";
        hubUrl = hubUrl.TrimEnd('/') + "/hubs/agent";
        _logger.LogInformation("Creating SignalR connection to {HubUrl}", hubUrl);

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.SkipNegotiation = false;
            })
            .WithAutomaticReconnect(new[] 
            { 
                TimeSpan.Zero, 
                TimeSpan.FromSeconds(2), 
                TimeSpan.FromSeconds(5), 
                TimeSpan.FromSeconds(10) 
            })
            .Build();

        _hubConnection.Reconnecting += error =>
        {
            _logger.LogWarning("SignalR connection lost. Reconnecting... Error: {Error}", error?.Message);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _logger.LogInformation("SignalR reconnected. ConnectionId: {ConnectionId}", connectionId);
            return Task.CompletedTask;
        };

        _hubConnection.Closed += error =>
        {
            _logger.LogError(error, "SignalR connection closed");
            return Task.CompletedTask;
        };

        // Register event handlers
        _hubConnection.On<string>("StatusUpdate", (status) =>
        {
            _logger.LogDebug("Received StatusUpdate: {Status}", status);
            OnStatusUpdate?.Invoke(status);
        });

        _hubConnection.On<string, string>("UrgencyUpdate", (level, message) =>
        {
            _logger.LogDebug("Received UrgencyUpdate: {Level} - {Message}", level, message);
            OnUrgencyBadge?.Invoke(level, message);
        });

        _hubConnection.On<object>("A2UIPayload", (payload) =>
        {
            _logger.LogDebug("Received A2UIPayload");
            OnA2UIPayload?.Invoke(payload);
        });

        _hubConnection.On<string>("Notification", (message) =>
        {
            _logger.LogDebug("Received Notification: {Message}", message);
            OnNotification?.Invoke(message);
        });

        _hubConnection.On<string, string, bool>("ThinkingState", (sessionId, agentName, isThinking) =>
        {
            _logger.LogDebug("Received ThinkingState: Agent={AgentName}, IsThinking={IsThinking}, Session={SessionId}", agentName, isThinking, sessionId);
            OnThinkingState?.Invoke(sessionId, agentName, isThinking);
        });

        _hubConnection.On<ReasoningStep>("ReasoningStep", (step) =>
        {
            _logger.LogDebug("Received ReasoningStep: StepId={StepId}, Agent={AgentName}, Type={StepType}, Session={SessionId}",
                step.StepId, step.AgentName, step.StepType, step.SessionId);
            OnReasoningStep?.Invoke(step);
        });

        _hubConnection.On<string, string, string, string, string>("A2AHandshakeStatus", (sessionId, sourceAgent, targetAgent, status, details) =>
        {
            _logger.LogDebug("Received A2AHandshakeStatus: Source={SourceAgent}, Target={TargetAgent}, Status={Status}, Session={SessionId}",
                sourceAgent, targetAgent, status, sessionId);
            OnA2AHandshakeStatus?.Invoke(sessionId, sourceAgent, targetAgent, status, details);
        });

        try
        {
            await _hubConnection.StartAsync();
            _logger.LogInformation("SignalR connection started successfully. ConnectionId: {ConnectionId}", 
                _hubConnection.ConnectionId);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "SignalR connection failed (server may not be running). Service will continue without real-time updates.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SignalR connection");
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection != null)
        {
            _logger.LogInformation("Stopping SignalR connection");
            
            try
            {
                await _hubConnection.StopAsync();
                _logger.LogInformation("SignalR connection stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping SignalR connection");
            }
            
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
