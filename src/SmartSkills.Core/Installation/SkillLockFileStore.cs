using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Installation;

/// <summary>
/// File-system-based implementation that reads and writes smart-skills.lock.json.
/// Includes one-time migration from the legacy .agents-skills-state.json.
/// </summary>
public sealed class SkillLockFileStore : ISkillLockFileStore
{
    internal const string LockFileName = "smart-skills.lock.json";
    private const string LegacyStateFileName = ".agents-skills-state.json";
    private readonly ILogger<SkillLockFileStore> _logger;

    private static readonly JsonSerializerOptions LegacyJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
            // Attempt migration from legacy state file
            var migrated = await TryMigrateFromLegacyAsync(projectRootPath, cancellationToken);
            if (migrated is not null)
                return migrated;

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

    private async Task<SkillsLockFile?> TryMigrateFromLegacyAsync(string projectRootPath, CancellationToken cancellationToken)
    {
        // The legacy state file lives in .agents/skills/
        var skillsDir = Path.Combine(projectRootPath, ".agents", "skills");
        var legacyPath = Path.Combine(skillsDir, LegacyStateFileName);

        if (!File.Exists(legacyPath))
            return null;

        _logger.LogInformation("Found legacy state file at {Path}, migrating to lock file...", legacyPath);

        var json = await File.ReadAllTextAsync(legacyPath, cancellationToken);
        var legacySkills = JsonSerializer.Deserialize<List<InstalledSkill>>(json, LegacyJsonOptions) ?? [];

        var lockFile = new SkillsLockFile();

        foreach (var skill in legacySkills)
        {
            var installDir = Path.Combine(skillsDir, skill.Name);
            var contentHash = Directory.Exists(installDir)
                ? SkillContentHasher.ComputeHash(installDir)
                : "sha256:unknown";

            lockFile.Skills[skill.Name] = new SkillLockEntry
            {
                RemoteUrl = skill.SourceUrl,
                SkillPath = skill.SourceUrl, // Legacy SourceUrl stored the skill path
                CommitSha = skill.CommitSha,
                LocalContentHash = contentHash
            };
        }

        // Save the migrated lock file
        await SaveAsync(projectRootPath, lockFile, cancellationToken);

        // Delete the legacy state file
        File.Delete(legacyPath);
        _logger.LogInformation("Migration complete. Deleted legacy state file. {Count} skills migrated.", legacySkills.Count);

        return lockFile;
    }
}
