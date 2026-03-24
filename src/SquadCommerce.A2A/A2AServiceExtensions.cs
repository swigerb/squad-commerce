using Microsoft.Extensions.DependencyInjection;
using SquadCommerce.A2A.Validation;
using SquadCommerce.Contracts.Interfaces;

namespace SquadCommerce.A2A;

/// <summary>
/// Extension methods for registering A2A protocol infrastructure.
/// </summary>
public static class A2AServiceExtensions
{
    /// <summary>
    /// Registers A2A client, server, and validation infrastructure.
    /// </summary>
    public static IServiceCollection AddSquadCommerceA2A(this IServiceCollection services)
    {
        services.AddHttpClient<IA2AClient, A2AClient>();
        services.AddScoped<A2AServer>();
        services.AddScoped<ExternalDataValidator>();

        return services;
    }
}
