using SmartSkills.Core.Registry;
using Xunit;

namespace SmartSkills.Core.Tests;

public class RegistryStrategyParsingTests
{
    [Fact]
    public void Parse_LegacyFormat_DefaultsToPackageStrategy()
    {
        var json = """
        {
            "repoUrl": "https://github.com/org/repo",
            "skills": [
                {
                    "type": "package",
                    "matchCriteria": ["Azure.Identity"],
                    "skillPath": ".github/skills/azure-identity-dotnet",
                    "language": "dotnet"
                }
            ]
        }
        """;

        var entries = RegistryIndexParser.Parse(json);

        Assert.Single(entries);
        Assert.Equal("package", entries[0].Type);
        Assert.Equal(["Azure.Identity"], entries[0].MatchCriteria);
    }

    [Fact]
    public void Parse_NewFormat_UsesTypeAndCriteria()
    {
        var json = """
        {
            "repoUrl": "https://github.com/org/repo",
            "skills": [
                {
                    "type": "file-exists",
                    "matchCriteria": ["*.sln", "global.json"],
                    "skillPath": "skills/nuget-manager",
                    "language": "dotnet"
                }
            ]
        }
        """;

        var entries = RegistryIndexParser.Parse(json);

        Assert.Single(entries);
        Assert.Equal("file-exists", entries[0].Type);
        Assert.Equal(["*.sln", "global.json"], entries[0].MatchCriteria);
        Assert.Equal("skills/nuget-manager", entries[0].SkillPath);
        Assert.Equal("dotnet", entries[0].Language);
    }

    [Fact]
    public void Parse_EntryWithoutType_IsSkipped()
    {
        var json = """
        {
            "repoUrl": "https://github.com/org/repo",
            "skills": [
                {
                    "skillPath": "skills/orphan"
                }
            ]
        }
        """;

        var entries = RegistryIndexParser.Parse(json);

        Assert.Empty(entries);
    }

    [Fact]
    public void Parse_MixedFormats_ParsesBoth()
    {
        var json = """
        {
            "repoUrl": "https://github.com/org/repo",
            "skills": [
                {
                    "type": "package",
                    "matchCriteria": ["Azure.Identity"],
                    "skillPath": ".github/skills/azure-identity",
                    "language": "dotnet"
                },
                {
                    "type": "file-exists",
                    "matchCriteria": ["*.sln"],
                    "skillPath": "skills/nuget-manager",
                    "language": "dotnet"
                }
            ]
        }
        """;

        var entries = RegistryIndexParser.Parse(json);

        Assert.Equal(2, entries.Count);
        Assert.Equal("package", entries[0].Type);
        Assert.Equal("file-exists", entries[1].Type);
    }

    [Fact]
    public void Parse_TopLevelType_NotInheritedByEntries()
    {
        var json = """
        {
            "repoUrl": "https://github.com/org/repo",
            "skills": [
                {
                    "type": "file-exists",
                    "matchCriteria": ["*.sln"],
                    "skillPath": "skills/nuget-manager",
                    "language": "dotnet"
                }
            ]
        }
        """;

        var entries = RegistryIndexParser.Parse(json);

        Assert.Single(entries);
        Assert.Equal("file-exists", entries[0].Type);
    }

    [Fact]
    public void LoadEmbedded_ContainsNugetManagerEntry()
    {
        var entries = RegistryIndexParser.LoadEmbedded();

        var nugetManager = entries.FirstOrDefault(e => e.SkillPath == "skills/nuget-manager");
        Assert.NotNull(nugetManager);
        Assert.Equal("file-exists", nugetManager.Type);
        Assert.Contains("*.sln", nugetManager.MatchCriteria);
        Assert.Contains("*.slnx", nugetManager.MatchCriteria);
        Assert.Contains("global.json", nugetManager.MatchCriteria);
        Assert.Contains("Directory.Build.props", nugetManager.MatchCriteria);
        Assert.Contains("Directory.Packages.props", nugetManager.MatchCriteria);
        Assert.Equal("dotnet", nugetManager.Language);
    }

    [Fact]
    public void LoadEmbedded_ExistingEntriesStillParse()
    {
        var entries = RegistryIndexParser.LoadEmbedded();

        // Verify existing package-based entries still work
        var azureServiceBus = entries.FirstOrDefault(e => e.MatchCriteria.Contains("Azure.Messaging.ServiceBus"));
        Assert.NotNull(azureServiceBus);
        Assert.Equal("package", azureServiceBus.Type);
    }
}
