using SmartSkills.Core.Registry;
using Xunit;

namespace SmartSkills.Core.Tests;

public class RegistryIndexParserTests
{
    [Fact]
    public void Parse_ValidJson_ReturnsEntries()
    {
        var json = """
        {
          "skills": [
            {
              "packagePatterns": ["Newtonsoft.Json"],
              "skillPath": "skills/json"
            },
            {
              "packagePatterns": ["Microsoft.EntityFrameworkCore*", "Npgsql.EntityFrameworkCore*"],
              "skillPath": "skills/ef-core"
            }
          ]
        }
        """;

        var entries = RegistryIndexParser.Parse(json);

        Assert.Equal(2, entries.Count);
        Assert.Equal("skills/json", entries[0].SkillPath);
        Assert.Null(entries[0].RepoUrl);
        Assert.Single(entries[0].MatchCriteria);
        Assert.Equal("skills/ef-core", entries[1].SkillPath);
        Assert.Equal(2, entries[1].MatchCriteria.Count);
    }

    [Fact]
    public void Parse_EmptySkills_ReturnsEmpty()
    {
        var json = """{ "skills": [] }""";
        var entries = RegistryIndexParser.Parse(json);
        Assert.Empty(entries);
    }

    [Fact]
    public void Parse_MissingSkillsKey_ReturnsEmpty()
    {
        var json = """{ "other": "data" }""";
        var entries = RegistryIndexParser.Parse(json);
        Assert.Empty(entries);
    }

    [Fact]
    public void LoadEmbedded_ReturnsEntriesFromEmbeddedResource()
    {
        var entries = RegistryIndexParser.LoadEmbedded();
        Assert.NotNull(entries);
        Assert.NotEmpty(entries);
        Assert.Contains(entries, e => e.SkillPath == ".github/skills/azure-servicebus-dotnet");
        Assert.Contains(entries, e => e.MatchCriteria.Contains("Azure.Messaging.ServiceBus"));
        Assert.All(entries, e => Assert.NotNull(e.RepoUrl));
    }

    [Fact]
    public void LoadMerged_WithNoAdditionalPaths_ReturnsEmbeddedEntries()
    {
        var entries = RegistryIndexParser.LoadMerged();
        Assert.NotNull(entries);
    }

    [Fact]
    public void LoadMerged_WithAdditionalFile_MergesEntries()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, """
            {
              "skills": [
                { "packagePatterns": ["Extra.Package"], "skillPath": "skills/extra" }
              ]
            }
            """);

            var entries = RegistryIndexParser.LoadMerged([tempFile]);

            Assert.Contains(entries, e => e.SkillPath == "skills/extra");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_TopLevelRepoUrl_AppliedToAllSkills()
    {
        var json = """
        {
          "repoUrl": "https://github.com/org/repo",
          "skills": [
            { "packagePatterns": ["Pkg.A"], "skillPath": "skills/a" },
            { "packagePatterns": ["Pkg.B"], "skillPath": "skills/b" }
          ]
        }
        """;

        var entries = RegistryIndexParser.Parse(json);

        Assert.All(entries, e => Assert.Equal("https://github.com/org/repo", e.RepoUrl));
    }

    [Fact]
    public void Parse_PerSkillRepoUrl_OverridesTopLevel()
    {
        var json = """
        {
          "repoUrl": "https://github.com/org/default",
          "skills": [
            { "packagePatterns": ["Pkg.A"], "skillPath": "skills/a" },
            { "packagePatterns": ["Pkg.B"], "skillPath": "skills/b", "repoUrl": "https://github.com/org/custom" }
          ]
        }
        """;

        var entries = RegistryIndexParser.Parse(json);

        Assert.Equal("https://github.com/org/default", entries[0].RepoUrl);
        Assert.Equal("https://github.com/org/custom", entries[1].RepoUrl);
    }
}
