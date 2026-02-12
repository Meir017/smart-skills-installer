namespace SmartSkills.Core.Scanning;

/// <summary>
/// Well-known ecosystem identifiers for package managers.
/// </summary>
public static class Ecosystems
{
    public const string Dotnet = "dotnet";
    public const string Npm = "npm";
    public const string Python = "python";
    public const string Java = "java";
}

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

    /// <summary>
    /// Identifies the package ecosystem (e.g. "dotnet", "npm").
    /// Defaults to "dotnet" for backward compatibility.
    /// </summary>
    public string Ecosystem { get; init; } = Ecosystems.Dotnet;
}

/// <summary>
/// Result of resolving packages for a single project.
/// </summary>
public record ProjectPackages(string ProjectPath, IReadOnlyList<ResolvedPackage> Packages);
