using System.Text.Json;
using SmartSkills.Core.Models;

namespace SmartSkills.Core.Scanning;

public class LockFileParser
{
    /// <summary>
    /// Parses a packages.lock.json file and returns all dependencies (direct + transitive).
    /// </summary>
    public IReadOnlyList<DetectedPackage> Parse(string lockFilePath)
    {
        var json = File.ReadAllText(lockFilePath);
        using var doc = JsonDocument.Parse(json);

        var packages = new Dictionary<string, DetectedPackage>(StringComparer.OrdinalIgnoreCase);
        var root = doc.RootElement;

        if (!root.TryGetProperty("dependencies", out var dependencies))
            return [];

        // dependencies is keyed by target framework, e.g. "net8.0"
        foreach (var framework in dependencies.EnumerateObject())
        {
            foreach (var pkg in framework.Value.EnumerateObject())
            {
                var name = pkg.Name;
                var resolved = pkg.Value.TryGetProperty("resolved", out var resolvedProp)
                    ? resolvedProp.GetString()
                    : null;

                var isTransitive = pkg.Value.TryGetProperty("type", out var typeProp)
                    && typeProp.GetString()?.Equals("Transitive", StringComparison.OrdinalIgnoreCase) == true;

                // Keep first occurrence (first TFM wins) or overwrite with direct dep
                if (!packages.TryGetValue(name, out var existing) || (existing.IsTransitive && !isTransitive))
                {
                    packages[name] = new DetectedPackage(name, resolved, isTransitive);
                }
            }
        }

        return packages.Values.ToList();
    }
}
