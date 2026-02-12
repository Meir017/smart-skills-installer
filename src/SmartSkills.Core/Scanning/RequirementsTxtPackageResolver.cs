using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Resolves Python packages from requirements.txt (plain text format).
/// </summary>
public sealed partial class RequirementsTxtPackageResolver(ILogger<RequirementsTxtPackageResolver> logger) : IPackageResolver
{
    [GeneratedRegex(@"^([A-Za-z0-9]([A-Za-z0-9._-]*[A-Za-z0-9])?)")]
    private static partial Regex PackageNamePattern();

    [GeneratedRegex(@"\[.*?\]")]
    private static partial Regex ExtrasPattern();

    [GeneratedRegex(@"[><=!~;@].*$")]
    private static partial Regex VersionSpecPattern();

    public Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var projectDir = Directory.Exists(projectPath) ? projectPath : Path.GetDirectoryName(projectPath)!;
        var requirementsPath = Path.Combine(projectDir, "requirements.txt");

        if (!File.Exists(requirementsPath))
        {
            logger.LogDebug("No requirements.txt found at {Path}, returning empty", requirementsPath);
            return Task.FromResult(new ProjectPackages(projectPath, []));
        }

        var packages = new List<ResolvedPackage>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ParseRequirementsFile(requirementsPath, packages, seen);

        logger.LogInformation("Resolved {Count} Python packages from {Path}", packages.Count, requirementsPath);
        return Task.FromResult(new ProjectPackages(projectPath, packages));
    }

    internal static void ParseRequirementsFile(string filePath, List<ResolvedPackage> packages, HashSet<string> seen)
    {
        if (!File.Exists(filePath))
            return;

        var lines = File.ReadAllLines(filePath);
        var baseDir = Path.GetDirectoryName(filePath)!;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            // Handle inline comments
            var commentIdx = line.IndexOf('#', StringComparison.Ordinal);
            if (commentIdx > 0)
                line = line[..commentIdx].Trim();

            // Handle -r / --requirement includes
            if (line.StartsWith("-r ", StringComparison.Ordinal) || line.StartsWith("--requirement ", StringComparison.Ordinal))
            {
                var includePath = line.StartsWith("-r ", StringComparison.Ordinal) ? line[3..].Trim() : line["--requirement ".Length..].Trim();
                var resolvedPath = Path.IsPathRooted(includePath)
                    ? includePath
                    : Path.Combine(baseDir, includePath);
                ParseRequirementsFile(resolvedPath, packages, seen);
                continue;
            }

            // Skip other options (-i, --index-url, -f, --find-links, -e, etc.)
            if (line.StartsWith('-'))
                continue;

            var parsed = ParseRequirementLine(line);
            if (parsed is not null && seen.Add(parsed.Value.Name))
            {
                packages.Add(new ResolvedPackage
                {
                    Name = parsed.Value.Name,
                    Version = parsed.Value.Version,
                    IsTransitive = false, // requirements.txt has no dependency graph
                    Ecosystem = Ecosystems.Python
                });
            }
        }
    }

    internal static (string Name, string Version)? ParseRequirementLine(string line)
    {
        // Strip extras like [security,tests]
        var stripped = ExtrasPattern().Replace(line, "");

        // Match the package name
        var match = PackageNamePattern().Match(stripped);
        if (!match.Success)
            return null;

        var name = PypiNameNormalizer.Normalize(match.Groups[1].Value);
        var rest = stripped[match.Length..].Trim();

        // Extract version: "==1.2.3" → "1.2.3", ">=1.0,<2.0" → ">=1.0,<2.0"
        var version = "";
        if (rest.StartsWith("==", StringComparison.Ordinal))
            version = rest[2..].Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        else if (rest.Length > 0 && (rest[0] == '>' || rest[0] == '<' || rest[0] == '~' || rest[0] == '!'))
            version = VersionSpecPattern().Match(rest).Value;

        return (name, version);
    }
}
