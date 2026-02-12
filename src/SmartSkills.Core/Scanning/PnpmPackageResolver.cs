using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Resolves packages from pnpm-lock.yaml.
/// </summary>
public sealed class PnpmPackageResolver(ILogger<PnpmPackageResolver> logger) : IPackageResolver
{
    public async Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectPath);

        var packageJsonPath = projectPath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase)
            ? projectPath
            : Path.Combine(projectPath, "package.json");

        if (!File.Exists(packageJsonPath))
            throw new FileNotFoundException($"package.json not found: {packageJsonPath}");

        var projectDir = Path.GetDirectoryName(packageJsonPath)!;
        var packageJsonText = await File.ReadAllTextAsync(packageJsonPath, cancellationToken).ConfigureAwait(false);
        var directDeps = NpmPackageResolver.ParseDirectDependencies(packageJsonText);

        var lockFilePath = Path.Combine(projectDir, "pnpm-lock.yaml");
        List<ResolvedPackage> packages;

        if (File.Exists(lockFilePath))
        {
            logger.LogDebug("Parsing pnpm-lock.yaml at {Path}", lockFilePath);
            var lockText = await File.ReadAllTextAsync(lockFilePath, cancellationToken).ConfigureAwait(false);
            packages = ParsePnpmLock(lockText, directDeps);
        }
        else
        {
            logger.LogDebug("No pnpm-lock.yaml found, using direct dependencies only");
            packages = directDeps.Select(kvp => new ResolvedPackage
            {
                Name = kvp.Key,
                Version = kvp.Value,
                IsTransitive = false,
                Ecosystem = Ecosystems.JavaScript
            }).ToList();
        }

        logger.LogInformation("Resolved {Count} pnpm packages from {Path}", packages.Count, packageJsonPath);
        return new ProjectPackages(packageJsonPath, packages);
    }

    /// <summary>
    /// Parse pnpm-lock.yaml to extract resolved packages.
    /// pnpm-lock.yaml v6+ uses "packages:" with keys like "/@azure/identity@3.4.0" or "/express@4.18.2".
    /// pnpm-lock.yaml v9+ uses keys like "@azure/identity@3.4.0" (no leading slash).
    /// </summary>
    internal static List<ResolvedPackage> ParsePnpmLock(string lockText, Dictionary<string, string> directDeps)
    {
        var packages = new List<ResolvedPackage>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var yaml = new YamlStream();
        using var reader = new StringReader(lockText);
        yaml.Load(reader);

        if (yaml.Documents.Count == 0)
            return packages;

        var root = (YamlMappingNode)yaml.Documents[0].RootNode;

        // Try "packages" key (present in all pnpm lockfile versions)
        if (root.Children.TryGetValue(new YamlScalarNode("packages"), out var packagesNode)
            && packagesNode is YamlMappingNode packagesMap)
        {
            foreach (var entry in packagesMap.Children)
            {
                var key = ((YamlScalarNode)entry.Key).Value;
                if (key is null)
                    continue;

                var (name, version) = ParsePnpmPackageKey(key);
                if (name is null || !seen.Add(name))
                    continue;

                // If the entry itself has a "version" field, prefer that
                if (entry.Value is YamlMappingNode entryMap
                    && entryMap.Children.TryGetValue(new YamlScalarNode("version"), out var versionNode)
                    && versionNode is YamlScalarNode versionScalar
                    && versionScalar.Value is not null)
                {
                    version = versionScalar.Value;
                }

                packages.Add(new ResolvedPackage
                {
                    Name = name,
                    Version = version ?? "",
                    IsTransitive = !directDeps.ContainsKey(name),
                    Ecosystem = Ecosystems.JavaScript
                });
            }
        }

        // Also check "snapshots" key (pnpm v9+) if packages was empty
        if (packages.Count == 0
            && root.Children.TryGetValue(new YamlScalarNode("snapshots"), out var snapshotsNode)
            && snapshotsNode is YamlMappingNode snapshotsMap)
        {
            foreach (var entry in snapshotsMap.Children)
            {
                var key = ((YamlScalarNode)entry.Key).Value;
                if (key is null)
                    continue;

                var (name, version) = ParsePnpmPackageKey(key);
                if (name is null || !seen.Add(name))
                    continue;

                packages.Add(new ResolvedPackage
                {
                    Name = name,
                    Version = version ?? "",
                    IsTransitive = !directDeps.ContainsKey(name),
                    Ecosystem = Ecosystems.JavaScript
                });
            }
        }

        return packages;
    }

    /// <summary>
    /// Parse a pnpm package key into name and version.
    /// Formats: "/@azure/identity@3.4.0", "/express@4.18.2", "@azure/identity@3.4.0", "express@4.18.2"
    /// </summary>
    internal static (string? Name, string? Version) ParsePnpmPackageKey(string key)
    {
        // Remove leading slash if present
        var trimmed = key.TrimStart('/');

        if (string.IsNullOrEmpty(trimmed))
            return (null, null);

        // Find the last '@' that separates name from version
        // For scoped packages like "@azure/identity@3.4.0", we need the last '@'
        int atIndex;
        if (trimmed.StartsWith('@'))
        {
            // Scoped package: find '@' after the scope
            atIndex = trimmed.IndexOf('@', 1);
        }
        else
        {
            atIndex = trimmed.IndexOf('@', StringComparison.Ordinal);
        }

        if (atIndex <= 0)
            return (trimmed, null);

        var name = trimmed[..atIndex];
        var version = trimmed[(atIndex + 1)..];

        // pnpm may append parenthesized peer info like "express@4.18.2(supports-color@5.5.0)"
        var parenIndex = version.IndexOf('(', StringComparison.Ordinal);
        if (parenIndex > 0)
            version = version[..parenIndex];

        return (name, version);
    }
}
