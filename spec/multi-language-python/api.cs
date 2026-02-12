// Multi-Language Support: Python — Public API Surface Changes
// This file is for design review only — not compiled.
// Shows NEW types only. Unchanged types from the Node.js PR are omitted.

namespace SmartSkills.Core.Scanning;

#region S01-T01: Ecosystem Constant

/// <summary>
/// CHANGED: Added Python constant.
/// </summary>
public static class Ecosystems
{
    public const string Dotnet = "dotnet";
    public const string Npm = "npm";
    public const string Python = "python"; // NEW
}

#endregion

#region S02-T01: PyPI Name Normalization

/// <summary>
/// NEW: Normalizes Python package names per PEP 503.
/// Lowercases, replaces underscores/dots/consecutive-hyphens with single hyphens.
/// </summary>
public static class PypiNameNormalizer
{
    public static string Normalize(string packageName);
}

#endregion

#region S02-T02: Direct Dependency Extraction

/// <summary>
/// NEW: Parses pyproject.toml to extract direct dependency names.
/// Supports PEP 621 [project.dependencies] and Poetry [tool.poetry.dependencies].
/// </summary>
public static class PyprojectParser
{
    /// <summary>
    /// Returns normalized direct dependency names from pyproject.toml content.
    /// </summary>
    public static HashSet<string> ParseDirectDependencies(string tomlContent);
}

#endregion

#region S02-T03 through S02-T06: Python Package Resolvers

/// <summary>
/// NEW: Resolves packages from uv.lock (TOML format).
/// </summary>
public sealed class UvLockPackageResolver : IPackageResolver
{
    public Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// NEW: Resolves packages from poetry.lock (TOML format).
/// </summary>
public sealed class PoetryLockPackageResolver : IPackageResolver
{
    public Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// NEW: Resolves packages from Pipfile.lock (JSON format).
/// </summary>
public sealed class PipfileLockPackageResolver : IPackageResolver
{
    public Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// NEW: Resolves packages from requirements.txt (plain text format).
/// </summary>
public sealed class RequirementsTxtPackageResolver : IPackageResolver
{
    public Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default);
}

#endregion
