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
    UvLockPackageResolver uvLockResolver,
    PoetryLockPackageResolver poetryLockResolver,
    PipfileLockPackageResolver pipfileLockResolver,
    RequirementsTxtPackageResolver requirementsTxtResolver,
    ILogger<PackageResolverFactory> logger) : IPackageResolverFactory
{
    public IPackageResolver GetResolver(DetectedProject project)
    {
        if (string.Equals(project.Ecosystem, Ecosystems.Dotnet, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("Using DotnetCliPackageResolver for {Path}", project.ProjectFilePath);
            return dotnetResolver;
        }

        if (string.Equals(project.Ecosystem, Ecosystems.Npm, StringComparison.OrdinalIgnoreCase))
        {
            return SelectNodeJsResolver(project);
        }

        if (string.Equals(project.Ecosystem, Ecosystems.Python, StringComparison.OrdinalIgnoreCase))
        {
            return SelectPythonResolver(project);
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

    private IPackageResolver SelectPythonResolver(DetectedProject project)
    {
        var dir = Path.GetDirectoryName(project.ProjectFilePath) ?? Directory.GetCurrentDirectory();

        if (File.Exists(Path.Combine(dir, "uv.lock")))
        {
            logger.LogDebug("Detected uv.lock, using UvLockPackageResolver for {Path}", project.ProjectFilePath);
            return uvLockResolver;
        }

        if (File.Exists(Path.Combine(dir, "poetry.lock")))
        {
            logger.LogDebug("Detected poetry.lock, using PoetryLockPackageResolver for {Path}", project.ProjectFilePath);
            return poetryLockResolver;
        }

        if (File.Exists(Path.Combine(dir, "Pipfile.lock")))
        {
            logger.LogDebug("Detected Pipfile.lock, using PipfileLockPackageResolver for {Path}", project.ProjectFilePath);
            return pipfileLockResolver;
        }

        logger.LogDebug("Using RequirementsTxtPackageResolver for {Path}", project.ProjectFilePath);
        return requirementsTxtResolver;
    }
}
