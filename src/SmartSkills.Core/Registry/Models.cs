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

    /// <summary>The package patterns that triggered the match.</summary>
    public required IReadOnlyList<string> MatchedPatterns { get; init; }
}
