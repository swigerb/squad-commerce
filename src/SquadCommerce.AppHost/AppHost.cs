var builder = DistributedApplication.CreateBuilder(args);

// API Service
var api = builder.AddProject<Projects.SquadCommerce_Api>("api")
    .WithExternalHttpEndpoints();

// TODO: Add Web project (Blazor frontend) - will be created by frontend team
// var web = builder.AddProject<Projects.SquadCommerce_Web>("web")
//     .WithReference(api);

// TODO: Add Agents project - will be created by agent team
// var agents = builder.AddProject<Projects.SquadCommerce_Agents>("agents")
//     .WithReference(api);

// TODO: Add MCP Server project - will be created by integration team
// var mcp = builder.AddProject<Projects.SquadCommerce_Mcp>("mcp");

// TODO: Add A2A project - will be created by integration team
// var a2a = builder.AddProject<Projects.SquadCommerce_A2A>("a2a");

builder.Build().Run();
