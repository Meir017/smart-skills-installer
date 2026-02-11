using Microsoft.Extensions.DependencyInjection;
using SmartSkills.Core.Installation;
using SmartSkills.Core.Providers;
using SmartSkills.Core.Registry;
using SmartSkills.Core.Resilience;
using SmartSkills.Core.Scanning;

namespace SmartSkills.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all SmartSkills.Core services into the DI container.
    /// </summary>
    public static IServiceCollection AddSmartSkills(this IServiceCollection services, string? skillsOutputDirectory = null)
    {
        // Resilience
        services.AddSingleton<RetryPolicy>();
        services.AddSingleton(sp =>
            new LocalCache(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartSkills", "cache"),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LocalCache>>()));

        // Scanning
        services.AddSingleton<IPackageResolver, DotnetCliPackageResolver>();
        services.AddSingleton<ILibraryScanner, LibraryScanner>();
        services.AddSingleton<IProjectDetector, ProjectDetector>();

        // Registry
        services.AddSingleton<ISkillMatcher, SkillMatcher>();
        services.AddSingleton<ISkillRegistry, SkillRegistry>();
        services.AddSingleton<ISkillMetadataParser, SkillMetadataParser>();

        // Provider factory — creates providers from RepoUrl on demand
        services.AddSingleton<ISkillSourceProviderFactory, SkillSourceProviderFactory>();

        // Installation
        services.AddSingleton<ISkillInstaller, SkillInstaller>();
        services.AddSingleton<ISkillStore>(sp =>
            new LocalSkillStore(
                skillsOutputDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), ".agents", "skills"),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LocalSkillStore>>()));
        return services;
    }
}
