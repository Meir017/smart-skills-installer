using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Resolves packages from bun.lock (JSONC text-based lockfile, Bun v1.2+).
/// </summary>
public sealed partial class BunPackageResolver(ILogger<BunPackageResolver> logger) : IPackageResolver
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

        var lockFilePath = Path.Combine(projectDir, "bun.lock");
        List<ResolvedPackage> packages;

        if (File.Exists(lockFilePath))
        {
            logger.LogDebug("Parsing bun.lock at {Path}", lockFilePath);
            var lockText = await File.ReadAllTextAsync(lockFilePath, cancellationToken).ConfigureAwait(false);
            packages = ParseBunLock(lockText, directDeps);
        }
        else
        {
            logger.LogDebug("No bun.lock found, using direct dependencies only");
            packages = directDeps.Select(kvp => new ResolvedPackage
            {
                Name = kvp.Key,
                Version = kvp.Value,
                IsTransitive = false,
                Ecosystem = Ecosystems.JavaScript
            }).ToList();
        }

        logger.LogInformation("Resolved {Count} bun packages from {Path}", packages.Count, packageJsonPath);
        return new ProjectPackages(packageJsonPath, packages);
    }

    /// <summary>
    /// Parse bun.lock (JSONC format) to extract resolved packages.
    /// bun.lock has a "packages" object where keys are package names
    /// and values are arrays: ["resolved@version", { metadata }, "hash"].
    /// </summary>
    internal static List<ResolvedPackage> ParseBunLock(string lockText, Dictionary<string, string> directDeps)
    {
        var packages = new List<ResolvedPackage>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // bun.lock is JSONC (JSON with trailing commas and comments) — strip comments and trailing commas
        var jsonText = StripJsoncFeatures(lockText);

        using var doc = JsonDocument.Parse(jsonText, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });
        var root = doc.RootElement;

        if (!root.TryGetProperty("packages", out var packagesObj))
            return packages;

        foreach (var entry in packagesObj.EnumerateObject())
        {
            var name = entry.Name;
            if (string.IsNullOrEmpty(name))
                continue;

            // The value is an array: ["resolved@version", { deps }, "hash"]
            if (entry.Value.ValueKind != JsonValueKind.Array || entry.Value.GetArrayLength() == 0)
                continue;

            var resolved = entry.Value[0].GetString();
            if (resolved is null)
                continue;

            var version = ExtractVersionFromResolved(resolved);

            if (!seen.Add(name))
                continue;

            packages.Add(new ResolvedPackage
            {
                Name = name,
                Version = version ?? "",
                IsTransitive = !directDeps.ContainsKey(name),
                Ecosystem = Ecosystems.JavaScript
            });
        }

        return packages;
    }

    /// <summary>
    /// Extract version from a bun.lock resolved string.
    /// Examples: "express@4.21.1" → "4.21.1", "@azure/identity@3.4.2" → "3.4.2"
    /// </summary>
    internal static string? ExtractVersionFromResolved(string resolved)
    {
        if (string.IsNullOrEmpty(resolved))
            return null;

        // Find the last '@' that separates name from version
        int atIndex;
        if (resolved.StartsWith('@'))
        {
            // Scoped package: find '@' after the scope
            atIndex = resolved.IndexOf('@', 1);
        }
        else
        {
            atIndex = resolved.IndexOf('@', StringComparison.Ordinal);
        }

        if (atIndex <= 0 || atIndex >= resolved.Length - 1)
            return null;

        return resolved[(atIndex + 1)..];
    }

    /// <summary>
    /// Strip JSONC features (line/block comments, trailing commas) to produce valid JSON.
    /// </summary>
    internal static string StripJsoncFeatures(string jsonc)
    {
        // Remove single-line comments (// ...)
        var result = SingleLineCommentRegex().Replace(jsonc, "");
        // Remove block comments (/* ... */)
        result = BlockCommentRegex().Replace(result, "");
        // Remove trailing commas before } or ]
        result = TrailingCommaRegex().Replace(result, "$1");
        return result;
    }

    [GeneratedRegex(@"//[^\n]*")]
    private static partial Regex SingleLineCommentRegex();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex BlockCommentRegex();

    [GeneratedRegex(@",\s*([\}\]])")]
    private static partial Regex TrailingCommaRegex();
}
