namespace SmartSkills.Core.Scanning;

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
