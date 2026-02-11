using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Resolves npm packages from package.json and package-lock.json.
/// </summary>
public sealed class NpmPackageResolver(ILogger<NpmPackageResolver> logger) : IPackageResolver
{
    public Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var packageJsonPath = projectPath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase)
            ? projectPath
            : Path.Combine(projectPath, "package.json");

        if (!File.Exists(packageJsonPath))
            throw new FileNotFoundException($"package.json not found: {packageJsonPath}");

        var projectDir = Path.GetDirectoryName(packageJsonPath)!;
        var packageJsonText = File.ReadAllText(packageJsonPath);
        var directDeps = ParseDirectDependencies(packageJsonText);

        var lockFilePath = Path.Combine(projectDir, "package-lock.json");
        List<ResolvedPackage> packages;

        if (File.Exists(lockFilePath))
        {
            logger.LogDebug("Parsing package-lock.json at {Path}", lockFilePath);
            var lockFileText = File.ReadAllText(lockFilePath);
            packages = ParseLockFile(lockFileText, directDeps);
        }
        else
        {
            logger.LogDebug("No package-lock.json found, using direct dependencies only");
            packages = directDeps.Select(kvp => new ResolvedPackage
            {
                Name = kvp.Key,
                Version = kvp.Value,
                IsTransitive = false,
                Ecosystem = Ecosystems.Npm
            }).ToList();
        }

        logger.LogInformation("Resolved {Count} npm packages from {Path}", packages.Count, packageJsonPath);
        return Task.FromResult(new ProjectPackages(packageJsonPath, packages));
    }

    /// <summary>
    /// Parse direct dependencies from package.json (dependencies + devDependencies).
    /// Returns a dictionary of package name → version range.
    /// </summary>
    internal static Dictionary<string, string> ParseDirectDependencies(string packageJsonText)
    {
        using var doc = JsonDocument.Parse(packageJsonText);
        var root = doc.RootElement;
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        ReadDependencySection(root, "dependencies", result);
        ReadDependencySection(root, "devDependencies", result);

        return result;
    }

    private static void ReadDependencySection(JsonElement root, string sectionName, Dictionary<string, string> result)
    {
        if (!root.TryGetProperty(sectionName, out var section))
            return;

        foreach (var prop in section.EnumerateObject())
        {
            result.TryAdd(prop.Name, prop.Value.GetString() ?? "");
        }
    }

    /// <summary>
    /// Parse package-lock.json (lockfile version 2 or 3) to extract all resolved packages.
    /// </summary>
    internal static List<ResolvedPackage> ParseLockFile(string lockFileText, Dictionary<string, string> directDeps)
    {
        using var doc = JsonDocument.Parse(lockFileText);
        var root = doc.RootElement;
        var packages = new List<ResolvedPackage>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // lockfileVersion 2+ uses "packages" (path-keyed), lockfileVersion 1 uses "dependencies"
        if (root.TryGetProperty("packages", out var packagesObj))
        {
            foreach (var entry in packagesObj.EnumerateObject())
            {
                // Skip the root entry (empty key "")
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                // Extract package name from the path (e.g. "node_modules/@azure/identity" → "@azure/identity")
                var name = ExtractPackageName(entry.Name);
                if (name is null || !seen.Add(name))
                    continue;

                var version = entry.Value.TryGetProperty("version", out var v) ? v.GetString() ?? "" : "";

                packages.Add(new ResolvedPackage
                {
                    Name = name,
                    Version = version,
                    IsTransitive = !directDeps.ContainsKey(name),
                    Ecosystem = Ecosystems.Npm
                });
            }
        }
        else if (root.TryGetProperty("dependencies", out var depsObj))
        {
            ParseLockFileV1Dependencies(depsObj, directDeps, packages, seen);
        }

        return packages;
    }

    private static void ParseLockFileV1Dependencies(
        JsonElement depsObj,
        Dictionary<string, string> directDeps,
        List<ResolvedPackage> packages,
        HashSet<string> seen)
    {
        foreach (var entry in depsObj.EnumerateObject())
        {
            if (!seen.Add(entry.Name))
                continue;

            var version = entry.Value.TryGetProperty("version", out var v) ? v.GetString() ?? "" : "";

            packages.Add(new ResolvedPackage
            {
                Name = entry.Name,
                Version = version,
                IsTransitive = !directDeps.ContainsKey(entry.Name),
                Ecosystem = Ecosystems.Npm
            });

            // Recurse into nested dependencies
            if (entry.Value.TryGetProperty("dependencies", out var nested))
            {
                ParseLockFileV1Dependencies(nested, directDeps, packages, seen);
            }
        }
    }

    /// <summary>
    /// Extract the npm package name from a node_modules path.
    /// e.g. "node_modules/@azure/identity" → "@azure/identity"
    ///      "node_modules/express" → "express"
    ///      "node_modules/foo/node_modules/bar" → "bar"
    /// </summary>
    internal static string? ExtractPackageName(string path)
    {
        const string nodeModules = "node_modules/";
        var lastIndex = path.LastIndexOf(nodeModules, StringComparison.Ordinal);
        if (lastIndex < 0)
            return null;

        return path[(lastIndex + nodeModules.Length)..];
    }
}
