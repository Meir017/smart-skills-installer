namespace SmartSkills.Core.Installation;

/// <summary>
/// Reads and writes the smart-skills.lock.json file.
/// </summary>
public interface ISkillLockFileStore
{
    /// <summary>
    /// Load the lock file from the project root. Returns an empty lock file if the file does not exist.
    /// </summary>
    Task<SkillsLockFile> LoadAsync(string projectRootPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save the lock file to the project root with deterministic JSON output.
    /// </summary>
    Task SaveAsync(string projectRootPath, SkillsLockFile lockFile, CancellationToken cancellationToken = default);
}
