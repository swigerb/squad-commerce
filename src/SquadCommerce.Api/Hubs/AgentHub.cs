using Microsoft.AspNetCore.SignalR;

namespace SquadCommerce.Api.Hubs;

public class AgentHub : Hub
{
    public async Task SendStatusUpdate(string agentName, string status)
        => await Clients.All.SendAsync("StatusUpdate", agentName, status);
    
    public async Task SendUrgencyUpdate(string level)
        => await Clients.All.SendAsync("UrgencyUpdate", level);
}
