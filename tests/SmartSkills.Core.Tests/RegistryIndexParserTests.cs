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
        Assert.Single(entries[0].PackagePatterns);
        Assert.Equal("skills/ef-core", entries[1].SkillPath);
        Assert.Equal(2, entries[1].PackagePatterns.Count);
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
}
