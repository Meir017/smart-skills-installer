namespace SmartSkills.Core.Scanning;

/// <summary>
/// Selects the appropriate <see cref="IPackageResolver"/> based on ecosystem and lock file presence.
/// </summary>
public interface IPackageResolverFactory
{
    /// <summary>
    /// Get the resolver for the given detected project.
    /// </summary>
    IPackageResolver GetResolver(DetectedProject project);
}
