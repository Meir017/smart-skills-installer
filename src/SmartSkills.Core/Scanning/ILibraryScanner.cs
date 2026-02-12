namespace SmartSkills.Core.Scanning;

/// <summary>
/// Scans projects and solutions for resolved packages across multiple ecosystems.
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

    /// <summary>
    /// Scan a directory for all supported project types and resolve packages from each.
    /// </summary>
    Task<IReadOnlyList<ProjectPackages>> ScanDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scan a directory with configurable detection options (supports recursive scanning).
    /// </summary>
    Task<IReadOnlyList<ProjectPackages>> ScanDirectoryAsync(string directoryPath, ProjectDetectionOptions options, CancellationToken cancellationToken = default);
}
