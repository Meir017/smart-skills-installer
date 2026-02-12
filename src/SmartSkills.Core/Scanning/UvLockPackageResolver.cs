using Microsoft.Extensions.Logging;
using Tomlyn;
using Tomlyn.Model;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Resolves Python packages from uv.lock (TOML format).
/// </summary>
public sealed class UvLockPackageResolver(ILogger<UvLockPackageResolver> logger) : IPackageResolver
{
    public Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var projectDir = Directory.Exists(projectPath) ? projectPath : Path.GetDirectoryName(projectPath)!;
        var uvLockPath = Path.Combine(projectDir, "uv.lock");

        if (!File.Exists(uvLockPath))
        {
            logger.LogDebug("No uv.lock found at {Path}, returning empty", uvLockPath);
            return Task.FromResult(new ProjectPackages(projectPath, []));
        }

        var directDeps = LoadDirectDependencies(projectDir);
        var lockContent = File.ReadAllText(uvLockPath);
        var packages = ParseUvLock(lockContent, directDeps);

        logger.LogInformation("Resolved {Count} Python packages from {Path}", packages.Count, uvLockPath);
        return Task.FromResult(new ProjectPackages(projectPath, packages));
    }

    internal static List<ResolvedPackage> ParseUvLock(string tomlContent, HashSet<string> directDeps)
    {
        var model = Toml.ToModel(tomlContent);
        var packages = new List<ResolvedPackage>();

        if (!model.TryGetValue("package", out var packageObj) || packageObj is not TomlTableArray packageArray)
            return packages;

        foreach (TomlTable entry in packageArray)
        {
            var name = entry.TryGetValue("name", out var nameObj) ? nameObj?.ToString() : null;
            var version = entry.TryGetValue("version", out var verObj) ? verObj?.ToString() : null;

            if (string.IsNullOrWhiteSpace(name))
                continue;

            var normalized = PypiNameNormalizer.Normalize(name);
            packages.Add(new ResolvedPackage
            {
                Name = normalized,
                Version = version ?? "",
                IsTransitive = !directDeps.Contains(normalized),
                Ecosystem = Ecosystems.Python
            });
        }

        return packages;
    }

    private static HashSet<string> LoadDirectDependencies(string projectDir)
    {
        var pyprojectPath = Path.Combine(projectDir, "pyproject.toml");
        if (File.Exists(pyprojectPath))
            return PyprojectParser.ParseDirectDependencies(File.ReadAllText(pyprojectPath));

        return [];
    }
}
