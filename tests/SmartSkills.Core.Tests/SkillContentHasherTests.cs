using SmartSkills.Core.Installation;
using Xunit;

namespace SmartSkills.Core.Tests;

public class SkillContentHasherTests : IDisposable
{
    private readonly string _tempDir;

    public SkillContentHasherTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"skill-hash-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void SameDirectory_ProducesIdenticalHash()
    {
        WriteFile("SKILL.md", "# Hello");

        var hash1 = SkillContentHasher.ComputeHash(_tempDir);
        var hash2 = SkillContentHasher.ComputeHash(_tempDir);

        Assert.Equal(hash1, hash2);
        Assert.StartsWith("sha256:", hash1, StringComparison.Ordinal);
    }

    [Fact]
    public void AddingFile_ChangesHash()
    {
        WriteFile("SKILL.md", "# Hello");
        var hash1 = SkillContentHasher.ComputeHash(_tempDir);

        WriteFile("scripts/setup.sh", "#!/bin/bash");
        var hash2 = SkillContentHasher.ComputeHash(_tempDir);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ModifyingFileContent_ChangesHash()
    {
        WriteFile("SKILL.md", "# Version 1");
        var hash1 = SkillContentHasher.ComputeHash(_tempDir);

        WriteFile("SKILL.md", "# Version 2");
        var hash2 = SkillContentHasher.ComputeHash(_tempDir);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void RenamingFile_ChangesHash()
    {
        WriteFile("file-a.md", "content");
        var hash1 = SkillContentHasher.ComputeHash(_tempDir);

        File.Delete(Path.Combine(_tempDir, "file-a.md"));
        WriteFile("file-b.md", "content");
        var hash2 = SkillContentHasher.ComputeHash(_tempDir);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HiddenFiles_AreExcluded()
    {
        WriteFile("SKILL.md", "# Hello");
        var hash1 = SkillContentHasher.ComputeHash(_tempDir);

        WriteFile(".hidden-file", "secret");
        var hash2 = SkillContentHasher.ComputeHash(_tempDir);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HiddenDirectories_AreExcluded()
    {
        WriteFile("SKILL.md", "# Hello");
        var hash1 = SkillContentHasher.ComputeHash(_tempDir);

        WriteFile(".git/config", "gitconfig");
        var hash2 = SkillContentHasher.ComputeHash(_tempDir);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void EmptyDirectory_ProducesConsistentHash()
    {
        var hash1 = SkillContentHasher.ComputeHash(_tempDir);
        var hash2 = SkillContentHasher.ComputeHash(_tempDir);

        Assert.Equal(hash1, hash2);
        Assert.StartsWith("sha256:", hash1, StringComparison.Ordinal);
    }

    [Fact]
    public void HashOutput_IsLowercaseHex()
    {
        WriteFile("SKILL.md", "test");
        var hash = SkillContentHasher.ComputeHash(_tempDir);

        // sha256: prefix + 64 lowercase hex chars
        Assert.Matches(@"^sha256:[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        Assert.Throws<DirectoryNotFoundException>(
            () => SkillContentHasher.ComputeHash(Path.Combine(_tempDir, "nonexistent")));
    }

    private void WriteFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_tempDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, System.Text.Encoding.UTF8.GetBytes(content));
    }
}
