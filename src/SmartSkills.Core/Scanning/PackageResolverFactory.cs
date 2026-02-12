using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Selects the appropriate <see cref="IPackageResolver"/> based on ecosystem and lock file presence.
/// </summary>
public sealed class PackageResolverFactory(
    DotnetCliPackageResolver dotnetResolver,
    NpmPackageResolver npmResolver,
    YarnPackageResolver yarnResolver,
    PnpmPackageResolver pnpmResolver,
    ILogger<PackageResolverFactory> logger) : IPackageResolverFactory
{
    public IPackageResolver GetResolver(DetectedProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (string.Equals(project.Ecosystem, Ecosystems.Dotnet, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("Using DotnetCliPackageResolver for {Path}", project.ProjectFilePath);
            return dotnetResolver;
        }

        if (string.Equals(project.Ecosystem, Ecosystems.Npm, StringComparison.OrdinalIgnoreCase))
        {
            return SelectNodeJsResolver(project);
        }

        throw new NotSupportedException($"Unsupported ecosystem: {project.Ecosystem}");
    }

    private IPackageResolver SelectNodeJsResolver(DetectedProject project)
    {
        var dir = Path.GetDirectoryName(project.ProjectFilePath) ?? Directory.GetCurrentDirectory();

        if (File.Exists(Path.Combine(dir, "pnpm-lock.yaml")))
        {
            logger.LogDebug("Detected pnpm-lock.yaml, using PnpmPackageResolver for {Path}", project.ProjectFilePath);
            return pnpmResolver;
        }

        if (File.Exists(Path.Combine(dir, "yarn.lock")))
        {
            logger.LogDebug("Detected yarn.lock, using YarnPackageResolver for {Path}", project.ProjectFilePath);
            return yarnResolver;
        }

        logger.LogDebug("Using NpmPackageResolver for {Path}", project.ProjectFilePath);
        return npmResolver;
    }
}
