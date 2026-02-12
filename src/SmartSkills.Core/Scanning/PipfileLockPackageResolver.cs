using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Resolves Python packages from Pipfile.lock (JSON format).
/// </summary>
public sealed class PipfileLockPackageResolver(ILogger<PipfileLockPackageResolver> logger) : IPackageResolver
{
    public Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var projectDir = Directory.Exists(projectPath) ? projectPath : Path.GetDirectoryName(projectPath)!;
        var pipfileLockPath = Path.Combine(projectDir, "Pipfile.lock");

        if (!File.Exists(pipfileLockPath))
        {
            logger.LogDebug("No Pipfile.lock found at {Path}, returning empty", pipfileLockPath);
            return Task.FromResult(new ProjectPackages(projectPath, []));
        }

        var lockContent = File.ReadAllText(pipfileLockPath);
        var packages = ParsePipfileLock(lockContent);

        logger.LogInformation("Resolved {Count} Python packages from {Path}", packages.Count, pipfileLockPath);
        return Task.FromResult(new ProjectPackages(projectPath, packages));
    }

    internal static List<ResolvedPackage> ParsePipfileLock(string jsonContent)
    {
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;
        var packages = new List<ResolvedPackage>();

        ParseSection(root, "default", packages);
        ParseSection(root, "develop", packages);

        return packages;
    }

    private static void ParseSection(JsonElement root, string sectionName, List<ResolvedPackage> packages)
    {
        if (!root.TryGetProperty(sectionName, out var section))
            return;

        foreach (var entry in section.EnumerateObject())
        {
            var name = PypiNameNormalizer.Normalize(entry.Name);
            var version = "";

            if (entry.Value.TryGetProperty("version", out var verProp))
            {
                version = verProp.GetString() ?? "";
                // Strip leading == prefix
                if (version.StartsWith("=="))
                    version = version[2..];
            }

            packages.Add(new ResolvedPackage
            {
                Name = name,
                Version = version,
                IsTransitive = false, // Pipfile.lock is flat — all entries are direct
                Ecosystem = Ecosystems.Python
            });
        }
    }
}
