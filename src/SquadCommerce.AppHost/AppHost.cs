var builder = DistributedApplication.CreateBuilder(args);

// API Service - Backend with SignalR and AG-UI endpoints
var api = builder.AddDotnetProject("api", Path.Combine("..", "SquadCommerce.Api", "SquadCommerce.Api.csproj"))
    .WithExternalHttpEndpoints();

// Web Frontend - Blazor application with A2UI components
var web = builder.AddDotnetProject("web", Path.Combine("..", "SquadCommerce.Web", "SquadCommerce.Web.csproj"))
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
