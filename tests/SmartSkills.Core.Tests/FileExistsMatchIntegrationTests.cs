using SmartSkills.Core.Registry;
using SmartSkills.Core.Registry.Matching;
using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class FileExistsMatchIntegrationTests
{
    private readonly SkillMatcher _matcher = new();

    [Fact]
    public void Match_WithSlnFile_MatchesNugetManager()
    {
        var packages = Array.Empty<ResolvedPackage>();
        var entries = new[]
        {
            new RegistryEntry
            {
                MatchStrategy = "file-exists",
                MatchCriteria = ["*.sln", "*.slnx", "global.json", "Directory.Build.props", "Directory.Packages.props"],
                SkillPath = "skills/nuget-manager",
                Language = "dotnet"
            }
        };
        var rootFiles = new[] { "MyApp.sln", "README.md" };

        var result = _matcher.Match(packages, entries, rootFiles);

        Assert.Single(result);
        Assert.Equal("skills/nuget-manager", result[0].RegistryEntry.SkillPath);
    }

    [Fact]
    public void Match_WithSlnxFile_MatchesNugetManager()
    {
        var packages = Array.Empty<ResolvedPackage>();
        var entries = new[]
        {
            new RegistryEntry
            {
                MatchStrategy = "file-exists",
                MatchCriteria = ["*.sln", "*.slnx", "global.json"],
                SkillPath = "skills/nuget-manager",
                Language = "dotnet"
            }
        };
        var rootFiles = new[] { "SmartSkills.slnx" };

        var result = _matcher.Match(packages, entries, rootFiles);

        Assert.Single(result);
    }

    [Fact]
    public void Match_WithGlobalJson_MatchesNugetManager()
    {
        var packages = Array.Empty<ResolvedPackage>();
        var entries = new[]
        {
            new RegistryEntry
            {
                MatchStrategy = "file-exists",
                MatchCriteria = ["*.sln", "global.json"],
                SkillPath = "skills/nuget-manager",
                Language = "dotnet"
            }
        };
        var rootFiles = new[] { "global.json", "README.md" };

        var result = _matcher.Match(packages, entries, rootFiles);

        Assert.Single(result);
    }

    [Fact]
    public void Match_WithoutDotNetFiles_DoesNotMatch()
    {
        var packages = Array.Empty<ResolvedPackage>();
        var entries = new[]
        {
            new RegistryEntry
            {
                MatchStrategy = "file-exists",
                MatchCriteria = ["*.sln", "*.slnx", "global.json"],
                SkillPath = "skills/nuget-manager",
                Language = "dotnet"
            }
        };
        var rootFiles = new[] { "package.json", "README.md" };

        var result = _matcher.Match(packages, entries, rootFiles);

        Assert.Empty(result);
    }

    [Fact]
    public void Match_MixedStrategies_BothMatch()
    {
        var packages = new[] { new ResolvedPackage { Name = "Azure.Identity", Version = "1.0.0", IsTransitive = false, Ecosystem = "dotnet" } };
        var entries = new[]
        {
            new RegistryEntry
            {
                MatchStrategy = "package",
                MatchCriteria = ["Azure.Identity"],
                SkillPath = ".github/skills/azure-identity-dotnet",
                Language = "dotnet"
            },
            new RegistryEntry
            {
                MatchStrategy = "file-exists",
                MatchCriteria = ["*.sln"],
                SkillPath = "skills/nuget-manager",
                Language = "dotnet"
            }
        };
        var rootFiles = new[] { "MyApp.sln" };

        var result = _matcher.Match(packages, entries, rootFiles);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.RegistryEntry.SkillPath == ".github/skills/azure-identity-dotnet");
        Assert.Contains(result, r => r.RegistryEntry.SkillPath == "skills/nuget-manager");
    }

    [Fact]
    public void Match_PackageOnlyEntry_IgnoresRootFiles()
    {
        var packages = Array.Empty<ResolvedPackage>();
        var entries = new[]
        {
            new RegistryEntry
            {
                MatchStrategy = "package",
                MatchCriteria = ["SomePackage"],
                SkillPath = "skills/some-skill"
            }
        };
        var rootFiles = new[] { "SomePackage" };

        var result = _matcher.Match(packages, entries, rootFiles);

        Assert.Empty(result);
    }

    [Fact]
    public void Match_FromEmbeddedRegistry_NugetManagerMatchesSlnFile()
    {
        var entries = RegistryIndexParser.LoadEmbedded();
        var packages = Array.Empty<ResolvedPackage>();
        var rootFiles = new[] { "MyProject.sln" };

        var result = _matcher.Match(packages, entries, rootFiles);

        Assert.Contains(result, r => r.RegistryEntry.SkillPath == "skills/nuget-manager");
    }
}
