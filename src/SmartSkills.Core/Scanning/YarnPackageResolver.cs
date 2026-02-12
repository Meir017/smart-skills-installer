using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Resolves packages from yarn.lock (Yarn Classic v1 and Yarn Berry v2+ formats).
/// </summary>
public sealed partial class YarnPackageResolver(ILogger<YarnPackageResolver> logger) : IPackageResolver
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

        var yarnLockPath = Path.Combine(projectDir, "yarn.lock");
        List<ResolvedPackage> packages;

        if (File.Exists(yarnLockPath))
        {
            logger.LogDebug("Parsing yarn.lock at {Path}", yarnLockPath);
            var lockText = await File.ReadAllTextAsync(yarnLockPath, cancellationToken).ConfigureAwait(false);
            packages = ParseYarnLock(lockText, directDeps);
        }
        else
        {
            logger.LogDebug("No yarn.lock found, using direct dependencies only");
            packages = directDeps.Select(kvp => new ResolvedPackage
            {
                Name = kvp.Key,
                Version = kvp.Value,
                IsTransitive = false,
                Ecosystem = Ecosystems.JavaScript
            }).ToList();
        }

        logger.LogInformation("Resolved {Count} yarn packages from {Path}", packages.Count, packageJsonPath);
        return new ProjectPackages(packageJsonPath, packages);
    }

    /// <summary>
    /// Parse yarn.lock, auto-detecting Classic (v1) vs Berry (v2+) format.
    /// Berry format starts with "__metadata:" while Classic uses a comment header.
    /// </summary>
    internal static List<ResolvedPackage> ParseYarnLock(string lockText, Dictionary<string, string> directDeps)
    {
        // Yarn Berry v2+ starts with __metadata:
        if (lockText.Contains("__metadata:", StringComparison.Ordinal))
            return ParseYarnBerry(lockText, directDeps);

        return ParseYarnClassic(lockText, directDeps);
    }

    /// <summary>
    /// Parse Yarn Classic (v1) lock file format.
    /// Format:
    ///   "package@^version":
    ///     version "1.2.3"
    ///     resolved "..."
    /// </summary>
    internal static List<ResolvedPackage> ParseYarnClassic(string lockText, Dictionary<string, string> directDeps)
    {
        var packages = new List<ResolvedPackage>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? currentName = null;
        foreach (var rawLine in lockText.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');

            // Skip comments and empty lines
            if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line))
                continue;

            // Entry header: not indented, ends with ':'
            if (!line.StartsWith(' ') && line.EndsWith(':'))
            {
                currentName = ExtractPackageNameFromClassicHeader(line);
                continue;
            }

            // Version line inside an entry
            if (currentName is not null && line.TrimStart().StartsWith("version ", StringComparison.Ordinal))
            {
                var version = ExtractQuotedValue(line);
                if (version is not null && seen.Add(currentName))
                {
                    packages.Add(new ResolvedPackage
                    {
                        Name = currentName,
                        Version = version,
                        IsTransitive = !directDeps.ContainsKey(currentName),
                        Ecosystem = Ecosystems.JavaScript
                    });
                }
                currentName = null;
            }
        }

        return packages;
    }

    /// <summary>
    /// Parse Yarn Berry (v2+) lock file format (YAML-like).
    /// Format:
    ///   "package@npm:^version":
    ///     version: 1.2.3
    /// </summary>
    internal static List<ResolvedPackage> ParseYarnBerry(string lockText, Dictionary<string, string> directDeps)
    {
        var packages = new List<ResolvedPackage>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? currentName = null;
        foreach (var rawLine in lockText.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            // Entry header: starts with a quote, not indented
            if (!line.StartsWith(' ') && line.Contains('@', StringComparison.Ordinal))
            {
                currentName = ExtractPackageNameFromBerryHeader(line);
                continue;
            }

            // Version line
            if (currentName is not null && line.TrimStart().StartsWith("version:", StringComparison.Ordinal))
            {
                var version = line.Split(':', 2)[1].Trim().Trim('"');
                if (seen.Add(currentName))
                {
                    packages.Add(new ResolvedPackage
                    {
                        Name = currentName,
                        Version = version,
                        IsTransitive = !directDeps.ContainsKey(currentName),
                        Ecosystem = Ecosystems.JavaScript
                    });
                }
                currentName = null;
            }
        }

        return packages;
    }

    /// <summary>
    /// Extract package name from a Yarn Classic header line like:
    ///   "express@^4.18.0":
    ///   "@azure/identity@^3.0.0, @azure/identity@^3.1.0":
    /// </summary>
    internal static string? ExtractPackageNameFromClassicHeader(string line)
    {
        // Remove trailing ':'
        var header = line.TrimEnd(':');

        // Take the first descriptor (before any comma)
        var firstDesc = header.Split(',')[0].Trim().Trim('"');

        return ExtractNameBeforeVersion(firstDesc);
    }

    /// <summary>
    /// Extract package name from a Yarn Berry header line like:
    ///   "express@npm:^4.18.0":
    ///   "@azure/identity@npm:^3.0.0":
    /// </summary>
    internal static string? ExtractPackageNameFromBerryHeader(string line)
    {
        var header = line.TrimEnd(':');
        var firstDesc = header.Split(',')[0].Trim().Trim('"');

        return ExtractNameBeforeVersion(firstDesc);
    }

    /// <summary>
    /// Given "express@^4.18.0" or "@azure/identity@npm:^3.0.0", extract the package name.
    /// </summary>
    private static string? ExtractNameBeforeVersion(string descriptor)
    {
        if (string.IsNullOrEmpty(descriptor))
            return null;

        // Handle scoped packages: @scope/name@version
        int atIndex;
        if (descriptor.StartsWith('@'))
        {
            atIndex = descriptor.IndexOf('@', 1);
        }
        else
        {
            atIndex = descriptor.IndexOf('@', StringComparison.Ordinal);
        }

        return atIndex > 0 ? descriptor[..atIndex] : descriptor;
    }

    private static string? ExtractQuotedValue(string line)
    {
        var match = QuotedValueRegex().Match(line);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"""([^""]+)""")]
    private static partial Regex QuotedValueRegex();
}
