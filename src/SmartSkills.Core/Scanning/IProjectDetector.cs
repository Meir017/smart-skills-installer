namespace SmartSkills.Core.Scanning;

/// <summary>
/// Describes a detected project in a directory.
/// </summary>
public record DetectedProject(string Ecosystem, string ProjectFilePath);

/// <summary>
/// Auto-detects project types in a directory.
/// </summary>
public interface IProjectDetector
{
    /// <summary>
    /// Inspect a directory and return all detected projects.
    /// May return multiple results for polyglot repositories.
    /// </summary>
    IReadOnlyList<DetectedProject> Detect(string directoryPath);
}
