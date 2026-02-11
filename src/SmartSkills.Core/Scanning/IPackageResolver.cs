namespace SmartSkills.Core.Scanning;

/// <summary>
/// Abstracts package resolution. Implementations can use different strategies
/// (dotnet CLI, project.assets.json, packages.lock.json, etc.).
/// </summary>
public interface IPackageResolver
{
    /// <summary>
    /// Resolve all packages (direct and transitive) for a project.
    /// </summary>
    Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default);
}
