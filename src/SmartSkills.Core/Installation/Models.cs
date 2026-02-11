using SmartSkills.Core.Registry;

namespace SmartSkills.Core.Installation;

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
#pragma warning disable CA1056 // URI properties should not be strings
    public required string SourceUrl { get; init; }
#pragma warning restore CA1056 // URI properties should not be strings
    public required string CommitSha { get; init; }
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
