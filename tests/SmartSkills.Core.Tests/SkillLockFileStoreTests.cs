using SmartSkills.Core.Installation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SmartSkills.Core.Tests;

public class SkillLockFileStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SkillLockFileStore _store;

    public SkillLockFileStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"lockstore-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _store = new SkillLockFileStore(NullLogger<SkillLockFileStore>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task LoadAsync_NonExistentFile_ReturnsEmptyLockFile()
    {
        var result = await _store.LoadAsync(_tempDir, TestContext.Current.CancellationToken);

        Assert.Equal(1, result.Version);
        Assert.Empty(result.Skills);
    }

    [Fact]
    public async Task SaveThenLoad_RoundTrip_ProducesIdenticalObject()
    {
        var lockFile = new SkillsLockFile();
        lockFile.Skills["test-skill"] = new SkillLockEntry
        {
            RemoteUrl = "https://github.com/org/repo",
            SkillPath = "skills/test-skill",
            Language = "dotnet",
            CommitSha = "abc123",
            LocalContentHash = "sha256:def456"
        };

        await _store.SaveAsync(_tempDir, lockFile, TestContext.Current.CancellationToken);
        var loaded = await _store.LoadAsync(_tempDir, TestContext.Current.CancellationToken);

        Assert.Single(loaded.Skills);
        Assert.Equal("abc123", loaded.Skills["test-skill"].CommitSha);
    }

    [Fact]
    public async Task SaveAsync_OutputHasSortedKeys()
    {
        var lockFile = new SkillsLockFile();
        lockFile.Skills["z-skill"] = MakeEntry("z");
        lockFile.Skills["a-skill"] = MakeEntry("a");

        await _store.SaveAsync(_tempDir, lockFile, TestContext.Current.CancellationToken);

        var json = await File.ReadAllTextAsync(Path.Combine(_tempDir, SkillLockFileStore.LockFileName), TestContext.Current.CancellationToken);
        var aIndex = json.IndexOf("a-skill", StringComparison.Ordinal);
        var zIndex = json.IndexOf("z-skill", StringComparison.Ordinal);
        Assert.True(aIndex < zIndex, "Skills should be sorted alphabetically in output");
    }

    [Fact]
    public async Task LoadAsync_UnsupportedVersion_Throws()
    {
        var badJson = """{"version":99,"skills":{}}""";
        await File.WriteAllTextAsync(Path.Combine(_tempDir, SkillLockFileStore.LockFileName), badJson, TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _store.LoadAsync(_tempDir, TestContext.Current.CancellationToken));
        Assert.Contains("Unsupported lock file version", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsync_WritesNextToProjectRoot()
    {
        var lockFile = new SkillsLockFile();
        await _store.SaveAsync(_tempDir, lockFile, TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(_tempDir, SkillLockFileStore.LockFileName)));
    }

    private static SkillLockEntry MakeEntry(string suffix) => new()
    {
        RemoteUrl = $"https://github.com/org/repo-{suffix}",
        SkillPath = $"skills/skill-{suffix}",
        CommitSha = $"sha-{suffix}",
        LocalContentHash = $"sha256:hash-{suffix}"
    };
}
