// Multi-Language Support: Node.js — Public API Surface Changes
// This file is for design review only — not compiled.
// Shows NEW and CHANGED types only. Unchanged types are omitted.

using SmartSkills.Core.Providers;

namespace SmartSkills.Core.Scanning;

#region S01-T01: Ecosystem Constants & ResolvedPackage Changes

/// <summary>
/// Well-known ecosystem identifiers.
/// </summary>
public static class Ecosystems
{
    public const string Dotnet = "dotnet";
    public const string Npm = "npm";
}

/// <summary>
/// CHANGED: Added Ecosystem field (defaults to "dotnet" for backward compatibility).
/// </summary>
public record ResolvedPackage
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required bool IsTransitive { get; init; }
    public string? TargetFramework { get; init; }
    public string? RequestedVersion { get; init; }
    /// <summary>NEW: Identifies the package ecosystem (e.g. "dotnet", "npm").</summary>
    public string Ecosystem { get; init; } = Ecosystems.Dotnet;
}

#endregion

#region S02: Project Type Detection

/// <summary>
/// NEW: Describes a detected project in a directory.
/// </summary>
public record DetectedProject(string Ecosystem, string ProjectFilePath);

/// <summary>
/// NEW: Auto-detects project types in a directory.
/// </summary>
public interface IProjectDetector
{
    /// <summary>
    /// Inspect a directory and return all detected projects (may return multiple for polyglot repos).
    /// </summary>
    IReadOnlyList<DetectedProject> Detect(string directoryPath);
}

#endregion

#region S03: Node.js Package Resolvers

/// <summary>
/// NEW: Resolves npm packages from package.json + package-lock.json.
/// </summary>
public sealed class NpmPackageResolver : IPackageResolver
{
    public Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// NEW: Resolves packages from yarn.lock (Classic v1 and Berry v2+).
/// </summary>
public sealed class YarnPackageResolver : IPackageResolver
{
    public Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// NEW: Resolves packages from pnpm-lock.yaml.
/// </summary>
public sealed class PnpmPackageResolver : IPackageResolver
{
    public Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default);
}

#endregion

#region S03-T04: Package Resolver Factory

/// <summary>
/// NEW: Selects the appropriate IPackageResolver based on ecosystem and lock file presence.
/// </summary>
public interface IPackageResolverFactory
{
    IPackageResolver GetResolver(DetectedProject project);
}

#endregion

#region S04: LibraryScanner Changes

/// <summary>
/// CHANGED: Added ScanDirectoryAsync for multi-project directory scanning.
/// </summary>
public interface ILibraryScanner
{
    Task<ProjectPackages> ScanProjectAsync(string projectPath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectPackages>> ScanSolutionAsync(string solutionPath, CancellationToken cancellationToken = default);
    /// <summary>NEW: Scan a directory for all supported project types and resolve packages from each.</summary>
    Task<IReadOnlyList<ProjectPackages>> ScanDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);
}

#endregion

namespace SmartSkills.Core.Registry;

#region S01-T02: RegistryEntry Changes

/// <summary>
/// CHANGED: Added optional Language field for ecosystem filtering.
/// </summary>
public record RegistryEntry
{
    public required IReadOnlyList<string> PackagePatterns { get; init; }
    public required string SkillPath { get; init; }
    public string? RepoUrl { get; init; }
    public ISkillSourceProvider? SourceProvider { get; init; }
    /// <summary>NEW: When set, this entry only matches packages from the specified ecosystem. Null = match any.</summary>
    public string? Language { get; init; }
}

#endregion
