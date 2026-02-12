using SmartSkills.Core.Providers;

namespace SmartSkills.Core.Registry;

/// <summary>
/// Parsed SKILL.md frontmatter per the Agent Skills specification.
/// </summary>
public record SkillMetadata
{
    /// <summary>1–64 chars, lowercase alphanumeric + hyphens, must match directory name.</summary>
    public required string Name { get; init; }

    /// <summary>1–1024 chars, describes what the skill does and when to use it.</summary>
    public required string Description { get; init; }

    /// <summary>Optional license name or reference to bundled license file.</summary>
    public string? License { get; init; }

    /// <summary>Optional, ≤500 chars. Environment requirements.</summary>
    public string? Compatibility { get; init; }

    /// <summary>Optional arbitrary key-value metadata (e.g. author, version).</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>Optional space-delimited list of pre-approved tools.</summary>
    public string? AllowedTools { get; init; }
}

/// <summary>
/// An entry in the registry index mapping package patterns to a skill.
/// </summary>
public record RegistryEntry
{
    /// <summary>Package name patterns (exact or glob) that trigger this skill.</summary>
    public required IReadOnlyList<string> PackagePatterns { get; init; }

    /// <summary>Relative path to the skill directory in the source repository.</summary>
    public required string SkillPath { get; init; }

    /// <summary>URL of the repository that hosts this skill.</summary>
#pragma warning disable CA1056 // URI properties should not be strings
    public string? RepoUrl { get; init; }
#pragma warning restore CA1056 // URI properties should not be strings

    /// <summary>The source provider that can fetch this skill. Null for entries that need a default provider.</summary>
    public ISkillSourceProvider? SourceProvider { get; init; }

    /// <summary>
    /// Optional ecosystem filter (e.g. "dotnet", "npm"). When set, this entry only matches
    /// packages from the specified ecosystem. When null, matches any ecosystem.
    /// </summary>
    public string? Language { get; init; }
}

/// <summary>
/// A skill matched from the registry, before download.
/// </summary>
public record MatchedSkill
{
    /// <summary>The registry entry that matched.</summary>
    public required RegistryEntry RegistryEntry { get; init; }

    /// <summary>The package patterns that triggered the match.</summary>
    public required IReadOnlyList<string> MatchedPatterns { get; init; }
}
