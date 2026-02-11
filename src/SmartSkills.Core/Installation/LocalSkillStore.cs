using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Installation;

/// <summary>
/// File-system-based skill store that persists installed skill state as JSON.
/// </summary>
public sealed class LocalSkillStore : ISkillStore
{
    private readonly string _baseDirectory;
    private readonly ILogger<LocalSkillStore> _logger;
    private const string StateFileName = ".agents-skills-state.json";

    public LocalSkillStore(string baseDirectory, ILogger<LocalSkillStore> logger)
    {
        _baseDirectory = baseDirectory;
        _logger = logger;
    }

    private string StateFilePath => Path.Combine(_baseDirectory, StateFileName);

    public async Task<IReadOnlyList<InstalledSkill>> GetInstalledSkillsAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(StateFilePath))
            return [];

        var json = await File.ReadAllTextAsync(StateFilePath, cancellationToken);
        return JsonSerializer.Deserialize<List<InstalledSkill>>(json, JsonOptions) ?? [];
    }

    public async Task<InstalledSkill?> GetByNameAsync(string skillName, CancellationToken cancellationToken = default)
    {
        var skills = await GetInstalledSkillsAsync(cancellationToken);
        return skills.FirstOrDefault(s => string.Equals(s.Name, skillName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task SaveAsync(InstalledSkill skill, CancellationToken cancellationToken = default)
    {
        var skills = (await GetInstalledSkillsAsync(cancellationToken)).ToList();
        skills.RemoveAll(s => string.Equals(s.Name, skill.Name, StringComparison.OrdinalIgnoreCase));
        skills.Add(skill);

        Directory.CreateDirectory(_baseDirectory);
        var json = JsonSerializer.Serialize(skills, JsonOptions);
        await File.WriteAllTextAsync(StateFilePath, json, cancellationToken);

        _logger.LogDebug("Saved skill state for {SkillName}", skill.Name);
    }

    public async Task RemoveAsync(string skillName, CancellationToken cancellationToken = default)
    {
        var skills = (await GetInstalledSkillsAsync(cancellationToken)).ToList();
        skills.RemoveAll(s => string.Equals(s.Name, skillName, StringComparison.OrdinalIgnoreCase));

        var skillDir = Path.Combine(_baseDirectory, skillName);
        if (Directory.Exists(skillDir))
        {
            Directory.Delete(skillDir, recursive: true);
            _logger.LogInformation("Removed skill directory: {SkillDir}", skillDir);
        }

        var json = JsonSerializer.Serialize(skills, JsonOptions);
        await File.WriteAllTextAsync(StateFilePath, json, cancellationToken);

        _logger.LogInformation("Removed skill: {SkillName}", skillName);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
