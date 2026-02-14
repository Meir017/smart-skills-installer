namespace SmartSkills.Core.Registry.Matching;

/// <summary>
/// Result of evaluating a match strategy against context.
/// </summary>
public record MatchResult(bool IsMatch, IReadOnlyList<string> MatchedPatterns)
{
    public static MatchResult NoMatch { get; } = new(false, []);
}
