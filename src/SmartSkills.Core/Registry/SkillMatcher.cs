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
        var packageNames = packages.Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var results = new Dictionary<string, MatchedSkill>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var matchedPatterns = new List<string>();

            foreach (var pattern in entry.PackagePatterns)
            {
                foreach (var pkgName in packageNames)
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
        if (pattern.Contains('*') || pattern.Contains('?'))
        {
            var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return Regex.IsMatch(packageName, regex, RegexOptions.IgnoreCase);
        }

        return string.Equals(packageName, pattern, StringComparison.OrdinalIgnoreCase);
    }
}
