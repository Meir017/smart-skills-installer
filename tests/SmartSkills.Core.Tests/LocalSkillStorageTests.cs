using SmartSkills.Core.Installation;

namespace SmartSkills.Core.Tests;

public class LocalSkillStorageTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LocalSkillStorage _storage;

    public LocalSkillStorageTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SmartSkillsTests_" + Guid.NewGuid().ToString("N"));
        _storage = new LocalSkillStorage(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void RecordInstall_CreatesManifestAndTracksSkill()
    {
        _storage.RecordInstall("test-skill", "1.0.0", "github:owner/repo");

        Assert.True(_storage.IsInstalled("test-skill"));
        Assert.True(_storage.IsInstalled("test-skill", "1.0.0"));
        Assert.False(_storage.IsInstalled("test-skill", "2.0.0"));
    }

    [Fact]
    public void RecordUninstall_RemovesSkillAndDirectory()
    {
        var skillDir = _storage.GetSkillDir("test-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "file.txt"), "content");
        _storage.RecordInstall("test-skill", "1.0.0", "source");

        _storage.RecordUninstall("test-skill");

        Assert.False(_storage.IsInstalled("test-skill"));
        Assert.False(Directory.Exists(skillDir));
    }

    [Fact]
    public void LoadManifest_NoFile_ReturnsEmpty()
    {
        var manifest = _storage.LoadManifest();
        Assert.Empty(manifest.Skills);
    }

    [Fact]
    public void RecordInstall_UpdatesExistingVersion()
    {
        _storage.RecordInstall("test-skill", "1.0.0", "source");
        _storage.RecordInstall("test-skill", "2.0.0", "source");

        var manifest = _storage.LoadManifest();
        Assert.Single(manifest.Skills);
        Assert.Equal("2.0.0", manifest.Skills[0].Version);
    }

    [Fact]
    public void GetSkillDir_ReturnsCorrectPath()
    {
        var dir = _storage.GetSkillDir("my-skill");
        Assert.Equal(Path.Combine(_tempDir, "my-skill"), dir);
    }
}
