using Microsoft.Extensions.DependencyInjection;

namespace SmartSkills.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all SmartSkills.Core services into the DI container.
    /// </summary>
    public static IServiceCollection AddSmartSkills(this IServiceCollection services)
    {
        return services;
    }
}
