using SmartSkills.Core.Scanning;

namespace SmartSkills.Core.Registry.Matching;

/// <summary>
/// Context carrying all available signals for match evaluation.
/// Designed as a class so new signal properties can be added over time
/// without breaking existing <see cref="IMatchStrategy"/> implementations.
/// </summary>
public class MatchContext
{
    /// <summary>Resolved packages from project scanning.</summary>
    public IReadOnlyList<ResolvedPackage> ResolvedPackages { get; init; } = [];

    /// <summary>File names (not full paths) found in the project root directory.</summary>
    public IReadOnlyList<string> RootFileNames { get; init; } = [];

    /// <summary>Optional ecosystem filter from the registry entry (e.g. "dotnet", "javascript").</summary>
    public string? Language { get; init; }
}
