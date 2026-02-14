namespace SmartSkills.Core.Registry.Matching;

/// <summary>
/// Strategy for evaluating whether a registry entry matches a project.
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
