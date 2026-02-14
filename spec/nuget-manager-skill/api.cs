// Match Strategy Abstraction & nuget-manager Skill — Public API Surface
// This file is for design review only — not compiled.
// Shows NEW and CHANGED types only. Unchanged types are omitted.

namespace SmartSkills.Core.Registry.Matching;

#region S01-T01: Strategy Abstraction

/// <summary>
/// NEW: Context carrying all available signals for match evaluation.
/// Designed as a class so new signal properties can be added over time
/// without breaking existing IMatchStrategy implementations.
/// </summary>
public class MatchContext
{
    /// <summary>Resolved packages from project scanning.</summary>
    public IReadOnlyList<ResolvedPackage> ResolvedPackages { get; init; } = [];

    /// <summary>File names (not paths) found in the project root directory.</summary>
    public IReadOnlyList<string> RootFileNames { get; init; } = [];

    /// <summary>Optional ecosystem filter from the registry entry (e.g. "dotnet", "javascript").</summary>
    public string? Language { get; init; }

    // Future signals can be added here without breaking existing strategies:
    // public IReadOnlyDictionary<string, string>? EnvironmentVariables { get; init; }
    // public IReadOnlyList<FileSearchResult>? FileSearchResults { get; init; }
}

/// <summary>
/// NEW: Result of evaluating a match strategy against context.
/// </summary>
public record MatchResult(bool IsMatch, IReadOnlyList<string> MatchedPatterns);

/// <summary>
/// NEW: Strategy for evaluating whether a registry entry matches a project.
/// Each implementation handles a specific match type (e.g. package patterns, file existence).
/// To add a new strategy: implement this interface and register in DI.
/// </summary>
public interface IMatchStrategy
{
    /// <summary>Strategy identifier (e.g. "package", "file-exists").</summary>
    string Name { get; }

    /// <summary>
    /// Evaluate the given criteria against the match context.
    /// </summary>
    /// <param name="context">All available signals (packages, files, etc.).</param>
    /// <param name="criteria">Strategy-specific patterns/values from the registry entry.</param>
    MatchResult Evaluate(MatchContext context, IReadOnlyList<string> criteria);
}

#endregion

#region S01-T02: Package Match Strategy

/// <summary>
/// NEW: Matches when installed packages match the criteria patterns (exact or glob).
/// Extracted from the existing SkillMatcher.IsMatch logic — identical behavior.
/// </summary>
public sealed class PackageMatchStrategy : IMatchStrategy
{
    public string Name => "package";
    public MatchResult Evaluate(MatchContext context, IReadOnlyList<string> criteria);
}

#endregion

#region S01-T03: File-Exists Match Strategy

/// <summary>
/// NEW: Matches when any of the criteria file patterns are found in
/// MatchContext.RootFileNames. Supports exact names and glob patterns.
/// </summary>
public sealed class FileExistsMatchStrategy : IMatchStrategy
{
    public string Name => "file-exists";
    public MatchResult Evaluate(MatchContext context, IReadOnlyList<string> criteria);
}

#endregion

#region S01-T04: Strategy Resolver

/// <summary>
/// NEW: Resolves IMatchStrategy implementations by name.
/// </summary>
public interface IMatchStrategyResolver
{
    IMatchStrategy Resolve(string strategyName);
}

/// <summary>
/// NEW: Default resolver backed by DI-registered strategies.
/// </summary>
public sealed class MatchStrategyResolver : IMatchStrategyResolver
{
    public MatchStrategyResolver(IEnumerable<IMatchStrategy> strategies);
    public IMatchStrategy Resolve(string strategyName);
}

#endregion

namespace SmartSkills.Core.Registry;

#region S02-T01: RegistryEntry Changes

/// <summary>
/// CHANGED: Replaced PackagePatterns with strategy-based MatchStrategy + MatchCriteria.
/// </summary>
public record RegistryEntry
{
    /// <summary>
    /// The match strategy to use (e.g. "package", "file-exists").
    /// Defaults to "package" for backward compatibility with existing entries.
    /// </summary>
    public string MatchStrategy { get; init; } = "package";

    /// <summary>
    /// Strategy-specific criteria. For "package" strategy, these are package name patterns.
    /// For "file-exists", these are file name patterns. For future strategies, any string values.
    /// </summary>
    public required IReadOnlyList<string> MatchCriteria { get; init; }

    /// <summary>Relative path to the skill directory in the source repository.</summary>
    public required string SkillPath { get; init; }

    /// <summary>URL of the repository that hosts this skill.</summary>
    public string? RepoUrl { get; init; }

    /// <summary>The source provider that can fetch this skill.</summary>
    public ISkillSourceProvider? SourceProvider { get; init; }

    /// <summary>Optional ecosystem filter.</summary>
    public string? Language { get; init; }
}

#endregion

#region S03-T02: Updated SkillMatcher

/// <summary>
/// CHANGED: Match signature accepts MatchContext for strategy-based matching.
/// </summary>
public interface ISkillMatcher
{
    IReadOnlyList<MatchedSkill> Match(
        IEnumerable<ResolvedPackage> packages,
        IEnumerable<RegistryEntry> registryEntries,
        IReadOnlyList<string>? rootFileNames = null);
}

/// <summary>
/// CHANGED: Delegates to IMatchStrategy via IMatchStrategyResolver.
/// No matching logic remains in this class.
/// </summary>
public sealed class SkillMatcher : ISkillMatcher
{
    public SkillMatcher(IMatchStrategyResolver strategyResolver);

    public IReadOnlyList<MatchedSkill> Match(
        IEnumerable<ResolvedPackage> packages,
        IEnumerable<RegistryEntry> registryEntries,
        IReadOnlyList<string>? rootFileNames = null);
}

#endregion

#region S04-T01: Registry JSON (nuget-manager entry)

// New entry in skills-registry.json:
// {
//   "repoUrl": "https://github.com/github/awesome-copilot",
//   "skills": [
//     {
//       "matchStrategy": "file-exists",
//       "matchCriteria": ["*.sln", "*.slnx", "global.json", "Directory.Build.props", "Directory.Packages.props"],
//       "skillPath": "skills/nuget-manager",
//       "language": "dotnet"
//     }
//   ]
// }
//
// Legacy entries continue to work:
// {
//   "packagePatterns": ["Azure.Identity"],  ← auto-inferred as strategy="package", criteria=["Azure.Identity"]
//   "skillPath": "skills/azure-identity-dotnet"
// }

#endregion
