using SmartSkills.Core.Registry;
using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class LanguageAwareMatcherTests
{
    private readonly SkillMatcher _matcher = new();

    [Fact]
    public void Match_NpmEntry_DoesNotMatchDotnetPackage()
    {
        var packages = new[] { new ResolvedPackage { Name = "express", Version = "4.18.0", IsTransitive = false, Ecosystem = Ecosystems.Dotnet } };
        var entries = new[] { new RegistryEntry { MatchCriteria = ["express"], SkillPath = "skills/express", Language = "javascript" } };

        var result = _matcher.Match(packages, entries);

        Assert.Empty(result);
    }

    [Fact]
    public void Match_DotnetEntry_DoesNotMatchNpmPackage()
    {
        var packages = new[] { new ResolvedPackage { Name = "Azure.Identity", Version = "1.0.0", IsTransitive = false, Ecosystem = Ecosystems.JavaScript } };
        var entries = new[] { new RegistryEntry { MatchCriteria = ["Azure.Identity"], SkillPath = "skills/azure-id", Language = "dotnet" } };

        var result = _matcher.Match(packages, entries);

        Assert.Empty(result);
    }

    [Fact]
    public void Match_NpmEntry_MatchesNpmPackage()
    {
        var packages = new[] { new ResolvedPackage { Name = "@azure/identity", Version = "3.4.0", IsTransitive = false, Ecosystem = Ecosystems.JavaScript } };
        var entries = new[] { new RegistryEntry { MatchCriteria = ["@azure/identity"], SkillPath = "skills/azure-identity-ts", Language = "javascript" } };

        var result = _matcher.Match(packages, entries);

        Assert.Single(result);
    }

    [Fact]
    public void Match_NullLanguage_MatchesBothEcosystems()
    {
        var packages = new[]
        {
            new ResolvedPackage { Name = "redis", Version = "4.0.0", IsTransitive = false, Ecosystem = Ecosystems.JavaScript },
            new ResolvedPackage { Name = "redis", Version = "2.0.0", IsTransitive = false, Ecosystem = Ecosystems.Dotnet }
        };
        var entries = new[] { new RegistryEntry { MatchCriteria = ["redis"], SkillPath = "skills/redis", Language = null } };

        var result = _matcher.Match(packages, entries);

        Assert.Single(result);
    }

    [Fact]
    public void Match_MixedPackagesAndEntries_CorrectCrossProduct()
    {
        var packages = new[]
        {
            new ResolvedPackage { Name = "Azure.Identity", Version = "1.0.0", IsTransitive = false, Ecosystem = Ecosystems.Dotnet },
            new ResolvedPackage { Name = "@azure/identity", Version = "3.0.0", IsTransitive = false, Ecosystem = Ecosystems.JavaScript }
        };
        var entries = new[]
        {
            new RegistryEntry { MatchCriteria = ["Azure.Identity"], SkillPath = "skills/azure-id-dotnet", Language = "dotnet" },
            new RegistryEntry { MatchCriteria = ["@azure/identity"], SkillPath = "skills/azure-id-ts", Language = "javascript" }
        };

        var result = _matcher.Match(packages, entries);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.RegistryEntry.SkillPath == "skills/azure-id-dotnet");
        Assert.Contains(result, r => r.RegistryEntry.SkillPath == "skills/azure-id-ts");
    }

    [Fact]
    public void Match_LanguageCaseInsensitive()
    {
        var packages = new[] { new ResolvedPackage { Name = "express", Version = "4.0.0", IsTransitive = false, Ecosystem = "javascript" } };
        var entries = new[] { new RegistryEntry { MatchCriteria = ["express"], SkillPath = "skills/express", Language = "javascript" } };

        var result = _matcher.Match(packages, entries);

        Assert.Single(result);
    }
}
