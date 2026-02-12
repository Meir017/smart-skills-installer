using SmartSkills.Core.Registry;

namespace SmartSkills.Core.Installation;

public record InstallOptions
{
    /// <summary>Project or solution path (defaults to current directory).</summary>
    public string? ProjectPath { get; init; }

    /// <summary>When true, report what would happen without making changes.</summary>
    public bool DryRun { get; init; }

    /// <summary>When true, overwrite locally modified skills.</summary>
    public bool Force { get; init; }
}

public record InstallResult
{
    public required IReadOnlyList<string> Installed { get; init; }
    public required IReadOnlyList<string> Updated { get; init; }
    public required IReadOnlyList<string> SkippedUpToDate { get; init; }
    public required IReadOnlyList<SkillInstallFailure> Failed { get; init; }
}

public record SkillInstallFailure(string SkillPath, string Reason);

public record RestoreResult
{
    public required IReadOnlyList<string> Restored { get; init; }
    public required IReadOnlyList<string> SkippedUpToDate { get; init; }
    public required IReadOnlyList<SkillInstallFailure> Failed { get; init; }
}
