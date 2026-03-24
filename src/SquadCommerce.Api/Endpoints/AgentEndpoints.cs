namespace SquadCommerce.Api.Endpoints;

public static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/agents")
            .WithTags("Agents");

        // TODO: Implement agent endpoints
        // - POST /api/agents/orchestrate - Main orchestration endpoint
        // - GET /api/agents/status - Agent status
        
        return app;
    }
}
