using SmartSkills.Core.Models;

namespace SmartSkills.Core.Scanning;

public interface IProjectScanner
{
    /// <summary>
    /// Scans the given path for a .NET project or solution and returns detected packages.
    /// </summary>
    Task<IReadOnlyList<DetectedPackage>> ScanAsync(string path, CancellationToken cancellationToken = default);
}
