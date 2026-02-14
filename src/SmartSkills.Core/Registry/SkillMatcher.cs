using System.Text.RegularExpressions;
using SmartSkills.Core.Scanning;

namespace SmartSkills.Core.Registry;

/// <summary>
/// Matches packages to skills using exact and glob pattern matching.
/// </summary>
public sealed class SkillMatcher : ISkillMatcher
{
    public IReadOnlyList<MatchedSkill> Match(
        IEnumerable<ResolvedPackage> packages,
        IEnumerable<RegistryEntry> registryEntries)
    {
        var entries = registryEntries.ToList();
        var packageList = packages.ToList();
        var results = new Dictionary<string, MatchedSkill>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            // Determine which packages are eligible based on the entry's language filter
            var eligiblePackages = entry.Language is null
                ? packageList
                : packageList.Where(p => string.Equals(p.Ecosystem, entry.Language, StringComparison.OrdinalIgnoreCase)).ToList();

            var eligibleNames = eligiblePackages.Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var matchedPatterns = new List<string>();

            foreach (var pattern in entry.MatchCriteria)
            {
                foreach (var pkgName in eligibleNames)
                {
                    if (IsMatch(pkgName, pattern))
                    {
                        matchedPatterns.Add(pattern);
                        break;
                    }
                }
            }

            if (matchedPatterns.Count > 0 && !results.ContainsKey(entry.SkillPath))
            {
                results[entry.SkillPath] = new MatchedSkill
                {
                    RegistryEntry = entry,
                    MatchedPatterns = matchedPatterns
                };
            }
        }

        return results.Values.ToList();
    }

    private static bool IsMatch(string packageName, string pattern)
    {
        // Note: Contains with char doesn't have StringComparison overload in .NET, using culture-invariant comparison
        if (pattern.Contains('*', StringComparison.Ordinal) || pattern.Contains('?', StringComparison.Ordinal))
        {
            var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal).Replace("\\?", ".", StringComparison.Ordinal) + "$";
            return Regex.IsMatch(packageName, regex, RegexOptions.IgnoreCase);
        }

        return string.Equals(packageName, pattern, StringComparison.OrdinalIgnoreCase);
    }
}
