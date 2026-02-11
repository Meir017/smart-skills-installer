using Microsoft.Extensions.Logging;
using SmartSkills.Core.Models;

namespace SmartSkills.Core.Scanning;

public class ProjectScanner : IProjectScanner
{
    private readonly CsprojParser _csprojParser = new();
    private readonly LockFileParser _lockFileParser = new();
    private readonly ILogger<ProjectScanner> _logger;

    public ProjectScanner(ILogger<ProjectScanner> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<DetectedPackage>> ScanAsync(string path, CancellationToken cancellationToken = default)
    {
        var resolvedPath = ProjectDiscovery.ResolvePath(path);
        var isSolution = resolvedPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase);
        var projectDir = isSolution ? Path.GetDirectoryName(resolvedPath)! : Path.GetDirectoryName(resolvedPath)!;

        // Try lock file first
        if (!isSolution)
        {
            var lockFilePath = Path.Combine(projectDir, "packages.lock.json");
            if (File.Exists(lockFilePath))
            {
                _logger.LogDebug("Found packages.lock.json, using lock file for dependency resolution");
                var result = _lockFileParser.Parse(lockFilePath);
                return Task.FromResult(result);
            }
        }

        // Fall back to .csproj parsing
        _logger.LogDebug("No lock file found, falling back to .csproj parsing");
        IReadOnlyList<DetectedPackage> packages = isSolution
            ? _csprojParser.ParseSolution(resolvedPath)
            : _csprojParser.Parse(resolvedPath);

        return Task.FromResult(packages);
    }
}
