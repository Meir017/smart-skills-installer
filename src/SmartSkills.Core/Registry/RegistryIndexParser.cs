using System.Text.Json;

namespace SmartSkills.Core.Registry;

/// <summary>
/// Parses a registry index JSON file into RegistryEntry objects.
/// Expected format:
/// {
///   "skills": [
///     { "packagePatterns": ["Pkg.Name", "Pkg.Prefix*"], "skillPath": "skills/my-skill" }
///   ]
/// }
/// </summary>
public static class RegistryIndexParser
{
    public static IReadOnlyList<RegistryEntry> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var entries = new List<RegistryEntry>();

        if (!root.TryGetProperty("skills", out var skills))
            return entries;

        foreach (var skill in skills.EnumerateArray())
        {
            var patterns = new List<string>();
            if (skill.TryGetProperty("packagePatterns", out var patternsElement))
            {
                foreach (var pattern in patternsElement.EnumerateArray())
                {
                    var val = pattern.GetString();
                    if (val is not null)
                        patterns.Add(val);
                }
            }

            var skillPath = skill.TryGetProperty("skillPath", out var sp) ? sp.GetString() : null;
            if (skillPath is not null && patterns.Count > 0)
            {
                entries.Add(new RegistryEntry
                {
                    PackagePatterns = patterns,
                    SkillPath = skillPath
                });
            }
        }

        return entries;
    }
}
