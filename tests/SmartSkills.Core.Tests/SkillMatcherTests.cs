using SmartSkills.Core.Registry;
using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class SkillMatcherTests
{
    private readonly SkillMatcher _matcher = new();

    [Fact]
    public void Match_ExactName_ReturnsMatch()
    {
        var packages = new[] { new ResolvedPackage { Name = "Newtonsoft.Json", Version = "13.0.3", IsTransitive = false } };
        var entries = new[] { new RegistryEntry { Type = "package", MatchCriteria = ["Newtonsoft.Json"], SkillPath = "skills/json" } };

        var result = _matcher.Match(packages, entries);

        Assert.Single(result);
        Assert.Equal("skills/json", result[0].RegistryEntry.SkillPath);
    }

    [Fact]
    public void Match_GlobPattern_ReturnsMatch()
    {
        var packages = new[]
        {
            new ResolvedPackage { Name = "Microsoft.EntityFrameworkCore", Version = "8.0.0", IsTransitive = false },
            new ResolvedPackage { Name = "Microsoft.EntityFrameworkCore.SqlServer", Version = "8.0.0", IsTransitive = false }
        };
        var entries = new[] { new RegistryEntry { Type = "package", MatchCriteria = ["Microsoft.EntityFrameworkCore*"], SkillPath = "skills/ef" } };

        var result = _matcher.Match(packages, entries);

        Assert.Single(result);
    }

    [Fact]
    public void Match_CaseInsensitive_ReturnsMatch()
    {
        var packages = new[] { new ResolvedPackage { Name = "NEWTONSOFT.JSON", Version = "13.0.3", IsTransitive = false } };
        var entries = new[] { new RegistryEntry { Type = "package", MatchCriteria = ["newtonsoft.json"], SkillPath = "skills/json" } };

        var result = _matcher.Match(packages, entries);

        Assert.Single(result);
    }

    [Fact]
    public void Match_NoMatch_ReturnsEmpty()
    {
        var packages = new[] { new ResolvedPackage { Name = "SomePackage", Version = "1.0.0", IsTransitive = false } };
        var entries = new[] { new RegistryEntry { Type = "package", MatchCriteria = ["OtherPackage"], SkillPath = "skills/other" } };

        var result = _matcher.Match(packages, entries);

        Assert.Empty(result);
    }

    [Fact]
    public void Match_MultiplePatterns_MatchesAny()
    {
        var packages = new[] { new ResolvedPackage { Name = "PackageB", Version = "1.0.0", IsTransitive = false } };
        var entries = new[] { new RegistryEntry { Type = "package", MatchCriteria = ["PackageA", "PackageB"], SkillPath = "skills/multi" } };

        var result = _matcher.Match(packages, entries);

        Assert.Single(result);
    }
}
