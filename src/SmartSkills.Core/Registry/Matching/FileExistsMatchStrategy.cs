namespace SmartSkills.Core.Registry.Matching;

/// <summary>
/// Matches when any of the criteria file patterns are found in
/// <see cref="MatchContext.RootFileNames"/>. Supports exact names and glob patterns.
/// </summary>
public sealed class FileExistsMatchStrategy : IMatchStrategy
{
    public string Name => "file-exists";

    public MatchResult Evaluate(MatchContext context, IReadOnlyList<string> criteria)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(criteria);

        var matchedPatterns = new List<string>();

        foreach (var pattern in criteria)
        {
            foreach (var fileName in context.RootFileNames)
            {
                if (PackageMatchStrategy.IsMatch(fileName, pattern))
                {
                    matchedPatterns.Add(pattern);
                    break;
                }
            }
        }

        return matchedPatterns.Count > 0
            ? new MatchResult(true, matchedPatterns)
            : MatchResult.NoMatch;
    }
}
