using System.Text.RegularExpressions;
using SmartSkills.Core.Models;

namespace SmartSkills.Core.Registry;

public class SkillMatchingEngine
{
    private readonly SkillRegistryParser _registryParser = new();

    /// <summary>
    /// Matches detected libraries against skill manifests from the registry.
    /// Returns a deduplicated, version-filtered list of resolved skills.
    /// </summary>
    public IReadOnlyList<ResolvedSkill> Match(
        IReadOnlyList<DetectedPackage> libraries,
        SkillRegistry registry,
        IReadOnlyList<SkillManifest> manifests)
    {
        var resolved = new Dictionary<string, ResolvedSkill>(StringComparer.OrdinalIgnoreCase);

        foreach (var manifest in manifests)
        {
            var matchedLibraries = new List<string>();

            foreach (var library in libraries)
            {
                foreach (var trigger in manifest.Triggers)
                {
                    if (!IsPatternMatch(trigger.LibraryPattern, library.Name))
                        continue;

                    if (!IsVersionCompatible(library.Version, trigger.MinVersion, trigger.MaxVersion))
                        continue;

                    matchedLibraries.Add(library.Name);
                    break; // one trigger match per library is enough
                }
            }

            if (matchedLibraries.Count == 0)
                continue;

            if (!resolved.TryGetValue(manifest.SkillId, out var existing))
            {
                resolved[manifest.SkillId] = new ResolvedSkill
                {
                    SkillId = manifest.SkillId,
                    Name = manifest.Name,
                    Version = manifest.Version,
                    Manifest = manifest,
                    MatchedLibraries = matchedLibraries,
                };
            }
            else
            {
                // Merge matched libraries for dedup
                foreach (var lib in matchedLibraries)
                {
                    if (!existing.MatchedLibraries.Contains(lib, StringComparer.OrdinalIgnoreCase))
                        existing.MatchedLibraries.Add(lib);
                }
            }
        }

        // Order by number of matched libraries (most relevant first)
        return resolved.Values
            .OrderByDescending(s => s.MatchedLibraries.Count)
            .ThenBy(s => s.Name)
            .ToList();
    }

    private static bool IsPatternMatch(string pattern, string libraryName)
    {
        if (pattern.Equals(libraryName, StringComparison.OrdinalIgnoreCase))
            return true;

        if (pattern.Contains('*') || pattern.Contains('?'))
        {
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return Regex.IsMatch(libraryName, regexPattern, RegexOptions.IgnoreCase);
        }

        // Support regex patterns (prefixed or containing regex metacharacters)
        if (pattern.StartsWith('^') || pattern.EndsWith('$') || pattern.Contains("\\.", StringComparison.Ordinal))
        {
            try
            {
                return Regex.IsMatch(libraryName, pattern, RegexOptions.IgnoreCase);
            }
            catch (RegexParseException)
            {
                return false;
            }
        }

        return false;
    }

    internal static bool IsVersionCompatible(string? libraryVersion, string? minVersion, string? maxVersion)
    {
        if (libraryVersion is null || (minVersion is null && maxVersion is null))
            return true;

        if (!Version.TryParse(NormalizeVersion(libraryVersion), out var libVer))
            return true; // can't compare, assume compatible

        if (minVersion is not null && Version.TryParse(NormalizeVersion(minVersion), out var minVer))
        {
            if (libVer < minVer)
                return false;
        }

        if (maxVersion is not null && Version.TryParse(NormalizeVersion(maxVersion), out var maxVer))
        {
            if (libVer > maxVer)
                return false;
        }

        return true;
    }

    private static string NormalizeVersion(string version)
    {
        // Strip pre-release suffixes for comparison (e.g., "1.0.0-beta" -> "1.0.0")
        var idx = version.IndexOf('-');
        return idx >= 0 ? version[..idx] : version;
    }
}
