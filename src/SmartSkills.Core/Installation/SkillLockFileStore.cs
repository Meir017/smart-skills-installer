using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Installation;

/// <summary>
/// File-system-based implementation that reads and writes smart-skills.lock.json.
/// </summary>
public sealed class SkillLockFileStore : ISkillLockFileStore
{
    internal const string LockFileName = "smart-skills.lock.json";
    private readonly ILogger<SkillLockFileStore> _logger;

    public SkillLockFileStore(ILogger<SkillLockFileStore> logger)
    {
        _logger = logger;
    }

    public async Task<SkillsLockFile> LoadAsync(string projectRootPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRootPath);

        var filePath = Path.Combine(projectRootPath, LockFileName);

        if (!File.Exists(filePath))
        {
            _logger.LogDebug("Lock file not found at {Path}, returning empty lock file", filePath);
            return new SkillsLockFile();
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        _logger.LogDebug("Loaded lock file from {Path}", filePath);
        return SkillsLockFileSerializer.Deserialize(json);
    }

    public async Task SaveAsync(string projectRootPath, SkillsLockFile lockFile, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRootPath);
        ArgumentNullException.ThrowIfNull(lockFile);

        var filePath = Path.Combine(projectRootPath, LockFileName);
        var json = SkillsLockFileSerializer.Serialize(lockFile);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        _logger.LogDebug("Saved lock file to {Path}", filePath);
    }
}
