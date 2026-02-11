using System.Security.Cryptography;
using SmartSkills.Core.Fetching;

namespace SmartSkills.Core.Tests;

public class SkillPackageDownloaderTests : IDisposable
{
    private readonly string _tempDir;

    public SkillPackageDownloaderTests()
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
    public void VerifyChecksum_MatchingHash_Succeeds()
    {
        var filePath = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(filePath, "hello world");

        using var stream = File.OpenRead(filePath);
        var expectedHash = Convert.ToHexString(SHA256.HashData(stream));

        // Should not throw
        SkillPackageDownloader.VerifyChecksum(filePath, expectedHash);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void VerifyChecksum_MismatchedHash_ThrowsAndDeletesFile()
    {
        var filePath = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(filePath, "hello world");

        var ex = Assert.Throws<InvalidOperationException>(
            () => SkillPackageDownloader.VerifyChecksum(filePath, "0000000000000000000000000000000000000000000000000000000000000000"));

        Assert.Contains("Checksum mismatch", ex.Message);
        Assert.False(File.Exists(filePath)); // file should be deleted
    }
}
