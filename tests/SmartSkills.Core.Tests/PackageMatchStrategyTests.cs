using SmartSkills.Core.Registry.Matching;
using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class PackageMatchStrategyTests
{
    private readonly PackageMatchStrategy _strategy = new();

    [Fact]
    public void Name_ReturnsPackage()
    {
        Assert.Equal("package", _strategy.Name);
    }

    [Fact]
    public void Evaluate_ExactMatch_ReturnsMatch()
    {
        var context = new MatchContext
        {
            ResolvedPackages = [new ResolvedPackage { Name = "Newtonsoft.Json", Version = "13.0.3", IsTransitive = false }]
        };

        var result = _strategy.Evaluate(context, ["Newtonsoft.Json"]);

        Assert.True(result.IsMatch);
        Assert.Contains("Newtonsoft.Json", result.MatchedPatterns);
    }

    [Fact]
    public void Evaluate_GlobPattern_ReturnsMatch()
    {
        var context = new MatchContext
        {
            ResolvedPackages =
            [
                new ResolvedPackage { Name = "Microsoft.EntityFrameworkCore", Version = "8.0.0", IsTransitive = false },
                new ResolvedPackage { Name = "Microsoft.EntityFrameworkCore.SqlServer", Version = "8.0.0", IsTransitive = false }
            ]
        };

        var result = _strategy.Evaluate(context, ["Microsoft.EntityFrameworkCore*"]);

        Assert.True(result.IsMatch);
    }

    [Fact]
    public void Evaluate_CaseInsensitive_ReturnsMatch()
    {
        var context = new MatchContext
        {
            ResolvedPackages = [new ResolvedPackage { Name = "NEWTONSOFT.JSON", Version = "13.0.3", IsTransitive = false }]
        };

        var result = _strategy.Evaluate(context, ["newtonsoft.json"]);

        Assert.True(result.IsMatch);
    }

    [Fact]
    public void Evaluate_NoMatch_ReturnsNoMatch()
    {
        var context = new MatchContext
        {
            ResolvedPackages = [new ResolvedPackage { Name = "SomePackage", Version = "1.0.0", IsTransitive = false }]
        };

        var result = _strategy.Evaluate(context, ["OtherPackage"]);

        Assert.False(result.IsMatch);
        Assert.Empty(result.MatchedPatterns);
    }

    [Fact]
    public void Evaluate_LanguageFilter_OnlyMatchesCorrectEcosystem()
    {
        var context = new MatchContext
        {
            ResolvedPackages =
            [
                new ResolvedPackage { Name = "express", Version = "4.18.2", IsTransitive = false, Ecosystem = "javascript" },
                new ResolvedPackage { Name = "Azure.Identity", Version = "1.0.0", IsTransitive = false, Ecosystem = "dotnet" }
            ],
            Language = "javascript"
        };

        var result = _strategy.Evaluate(context, ["express"]);

        Assert.True(result.IsMatch);
    }

    [Fact]
    public void Evaluate_LanguageFilter_DoesNotMatchWrongEcosystem()
    {
        var context = new MatchContext
        {
            ResolvedPackages = [new ResolvedPackage { Name = "express", Version = "4.18.2", IsTransitive = false, Ecosystem = "javascript" }],
            Language = "dotnet"
        };

        var result = _strategy.Evaluate(context, ["express"]);

        Assert.False(result.IsMatch);
    }

    [Fact]
    public void Evaluate_NullLanguage_MatchesAllEcosystems()
    {
        var context = new MatchContext
        {
            ResolvedPackages = [new ResolvedPackage { Name = "redis", Version = "4.0.0", IsTransitive = false, Ecosystem = "javascript" }],
            Language = null
        };

        var result = _strategy.Evaluate(context, ["redis"]);

        Assert.True(result.IsMatch);
    }
}
