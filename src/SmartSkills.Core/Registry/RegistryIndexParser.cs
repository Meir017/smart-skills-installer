using System.Reflection;
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
    private const string EmbeddedResourceName = "SmartSkills.Core.Registry.skills-registry.json";

    /// <summary>
    /// Parse a single JSON string into registry entries.
    /// </summary>
    public static IReadOnlyList<RegistryEntry> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return ParseDocument(doc);
    }

    /// <summary>
    /// Load the base embedded registry and merge with entries parsed from additional JSON files.
    /// </summary>
    public static IReadOnlyList<RegistryEntry> LoadMerged(IEnumerable<string>? additionalJsonPaths = null)
    {
        var entries = new List<RegistryEntry>(LoadEmbedded());

        if (additionalJsonPaths is not null)
        {
            foreach (var path in additionalJsonPaths)
            {
                var json = File.ReadAllText(path);
                entries.AddRange(Parse(json));
            }
        }

        return entries;
    }

    /// <summary>
    /// Load registry entries from the embedded base configuration.
    /// </summary>
    public static IReadOnlyList<RegistryEntry> LoadEmbedded()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{EmbeddedResourceName}' not found.");

        using var doc = JsonDocument.Parse(stream);
        return ParseDocument(doc);
    }

    private static IReadOnlyList<RegistryEntry> ParseDocument(JsonDocument doc)
    {
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
