var builder = DistributedApplication.CreateBuilder(args);

// API Service - Backend with SignalR and AG-UI endpoints
var api = builder.AddProject<Projects.SquadCommerce_Api>("api")
    .WithExternalHttpEndpoints();

// Web Frontend - Blazor application with A2UI components
var web = builder.AddProject<Projects.SquadCommerce_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
