using SmartSkills.Core.Installation;
using Xunit;

namespace SmartSkills.Core.Tests;

public class SkillsLockFileSerializerTests
{
    [Fact]
    public void Serialize_SortsSkillKeysAlphabetically()
    {
        var lockFile = new SkillsLockFile();
        lockFile.Skills["zebra-skill"] = MakeEntry("z");
        lockFile.Skills["alpha-skill"] = MakeEntry("a");

        var json = SkillsLockFileSerializer.Serialize(lockFile);

        var alphaIndex = json.IndexOf("alpha-skill", StringComparison.Ordinal);
        var zebraIndex = json.IndexOf("zebra-skill", StringComparison.Ordinal);
        Assert.True(alphaIndex < zebraIndex, "Skills should be sorted alphabetically");
    }

    [Fact]
    public void RoundTrip_ProducesIdenticalObject()
    {
        var lockFile = new SkillsLockFile();
        lockFile.Skills["my-skill"] = new SkillLockEntry
        {
            RemoteUrl = "https://github.com/org/repo",
            SkillPath = "skills/my-skill",
            Language = "dotnet",
            CommitSha = "abc123",
            LocalContentHash = "sha256:def456"
        };

        var json = SkillsLockFileSerializer.Serialize(lockFile);
        var result = SkillsLockFileSerializer.Deserialize(json);

        Assert.Equal(1, result.Version);
        Assert.Single(result.Skills);
        var entry = result.Skills["my-skill"];
        Assert.Equal("https://github.com/org/repo", entry.RemoteUrl);
        Assert.Equal("skills/my-skill", entry.SkillPath);
        Assert.Equal("dotnet", entry.Language);
        Assert.Equal("abc123", entry.CommitSha);
        Assert.Equal("sha256:def456", entry.LocalContentHash);
    }

    [Fact]
    public void Deserialize_RejectsUnsupportedVersion()
    {
        var json = """{"version":99,"skills":{}}""";

        var ex = Assert.Throws<InvalidOperationException>(
            () => SkillsLockFileSerializer.Deserialize(json));
        Assert.Contains("Unsupported lock file version: 99", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_OmitsNullLanguage()
    {
        var lockFile = new SkillsLockFile();
        lockFile.Skills["test"] = new SkillLockEntry
        {
            RemoteUrl = "https://github.com/org/repo",
            SkillPath = "skills/test",
            Language = null,
            CommitSha = "abc",
            LocalContentHash = "sha256:def"
        };

        var json = SkillsLockFileSerializer.Serialize(lockFile);

        Assert.DoesNotContain("language", json, StringComparison.Ordinal);
    }

    [Fact]
    public void RoundTrip_ProducesIdenticalJson()
    {
        var lockFile = new SkillsLockFile();
        lockFile.Skills["b-skill"] = MakeEntry("b");
        lockFile.Skills["a-skill"] = MakeEntry("a");

        var json1 = SkillsLockFileSerializer.Serialize(lockFile);
        var deserialized = SkillsLockFileSerializer.Deserialize(json1);
        var json2 = SkillsLockFileSerializer.Serialize(deserialized);

        Assert.Equal(json1, json2);
    }

    [Fact]
    public void Deserialize_ThrowsOnEmptyString()
    {
        Assert.Throws<ArgumentException>(
            () => SkillsLockFileSerializer.Deserialize(""));
    }

    private static SkillLockEntry MakeEntry(string suffix) => new()
    {
        RemoteUrl = $"https://github.com/org/repo-{suffix}",
        SkillPath = $"skills/skill-{suffix}",
        CommitSha = $"sha-{suffix}",
        LocalContentHash = $"sha256:hash-{suffix}"
    };
}
