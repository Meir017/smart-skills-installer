using SmartSkills.Core.Registry;
using Xunit;

namespace SmartSkills.Core.Tests;

public class RegistryLanguageParsingTests
{
    [Fact]
    public void Parse_WithLanguageField_SetsLanguage()
    {
        var json = """
        {
          "skills": [
            { "packagePatterns": ["express"], "skillPath": "skills/express", "language": "npm" }
          ]
        }
        """;

        var entries = RegistryIndexParser.Parse(json);

        Assert.Single(entries);
        Assert.Equal("npm", entries[0].Language);
    }

    [Fact]
    public void Parse_WithoutLanguageField_LanguageIsNull()
    {
        var json = """
        {
          "skills": [
            { "packagePatterns": ["SomePackage"], "skillPath": "skills/some" }
          ]
        }
        """;

        var entries = RegistryIndexParser.Parse(json);

        Assert.Single(entries);
        Assert.Null(entries[0].Language);
    }

    [Fact]
    public void Parse_TopLevelLanguage_InheritedBySkills()
    {
        var json = """
        {
          "language": "npm",
          "skills": [
            { "packagePatterns": ["express"], "skillPath": "skills/express" },
            { "packagePatterns": ["react"], "skillPath": "skills/react" }
          ]
        }
        """;

        var entries = RegistryIndexParser.Parse(json);

        Assert.All(entries, e => Assert.Equal("npm", e.Language));
    }

    [Fact]
    public void Parse_PerSkillLanguage_OverridesTopLevel()
    {
        var json = """
        {
          "language": "dotnet",
          "skills": [
            { "packagePatterns": ["SomePackage"], "skillPath": "skills/dotnet-skill" },
            { "packagePatterns": ["express"], "skillPath": "skills/express", "language": "npm" }
          ]
        }
        """;

        var entries = RegistryIndexParser.Parse(json);

        Assert.Equal("dotnet", entries[0].Language);
        Assert.Equal("npm", entries[1].Language);
    }

    [Fact]
    public void LoadEmbedded_ContainsNpmEntries()
    {
        var entries = RegistryIndexParser.LoadEmbedded();

        Assert.Contains(entries, e => e.Language == "npm");
        Assert.Contains(entries, e => e.PackagePatterns.Contains("@azure/identity"));
    }

    [Fact]
    public void LoadEmbedded_DotnetEntries_HaveNullLanguage()
    {
        var entries = RegistryIndexParser.LoadEmbedded();

        // Existing .NET entries should have null language (backward compat)
        var dotnetEntry = entries.First(e => e.SkillPath == ".github/skills/azure-servicebus-dotnet");
        Assert.Null(dotnetEntry.Language);
    }
}
