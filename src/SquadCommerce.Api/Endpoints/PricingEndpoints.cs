namespace SquadCommerce.Api.Endpoints;

public static class PricingEndpoints
{
    public static IEndpointRouteBuilder MapPricingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pricing")
            .WithTags("Pricing");

        // TODO: Implement pricing endpoints
        // - GET /api/pricing/{sku} - Get current pricing
        // - POST /api/pricing/update - Update pricing
        // - GET /api/pricing/competitor/{sku} - Get competitor pricing
        
        return app;
    }
}
