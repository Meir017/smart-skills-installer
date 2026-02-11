using System.Text.Json;
using System.Text.RegularExpressions;
using SmartSkills.Core.Models;

namespace SmartSkills.Core.Registry;

public class SkillRegistryParser
{
    public SkillRegistry Parse(string json)
    {
        return JsonSerializer.Deserialize<SkillRegistry>(json)
               ?? throw new InvalidOperationException("Failed to deserialize skill registry.");
    }

    /// <summary>
    /// Finds all manifest URLs for a given library name by matching against registry patterns.
    /// Supports exact matches and glob patterns (e.g., "Microsoft.Extensions.*").
    /// </summary>
    public IReadOnlyList<string> FindManifestUrls(SkillRegistry registry, string libraryName)
    {
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in registry.Entries)
        {
            if (IsMatch(entry.LibraryPattern, libraryName))
            {
                foreach (var url in entry.SkillManifestUrls)
                {
                    urls.Add(url);
                }
            }
        }

        return urls.ToList();
    }

    /// <summary>
    /// Finds all unique manifest URLs for a set of library names.
    /// </summary>
    public IReadOnlyList<string> FindAllManifestUrls(SkillRegistry registry, IEnumerable<string> libraryNames)
    {
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in libraryNames)
        {
            foreach (var url in FindManifestUrls(registry, name))
            {
                urls.Add(url);
            }
        }

        return urls.ToList();
    }

    private static bool IsMatch(string pattern, string libraryName)
    {
        // Exact match (case-insensitive)
        if (pattern.Equals(libraryName, StringComparison.OrdinalIgnoreCase))
            return true;

        // Glob pattern: convert * to regex .*
        if (pattern.Contains('*') || pattern.Contains('?'))
        {
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return Regex.IsMatch(libraryName, regexPattern, RegexOptions.IgnoreCase);
        }

        return false;
    }
}
