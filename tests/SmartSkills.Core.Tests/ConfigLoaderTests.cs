using SmartSkills.Core.Configuration;

namespace SmartSkills.Core.Tests;

public class ConfigLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public ConfigLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SmartSkillsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Load_NoConfigFiles_ReturnsDefaults()
    {
        var config = ConfigLoader.Load(projectDir: _tempDir);

        Assert.Equal(60, config.CacheTtlMinutes);
        Assert.Equal("main", config.DefaultBranch);
        Assert.Null(config.InstallDir);
        Assert.Empty(config.Sources);
    }

    [Fact]
    public void Load_ProjectConfig_OverridesDefaults()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".skills-installer.json"), """
            {
              "installDir": "/custom/path",
              "cacheTtlMinutes": 30,
              "defaultBranch": "develop"
            }
            """);

        var config = ConfigLoader.Load(projectDir: _tempDir);

        Assert.Equal("/custom/path", config.InstallDir);
        Assert.Equal(30, config.CacheTtlMinutes);
        Assert.Equal("develop", config.DefaultBranch);
    }

    [Fact]
    public void Load_ExplicitConfigFile_OverridesAll()
    {
        var explicitConfig = Path.Combine(_tempDir, "custom-config.json");
        File.WriteAllText(explicitConfig, """
            {
              "cacheTtlMinutes": 5,
              "defaultBranch": "release"
            }
            """);

        var config = ConfigLoader.Load(configFilePath: explicitConfig);

        Assert.Equal(5, config.CacheTtlMinutes);
        Assert.Equal("release", config.DefaultBranch);
    }

    [Fact]
    public void SetAndGet_RoundTrip()
    {
        var configPath = Path.Combine(_tempDir, "test-config.json");

        ConfigLoader.Set("installDir", "/my/dir", configPath);
        var result = ConfigLoader.Get("installDir", configPath);

        Assert.Equal("/my/dir", result);
    }

    [Fact]
    public void Set_UnknownKey_Throws()
    {
        var configPath = Path.Combine(_tempDir, "test-config.json");

        Assert.Throws<ArgumentException>(() => ConfigLoader.Set("unknownKey", "value", configPath));
    }
}
