using SquadCommerce.Api.Endpoints;
using SquadCommerce.Api.Hubs;
using SquadCommerce.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();
app.UseCors();

app.UseMiddleware<EntraIdScopeMiddleware>();

// SignalR Hub for background state updates
app.MapHub<AgentHub>("/hubs/agent");

// TODO: MapAGUI endpoint placeholder - will be implemented by agent team
// app.MapAGUI("/api/agui");

// Endpoint groups
app.MapAgentEndpoints();
app.MapPricingEndpoints();

app.Run();
