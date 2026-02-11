using SmartSkills.Core.Models;
using SmartSkills.Core.Registry;

namespace SmartSkills.Core.Tests;

public class SkillMatchingEngineTests
{
    private readonly SkillMatchingEngine _engine = new();

    private static SkillManifest CreateManifest(string skillId, string name, params SkillTrigger[] triggers) => new()
    {
        SkillId = skillId,
        Name = name,
        Version = "1.0.0",
        Author = "test",
        Triggers = [.. triggers],
        InstallSteps = [new InstallStep { Action = "copy", Source = "a", Destination = "b" }]
    };

    [Fact]
    public void Match_ExactLibraryName_ReturnsCorrectSkill()
    {
        var libraries = new List<DetectedPackage> { new("Newtonsoft.Json", "13.0.1") };
        var manifests = new List<SkillManifest>
        {
            CreateManifest("json-skill", "JSON Skill",
                new SkillTrigger { LibraryPattern = "Newtonsoft.Json" })
        };

        var result = _engine.Match(libraries, new SkillRegistry(), manifests);

        Assert.Single(result);
        Assert.Equal("json-skill", result[0].SkillId);
        Assert.Contains("Newtonsoft.Json", result[0].MatchedLibraries);
    }

    [Fact]
    public void Match_GlobPattern_MatchesCorrectly()
    {
        var libraries = new List<DetectedPackage>
        {
            new("Microsoft.Extensions.Logging", "8.0.0"),
            new("Microsoft.Extensions.DependencyInjection", "8.0.0"),
        };
        var manifests = new List<SkillManifest>
        {
            CreateManifest("extensions-skill", "Extensions Skill",
                new SkillTrigger { LibraryPattern = "Microsoft.Extensions.*" })
        };

        var result = _engine.Match(libraries, new SkillRegistry(), manifests);

        Assert.Single(result);
        Assert.Equal(2, result[0].MatchedLibraries.Count);
    }

    [Fact]
    public void Match_VersionConstraints_FiltersIncompatible()
    {
        var libraries = new List<DetectedPackage> { new("PkgA", "5.0.0") };
        var manifests = new List<SkillManifest>
        {
            CreateManifest("skill-v7", "Skill for v7+",
                new SkillTrigger { LibraryPattern = "PkgA", MinVersion = "7.0.0" }),
            CreateManifest("skill-v3", "Skill for v3+",
                new SkillTrigger { LibraryPattern = "PkgA", MinVersion = "3.0.0", MaxVersion = "6.0.0" })
        };

        var result = _engine.Match(libraries, new SkillRegistry(), manifests);

        Assert.Single(result);
        Assert.Equal("skill-v3", result[0].SkillId);
    }

    [Fact]
    public void Match_DuplicateSkillsFromMultipleLibraries_Deduplicated()
    {
        var libraries = new List<DetectedPackage>
        {
            new("Microsoft.EntityFrameworkCore", "8.0.0"),
            new("Microsoft.EntityFrameworkCore.SqlServer", "8.0.0"),
        };
        var manifests = new List<SkillManifest>
        {
            CreateManifest("ef-skill", "EF Skill",
                new SkillTrigger { LibraryPattern = "Microsoft.EntityFrameworkCore" },
                new SkillTrigger { LibraryPattern = "Microsoft.EntityFrameworkCore.*" })
        };

        var result = _engine.Match(libraries, new SkillRegistry(), manifests);

        // Should be only one resolved skill (deduplicated by skillId)
        Assert.Single(result);
        Assert.Equal(2, result[0].MatchedLibraries.Count);
    }

    [Fact]
    public void Match_NoMatchingLibraries_ReturnsEmpty()
    {
        var libraries = new List<DetectedPackage> { new("SomePackage", "1.0.0") };
        var manifests = new List<SkillManifest>
        {
            CreateManifest("other-skill", "Other Skill",
                new SkillTrigger { LibraryPattern = "CompletelyDifferent" })
        };

        var result = _engine.Match(libraries, new SkillRegistry(), manifests);
        Assert.Empty(result);
    }

    [Fact]
    public void Match_OrderedByRelevance_MostMatchesFirst()
    {
        var libraries = new List<DetectedPackage>
        {
            new("Microsoft.Extensions.Logging", "8.0.0"),
            new("Microsoft.Extensions.DependencyInjection", "8.0.0"),
            new("Newtonsoft.Json", "13.0.1"),
        };
        var manifests = new List<SkillManifest>
        {
            CreateManifest("json-skill", "JSON Skill",
                new SkillTrigger { LibraryPattern = "Newtonsoft.Json" }),
            CreateManifest("ext-skill", "Extensions Skill",
                new SkillTrigger { LibraryPattern = "Microsoft.Extensions.*" })
        };

        var result = _engine.Match(libraries, new SkillRegistry(), manifests);

        Assert.Equal(2, result.Count);
        // ext-skill matches 2 libraries, json-skill matches 1
        Assert.Equal("ext-skill", result[0].SkillId);
        Assert.Equal("json-skill", result[1].SkillId);
    }

    [Theory]
    [InlineData("5.0.0", "3.0.0", null, true)]
    [InlineData("5.0.0", "7.0.0", null, false)]
    [InlineData("5.0.0", null, "6.0.0", true)]
    [InlineData("5.0.0", null, "4.0.0", false)]
    [InlineData("5.0.0", "3.0.0", "6.0.0", true)]
    [InlineData("5.0.0-beta", "3.0.0", null, true)]
    [InlineData(null, "3.0.0", null, true)]
    public void IsVersionCompatible_VariousScenarios(string? libVersion, string? min, string? max, bool expected)
    {
        Assert.Equal(expected, SkillMatchingEngine.IsVersionCompatible(libVersion, min, max));
    }
}
