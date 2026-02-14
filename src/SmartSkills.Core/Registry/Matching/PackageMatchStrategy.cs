using System.Text.RegularExpressions;

namespace SmartSkills.Core.Registry.Matching;

/// <summary>
/// Matches when installed packages match the criteria patterns (exact or glob).
/// Extracted from the existing SkillMatcher.IsMatch logic.
/// </summary>
public sealed class PackageMatchStrategy : IMatchStrategy
{
    public string Name => "package";

    public MatchResult Evaluate(MatchContext context, IReadOnlyList<string> criteria)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(criteria);

        var eligibleNames = context.Language is null
            ? context.ResolvedPackages.Select(p => p.Name)
            : context.ResolvedPackages
                .Where(p => string.Equals(p.Ecosystem, context.Language, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Name);

        var distinctNames = eligibleNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var matchedPatterns = new List<string>();

        foreach (var pattern in criteria)
        {
            foreach (var pkgName in distinctNames)
            {
                if (IsMatch(pkgName, pattern))
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

    internal static bool IsMatch(string value, string pattern)
    {
        if (pattern.Contains('*', StringComparison.Ordinal) || pattern.Contains('?', StringComparison.Ordinal))
        {
            var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal).Replace("\\?", ".", StringComparison.Ordinal) + "$";
            return Regex.IsMatch(value, regex, RegexOptions.IgnoreCase);
        }

        return string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase);
    }
}
