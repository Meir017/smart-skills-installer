using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Configuration;

/// <summary>
/// Loads configuration from user-level and project-level JSON files with merging.
/// </summary>
public sealed class ConfigProvider : IConfigProvider
{
    private readonly string? _overridePath;
    private readonly ILogger<ConfigProvider> _logger;
    private const string ConfigFileName = "smartskills.json";

    public ConfigProvider(string? overridePath, ILogger<ConfigProvider> logger)
    {
        _overridePath = overridePath;
        _logger = logger;
    }

    public async Task<SmartSkillsConfig> LoadAsync(string? projectPath = null, CancellationToken cancellationToken = default)
    {
        // Priority: CLI override > project-level > user-level
        SmartSkillsConfig config = new();

        // 1. User-level
        var userConfigPath = GetUserConfigPath();
        if (userConfigPath is not null)
            config = await MergeFromFileAsync(config, userConfigPath, cancellationToken);

        // 2. Project-level
        if (projectPath is not null)
        {
            var dir = File.Exists(projectPath) ? Path.GetDirectoryName(projectPath)! : projectPath;
            var projectConfigPath = Path.Combine(dir, ConfigFileName);
            config = await MergeFromFileAsync(config, projectConfigPath, cancellationToken);
        }

        // 3. CLI override
        if (_overridePath is not null)
            config = await MergeFromFileAsync(config, _overridePath, cancellationToken);

        return config;
    }

    private async Task<SmartSkillsConfig> MergeFromFileAsync(SmartSkillsConfig current, string path, CancellationToken ct)
    {
        if (!File.Exists(path))
        {
            _logger.LogDebug("Config file not found: {Path}", path);
            return current;
        }

        _logger.LogDebug("Loading config from: {Path}", path);
        var json = await File.ReadAllTextAsync(path, ct);
        var loaded = JsonSerializer.Deserialize<SmartSkillsConfig>(json, JsonOptions);

        if (loaded is null) return current;

        return new SmartSkillsConfig
        {
            Sources = loaded.Sources.Count > 0 ? loaded.Sources : current.Sources,
            SkillsOutputDirectory = loaded.SkillsOutputDirectory ?? current.SkillsOutputDirectory
        };
    }

    private static string? GetUserConfigPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrEmpty(appData)) return null;
        return Path.Combine(appData, "smartskills", ConfigFileName);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
}
