using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartSkills.Core.Configuration;

public class SmartSkillsConfig
{
    [JsonPropertyName("sources")]
    public List<RegistrySourceConfig> Sources { get; set; } = [];

    [JsonPropertyName("installDir")]
    public string? InstallDir { get; set; }

    [JsonPropertyName("cacheTtlMinutes")]
    public int CacheTtlMinutes { get; set; } = 60;

    [JsonPropertyName("defaultBranch")]
    public string DefaultBranch { get; set; } = "main";
}

public class RegistrySourceConfig
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "github";

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("branch")]
    public string? Branch { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 100;
}

public static class ConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static string UserConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".smart-skills", "config.json");

    /// <summary>
    /// Loads configuration with precedence: CLI overrides > project-level > user-level > defaults.
    /// </summary>
    public static SmartSkillsConfig Load(string? projectDir = null, string? configFilePath = null)
    {
        var config = new SmartSkillsConfig();

        // 1. User-level
        MergeFromFile(config, UserConfigPath);

        // 2. Project-level
        if (projectDir is not null)
        {
            var projectConfig = Path.Combine(projectDir, ".skills-installer.json");
            MergeFromFile(config, projectConfig);
        }

        // 3. Explicit config file (CLI --config)
        if (configFilePath is not null)
        {
            MergeFromFile(config, configFilePath);
        }

        return config;
    }

    public static void Set(string key, string value, string? configFilePath = null)
    {
        var path = configFilePath ?? UserConfigPath;
        var config = LoadFromFile(path) ?? new SmartSkillsConfig();

        switch (key.ToLowerInvariant())
        {
            case "installdir":
                config.InstallDir = value;
                break;
            case "cachettlminutes":
                if (int.TryParse(value, out var ttl))
                    config.CacheTtlMinutes = ttl;
                break;
            case "defaultbranch":
                config.DefaultBranch = value;
                break;
            default:
                throw new ArgumentException($"Unknown configuration key: '{key}'");
        }

        SaveToFile(config, path);
    }

    public static string? Get(string key, string? configFilePath = null)
    {
        var config = Load(configFilePath: configFilePath);

        return key.ToLowerInvariant() switch
        {
            "installdir" => config.InstallDir,
            "cachettlminutes" => config.CacheTtlMinutes.ToString(),
            "defaultbranch" => config.DefaultBranch,
            "sources" => JsonSerializer.Serialize(config.Sources, JsonOptions),
            _ => throw new ArgumentException($"Unknown configuration key: '{key}'"),
        };
    }

    private static void MergeFromFile(SmartSkillsConfig target, string filePath)
    {
        var source = LoadFromFile(filePath);
        if (source is null) return;

        if (source.InstallDir is not null)
            target.InstallDir = source.InstallDir;
        if (source.CacheTtlMinutes != 60)
            target.CacheTtlMinutes = source.CacheTtlMinutes;
        if (source.DefaultBranch != "main")
            target.DefaultBranch = source.DefaultBranch;
        if (source.Sources.Count > 0)
            target.Sources = source.Sources;
    }

    private static SmartSkillsConfig? LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<SmartSkillsConfig>(json);
        }
        catch
        {
            return null;
        }
    }

    private static void SaveToFile(SmartSkillsConfig config, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(path, json);
    }
}
