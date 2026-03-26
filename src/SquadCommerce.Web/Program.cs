using SquadCommerce.Web.Components;
using SquadCommerce.Web.Services;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register Fluent UI Blazor components
builder.Services.AddFluentUIComponents();

// Register HTTP client for AG-UI streaming
builder.Services.AddHttpClient<AgUiStreamService>(client =>
{
    // In Azure Container Apps, service discovery is via environment variables
    // Format: services__<service-name>__https__0 or services__<service-name>__http__0
    var apiBaseUrl = builder.Configuration["services:api:https:0"] 
                     ?? builder.Configuration["services:api:http:0"]
                     ?? builder.Configuration["Api:BaseUrl"] 
                     ?? "http://localhost:5000";
    
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5); // Long timeout for streaming
});

// Register SignalR state service
builder.Services.AddSingleton<SignalRStateService>();

// Register chat command service for action card → chat integration
builder.Services.AddSingleton<ChatCommandService>();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
