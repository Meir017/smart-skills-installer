using Microsoft.Extensions.DependencyInjection;
using SmartSkills.Core.Installation;
using SmartSkills.Core.Registry;
using SmartSkills.Core.Scanning;

namespace SmartSkills.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all SmartSkills.Core services into the DI container.
    /// </summary>
    public static IServiceCollection AddSmartSkills(this IServiceCollection services, string? skillsOutputDirectory = null)
    {
        services.AddSingleton<IPackageResolver, DotnetCliPackageResolver>();
        services.AddSingleton<ILibraryScanner, LibraryScanner>();
        services.AddSingleton<ISkillMatcher, SkillMatcher>();
        services.AddSingleton<ISkillRegistry, SkillRegistry>();
        services.AddSingleton<ISkillMetadataParser, SkillMetadataParser>();
        services.AddSingleton<ISkillInstaller, SkillInstaller>();
        services.AddSingleton<ISkillStore>(sp =>
            new LocalSkillStore(
                skillsOutputDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), ".smartskills"),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LocalSkillStore>>()));
        return services;
    }
}
