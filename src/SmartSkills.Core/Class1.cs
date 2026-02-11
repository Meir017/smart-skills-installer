using Microsoft.Extensions.DependencyInjection;
using SmartSkills.Core.Registry;
using SmartSkills.Core.Scanning;

namespace SmartSkills.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all SmartSkills.Core services into the DI container.
    /// </summary>
    public static IServiceCollection AddSmartSkills(this IServiceCollection services)
    {
        services.AddSingleton<IPackageResolver, DotnetCliPackageResolver>();
        services.AddSingleton<ILibraryScanner, LibraryScanner>();
        services.AddSingleton<ISkillMatcher, SkillMatcher>();
        services.AddSingleton<ISkillRegistry, SkillRegistry>();
        return services;
    }
}
