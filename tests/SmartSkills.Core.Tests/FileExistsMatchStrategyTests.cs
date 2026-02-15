using SmartSkills.Core.Registry.Matching;
using Xunit;

namespace SmartSkills.Core.Tests;

public class FileExistsMatchStrategyTests
{
    private readonly FileExistsMatchStrategy _strategy = new();

    [Fact]
    public void Name_ReturnsFileExists()
    {
        Assert.Equal("file-exists", _strategy.Name);
    }

    [Fact]
    public void Evaluate_GlobMatch_ReturnsMatch()
    {
        var context = new MatchContext
        {
            RootFileNames = ["MyApp.sln", "README.md", "global.json"],
            Language = "dotnet"
        };

        var result = _strategy.Evaluate(context, ["*.sln"]);

        Assert.True(result.IsMatch);
        Assert.Contains("*.sln", result.MatchedPatterns);
    }

    [Fact]
    public void Evaluate_ExactMatch_ReturnsMatch()
    {
        var context = new MatchContext
        {
            RootFileNames = ["global.json", "README.md"],
            Language = "dotnet"
        };

        var result = _strategy.Evaluate(context, ["global.json"]);

        Assert.True(result.IsMatch);
        Assert.Contains("global.json", result.MatchedPatterns);
    }

    [Fact]
    public void Evaluate_NoMatch_ReturnsNoMatch()
    {
        var context = new MatchContext
        {
            RootFileNames = ["package.json", "README.md"],
            Language = "dotnet"
        };

        var result = _strategy.Evaluate(context, ["*.sln"]);

        Assert.False(result.IsMatch);
        Assert.Empty(result.MatchedPatterns);
    }

    [Fact]
    public void Evaluate_EmptyRootFiles_ReturnsNoMatch()
    {
        var context = new MatchContext
        {
            RootFileNames = [],
            Language = "dotnet"
        };

        var result = _strategy.Evaluate(context, ["*.sln", "global.json"]);

        Assert.False(result.IsMatch);
    }

    [Fact]
    public void Evaluate_CaseInsensitive_ReturnsMatch()
    {
        var context = new MatchContext
        {
            RootFileNames = ["directory.build.props"],
            Language = "dotnet"
        };

        var result = _strategy.Evaluate(context, ["Directory.Build.props"]);

        Assert.True(result.IsMatch);
    }

    [Fact]
    public void Evaluate_MultipleCriteria_MatchesAny()
    {
        var context = new MatchContext
        {
            RootFileNames = ["Directory.Packages.props"],
            Language = "dotnet"
        };

        var result = _strategy.Evaluate(context, ["*.sln", "*.slnx", "global.json", "Directory.Build.props", "Directory.Packages.props"]);

        Assert.True(result.IsMatch);
        Assert.Contains("Directory.Packages.props", result.MatchedPatterns);
    }

    [Fact]
    public void Evaluate_SlnxExtension_MatchesGlob()
    {
        var context = new MatchContext
        {
            RootFileNames = ["SmartSkills.slnx"],
            Language = "dotnet"
        };

        var result = _strategy.Evaluate(context, ["*.slnx"]);

        Assert.True(result.IsMatch);
    }
}
