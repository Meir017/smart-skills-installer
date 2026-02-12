using Microsoft.Extensions.Logging;
using Tomlyn;
using Tomlyn.Model;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Resolves Python packages from poetry.lock (TOML format).
/// </summary>
public sealed class PoetryLockPackageResolver(ILogger<PoetryLockPackageResolver> logger) : IPackageResolver
{
    public Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var projectDir = Directory.Exists(projectPath) ? projectPath : Path.GetDirectoryName(projectPath)!;
        var poetryLockPath = Path.Combine(projectDir, "poetry.lock");

        if (!File.Exists(poetryLockPath))
        {
            logger.LogDebug("No poetry.lock found at {Path}, returning empty", poetryLockPath);
            return Task.FromResult(new ProjectPackages(projectPath, []));
        }

        var directDeps = LoadDirectDependencies(projectDir);
        var lockContent = File.ReadAllText(poetryLockPath);
        var packages = ParsePoetryLock(lockContent, directDeps);

        logger.LogInformation("Resolved {Count} Python packages from {Path}", packages.Count, poetryLockPath);
        return Task.FromResult(new ProjectPackages(projectPath, packages));
    }

    internal static List<ResolvedPackage> ParsePoetryLock(string tomlContent, HashSet<string> directDeps)
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
            var isTransitive = !directDeps.Contains(normalized);

            packages.Add(new ResolvedPackage
            {
                Name = normalized,
                Version = version ?? "",
                IsTransitive = isTransitive,
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
