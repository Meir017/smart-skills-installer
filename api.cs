// Recursive Project Scanning — Public API Surface Changes
// This file is for design review only — not compiled.
// Shows NEW and CHANGED types only. Unchanged types are omitted.

namespace SmartSkills.Core.Scanning;

#region S01-T01: ProjectDetectionOptions

/// <summary>
/// NEW: Configuration options for project detection behavior.
/// </summary>
public record ProjectDetectionOptions
{
    /// <summary>Whether to recursively search subdirectories. Default: false.</summary>
    public bool Recursive { get; init; }

    /// <summary>
    /// Maximum directory depth to traverse when Recursive is true.
    /// 0 = current directory only, 1 = immediate children, etc. Default: 5.
    /// </summary>
    public int MaxDepth { get; init; } = 5;
}

#endregion

#region S01-T02: IProjectDetector Changes

/// <summary>
/// CHANGED: Added overload accepting ProjectDetectionOptions.
/// </summary>
public interface IProjectDetector
{
    /// <summary>
    /// Inspect a directory and return all detected projects (single-level, existing behavior).
    /// </summary>
    IReadOnlyList<DetectedProject> Detect(string directoryPath);

    /// <summary>
    /// NEW: Inspect a directory and return all detected projects, optionally recursing into subdirectories.
    /// </summary>
    IReadOnlyList<DetectedProject> Detect(string directoryPath, ProjectDetectionOptions options);
}

#endregion

#region S01-T04: ExcludedDirectories

/// <summary>
/// NEW: Well-known directory names that should be skipped during recursive traversal.
/// </summary>
public static class ExcludedDirectories
{
    /// <summary>Version control and IDE directories.</summary>
    public static IReadOnlySet<string> Universal { get; }
    // Contains: .git, .hg, .svn, .vs, .vscode, .idea

    /// <summary>.NET build output directories.</summary>
    public static IReadOnlySet<string> DotNet { get; }
    // Contains: bin, obj

    /// <summary>Node.js ecosystem directories.</summary>
    public static IReadOnlySet<string> NodeJs { get; }
    // Contains: node_modules, .next, .nuxt, bower_components

    /// <summary>Python ecosystem directories.</summary>
    public static IReadOnlySet<string> Python { get; }
    // Contains: venv, .venv, __pycache__, .tox, .mypy_cache, .pytest_cache, site-packages

    /// <summary>Java ecosystem directories.</summary>
    public static IReadOnlySet<string> Java { get; }
    // Contains: target, .gradle, build

    /// <summary>Generic build output directories.</summary>
    public static IReadOnlySet<string> BuildOutput { get; }
    // Contains: dist, vendor, coverage, .cache

    /// <summary>Combined set of all excluded directory names (case-insensitive).</summary>
    public static IReadOnlySet<string> All { get; }
}

#endregion

#region S02-T01: ILibraryScanner Changes

/// <summary>
/// CHANGED: Added ScanDirectoryAsync overload with ProjectDetectionOptions.
/// </summary>
public interface ILibraryScanner
{
    Task<ProjectPackages> ScanProjectAsync(string projectPath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectPackages>> ScanSolutionAsync(string solutionPath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectPackages>> ScanDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>NEW: Scan a directory with configurable detection options (supports recursive scanning).</summary>
    Task<IReadOnlyList<ProjectPackages>> ScanDirectoryAsync(string directoryPath, ProjectDetectionOptions options, CancellationToken cancellationToken = default);
}

#endregion
