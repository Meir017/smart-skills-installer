// SmartSkills Public API Surface
// This file is for design review only — not compiled.

namespace SmartSkills.Core;

#region S02: Library Detection & Scanning

/// <summary>
/// Represents a resolved package in a project's dependency graph.
/// </summary>
public record ResolvedPackage
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required bool IsTransitive { get; init; }
    public string? TargetFramework { get; init; }
    public string? RequestedVersion { get; init; }
}

/// <summary>
/// Result of resolving packages for a single project.
/// </summary>
public record ProjectPackages(string ProjectPath, IReadOnlyList<ResolvedPackage> Packages);

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

#endregion

#region S03: Skill Registry & Mapping

/// <summary>
/// Parsed SKILL.md frontmatter per the Agent Skills specification.
/// The SKILL.md frontmatter IS the manifest — no separate schema needed.
/// See: https://agentskills.io/specification.md
/// </summary>
public record SkillMetadata
{
    /// <summary>1–64 chars, lowercase alphanumeric + hyphens, must match directory name.</summary>
    public required string Name { get; init; }

    /// <summary>1–1024 chars, describes what the skill does and when to use it.</summary>
    public required string Description { get; init; }

    /// <summary>Optional license name or reference to bundled license file.</summary>
    public string? License { get; init; }

    /// <summary>Optional, ≤500 chars. Environment requirements (products, system packages, network).</summary>
    public string? Compatibility { get; init; }

    /// <summary>Optional arbitrary key-value metadata (e.g. author, version).</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>Optional space-delimited list of pre-approved tools. (Experimental)</summary>
    public string? AllowedTools { get; init; }
}

/// <summary>
/// An entry in the registry index mapping package patterns to a skill.
/// </summary>
public record RegistryEntry
{
    /// <summary>NuGet package name patterns (exact or glob) that trigger this skill.</summary>
    public required IReadOnlyList<string> PackagePatterns { get; init; }

    /// <summary>Relative path to the skill directory in the source repository.</summary>
    public required string SkillPath { get; init; }
}

/// <summary>
/// A skill matched from the registry, before download.
/// </summary>
public record MatchedSkill
{
    /// <summary>The registry entry that matched.</summary>
    public required RegistryEntry RegistryEntry { get; init; }

    /// <summary>The provider that hosts this skill.</summary>
    public required ISkillSourceProvider SourceProvider { get; init; }
}

/// <summary>
/// Provides access to the skill registry index from configured sources.
/// </summary>
public interface ISkillRegistry
{
    /// <summary>
    /// Load the registry index entries from all configured source providers.
    /// </summary>
    Task<IReadOnlyList<RegistryEntry>> GetRegistryEntriesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Matches detected packages to applicable skills from the registry.
/// Supports exact name matches, prefix/glob patterns, and priority ordering.
/// </summary>
public interface ISkillMatcher
{
    IReadOnlyList<MatchedSkill> Match(
        IEnumerable<ResolvedPackage> packages,
        IEnumerable<RegistryEntry> registryEntries);
}

#endregion

#region S04: Skill Source Provider Abstraction

/// <summary>
/// Abstracts all interaction with a remote skill repository.
/// Implementations exist for GitHub (S05) and Azure DevOps (S06).
/// </summary>
public interface ISkillSourceProvider
{
    /// <summary>Identifies this provider type for configuration and state tracking.</summary>
    string ProviderType { get; }

    /// <summary>
    /// Fetch and return the parsed registry index from this source.
    /// </summary>
    Task<IReadOnlyList<RegistryEntry>> GetRegistryIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerate all files in a skill directory tree (SKILL.md, scripts/*, references/*, assets/*).
    /// Returns relative paths from the skill root.
    /// </summary>
    Task<IReadOnlyList<string>> ListSkillFilesAsync(string skillPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download the raw content of a single file.
    /// </summary>
    Task<Stream> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Return the SHA of the most recent commit that touched the given skill directory.
    /// Used for cache validation — if SHA matches the locally stored value, skip re-download.
    /// </summary>
    Task<string> GetLatestCommitShaAsync(string skillPath, CancellationToken cancellationToken = default);
}

#endregion

#region S07: Skill Installation Engine

/// <summary>
/// Parses and validates SKILL.md frontmatter per the Agent Skills specification.
/// </summary>
public interface ISkillMetadataParser
{
    /// <summary>
    /// Parse YAML frontmatter from SKILL.md content. Returns null if invalid.
    /// </summary>
    SkillMetadata? Parse(string skillMdContent, out IReadOnlyList<string> validationErrors);
}

/// <summary>
/// Tracks the state of a locally installed skill.
/// </summary>
public record InstalledSkill
{
    public required string Name { get; init; }
    public required SkillMetadata Metadata { get; init; }
    public required string InstallPath { get; init; }
    public required DateTimeOffset InstalledAt { get; init; }
    public required string SourceProviderType { get; init; }
    public required string SourceUrl { get; init; }
    public required string CommitSha { get; init; }
}

/// <summary>
/// Orchestrates the full skill installation pipeline:
/// resolve packages → match skills → check cache → fetch via provider → validate → install.
/// </summary>
public interface ISkillInstaller
{
    Task<InstallResult> InstallAsync(InstallOptions options, CancellationToken cancellationToken = default);
    Task UninstallAsync(string skillName, CancellationToken cancellationToken = default);
}

public record InstallOptions
{
    /// <summary>Project or solution path (defaults to current directory).</summary>
    public string? ProjectPath { get; init; }

    /// <summary>When true, report what would happen without making changes.</summary>
    public bool DryRun { get; init; }
}

public record InstallResult
{
    public required IReadOnlyList<InstalledSkill> Installed { get; init; }
    public required IReadOnlyList<InstalledSkill> Updated { get; init; }
    public required IReadOnlyList<string> SkippedUpToDate { get; init; }
    public required IReadOnlyList<SkillInstallFailure> Failed { get; init; }
}

public record SkillInstallFailure(string SkillPath, string Reason);

/// <summary>
/// Manages the local skill storage and installed state tracking.
/// </summary>
public interface ISkillStore
{
    Task<IReadOnlyList<InstalledSkill>> GetInstalledSkillsAsync(CancellationToken cancellationToken = default);
    Task<InstalledSkill?> GetByNameAsync(string skillName, CancellationToken cancellationToken = default);
    Task SaveAsync(InstalledSkill skill, CancellationToken cancellationToken = default);
    Task RemoveAsync(string skillName, CancellationToken cancellationToken = default);
}

#endregion

#region S08: Configuration

public record SmartSkillsConfig
{
    /// <summary>Configured skill source providers, in priority order.</summary>
    public IReadOnlyList<SourceProviderConfig> Sources { get; init; } = [];

    /// <summary>Local directory for installed skills.</summary>
    public string? SkillsOutputDirectory { get; init; }
}

public record SourceProviderConfig
{
    /// <summary>Provider type identifier (e.g. "github", "azuredevops").</summary>
    public required string ProviderType { get; init; }

    /// <summary>Repository URL.</summary>
    public required string Url { get; init; }

    /// <summary>Optional path to the registry index file within the repo.</summary>
    public string? RegistryIndexPath { get; init; }

    /// <summary>Optional credential key for authenticated sources.</summary>
    public string? CredentialKey { get; init; }
}

public interface IConfigProvider
{
    /// <summary>
    /// Load merged configuration (user-level + project-level overrides).
    /// </summary>
    Task<SmartSkillsConfig> LoadAsync(string? projectPath = null, CancellationToken cancellationToken = default);
}

#endregion

#region DI Registration

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all SmartSkills.Core services into the DI container.
    /// </summary>
    public static IServiceCollection AddSmartSkills(this IServiceCollection services);
}

#endregion
