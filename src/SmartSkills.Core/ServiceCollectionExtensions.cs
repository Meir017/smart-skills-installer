using Microsoft.Extensions.DependencyInjection;
using SmartSkills.Core.Installation;
using SmartSkills.Core.Providers;
using SmartSkills.Core.Registry;
using SmartSkills.Core.Registry.Matching;
using SmartSkills.Core.Resilience;
using SmartSkills.Core.Scanning;

namespace SmartSkills.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all SmartSkills.Core services into the DI container.
    /// </summary>
    public static IServiceCollection AddSmartSkills(this IServiceCollection services)
    {
        // Resilience
        services.AddSingleton<RetryPolicy>();
        services.AddSingleton<LocalCache>();

        // Scanning
        services.AddSingleton<DotnetCliPackageResolver>();
        services.AddSingleton<IPackageResolver>(sp => sp.GetRequiredService<DotnetCliPackageResolver>());
        services.AddSingleton<NpmPackageResolver>();
        services.AddSingleton<YarnPackageResolver>();
        services.AddSingleton<PnpmPackageResolver>();
        services.AddSingleton<BunPackageResolver>();
        services.AddSingleton<UvLockPackageResolver>();
        services.AddSingleton<PoetryLockPackageResolver>();
        services.AddSingleton<PipfileLockPackageResolver>();
        services.AddSingleton<RequirementsTxtPackageResolver>();
        services.AddSingleton<MavenPomPackageResolver>();
        services.AddSingleton<GradlePackageResolver>();
        services.AddSingleton<IPackageResolverFactory, PackageResolverFactory>();
        services.AddSingleton<ILibraryScanner, LibraryScanner>();
        services.AddSingleton<IProjectDetector, ProjectDetector>();

        // Match strategies
        services.AddSingleton<IMatchStrategy, PackageMatchStrategy>();
        services.AddSingleton<IMatchStrategy, FileExistsMatchStrategy>();
        services.AddSingleton<IMatchStrategyResolver, MatchStrategyResolver>();

        // Registry
        services.AddSingleton<ISkillMatcher, SkillMatcher>();
        services.AddSingleton<ISkillRegistry, SkillRegistry>();
        services.AddSingleton<ISkillMetadataParser, SkillMetadataParser>();

        // Provider factory — creates providers from RepoUrl on demand
        services.AddSingleton<ISkillSourceProviderFactory, SkillSourceProviderFactory>();

        // Installation
        services.AddSingleton<ISkillLockFileStore, SkillLockFileStore>();
        services.AddSingleton<ISkillInstaller, SkillInstaller>();
        return services;
    }
}
