namespace SmartSkills.Core.Scanning;

/// <summary>
/// Scans .NET projects/solutions and delegates to IPackageResolver for dependency resolution.
/// </summary>
public interface ILibraryScanner
{
    /// <summary>
    /// Scan a single project file for resolved packages.
    /// </summary>
    Task<ProjectPackages> ScanProjectAsync(string projectPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scan all projects in a solution (.sln or .slnx) file.
    /// </summary>
    Task<IReadOnlyList<ProjectPackages>> ScanSolutionAsync(string solutionPath, CancellationToken cancellationToken = default);
}
