using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartSkills.Core.Installation;

public class InstalledSkillEntry
{
    [JsonPropertyName("skillId")]
    public string SkillId { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("installedAt")]
    public DateTimeOffset InstalledAt { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;
}

public class InstalledSkillsManifest
{
    [JsonPropertyName("skills")]
    public List<InstalledSkillEntry> Skills { get; set; } = [];
}

public class LocalSkillStorage
{
    private readonly string _baseDir;
    private readonly string _manifestPath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public LocalSkillStorage(string? baseDir = null)
    {
        _baseDir = baseDir
                   ?? Environment.GetEnvironmentVariable("SMART_SKILLS_HOME")
                   ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".smart-skills");
        _manifestPath = Path.Combine(_baseDir, "installed-skills.json");
    }

    public string BaseDir => _baseDir;

    public string GetSkillDir(string skillId) => Path.Combine(_baseDir, skillId);

    public InstalledSkillsManifest LoadManifest()
    {
        if (!File.Exists(_manifestPath))
            return new InstalledSkillsManifest();

        var json = File.ReadAllText(_manifestPath);
        return JsonSerializer.Deserialize<InstalledSkillsManifest>(json) ?? new InstalledSkillsManifest();
    }

    public void SaveManifest(InstalledSkillsManifest manifest)
    {
        Directory.CreateDirectory(_baseDir);
        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        File.WriteAllText(_manifestPath, json);
    }

    public bool IsInstalled(string skillId, string? version = null)
    {
        var manifest = LoadManifest();
        var entry = manifest.Skills.Find(s => s.SkillId.Equals(skillId, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            return false;
        return version is null || entry.Version.Equals(version, StringComparison.OrdinalIgnoreCase);
    }

    public void RecordInstall(string skillId, string version, string source)
    {
        var manifest = LoadManifest();
        manifest.Skills.RemoveAll(s => s.SkillId.Equals(skillId, StringComparison.OrdinalIgnoreCase));
        manifest.Skills.Add(new InstalledSkillEntry
        {
            SkillId = skillId,
            Version = version,
            InstalledAt = DateTimeOffset.UtcNow,
            Source = source,
        });
        SaveManifest(manifest);
    }

    public void RecordUninstall(string skillId)
    {
        var manifest = LoadManifest();
        manifest.Skills.RemoveAll(s => s.SkillId.Equals(skillId, StringComparison.OrdinalIgnoreCase));
        SaveManifest(manifest);

        var skillDir = GetSkillDir(skillId);
        if (Directory.Exists(skillDir))
            Directory.Delete(skillDir, true);
    }
}
