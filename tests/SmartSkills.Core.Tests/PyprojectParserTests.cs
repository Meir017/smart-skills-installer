using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class PyprojectParserTests
{
    private static string TestDataPath => Path.Combine(AppContext.BaseDirectory, "TestData", "Python");

    [Fact]
    public void ParseDirectDependencies_Pep621_ReturnsNormalized()
    {
        var toml = File.ReadAllText(Path.Combine(TestDataPath, "pyproject.toml"));
        var deps = PyprojectParser.ParseDirectDependencies(toml);

        Assert.Contains("azure-identity", deps);
        Assert.Contains("fastapi", deps);
        Assert.Contains("pydantic", deps);
        Assert.Contains("pytest", deps); // from optional-dependencies
        Assert.Equal(4, deps.Count);
    }

    [Fact]
    public void ParseDirectDependencies_Poetry_ReturnsNormalized()
    {
        var toml = File.ReadAllText(Path.Combine(TestDataPath, "pyproject-poetry.toml"));
        var deps = PyprojectParser.ParseDirectDependencies(toml);

        Assert.Contains("azure-identity", deps);
        Assert.Contains("fastapi", deps);
        Assert.Contains("pytest", deps); // from dev-dependencies
        Assert.DoesNotContain("python", deps); // python itself is excluded
    }

    [Fact]
    public void ParseDirectDependencies_StripsVersionSpecifiers()
    {
        var toml = """
            [project]
            dependencies = [
                "azure-identity>=1.16.0,<2.0",
                "requests[security]>=2.0",
                "simple",
            ]
            """;
        var deps = PyprojectParser.ParseDirectDependencies(toml);

        Assert.Contains("azure-identity", deps);
        Assert.Contains("requests", deps);
        Assert.Contains("simple", deps);
    }

    [Fact]
    public void ExtractPackageName_HandlesVariousFormats()
    {
        Assert.Equal("azure-identity", PyprojectParser.ExtractPackageName("azure-identity>=1.0"));
        Assert.Equal("requests", PyprojectParser.ExtractPackageName("requests[security]>=2.0"));
        Assert.Equal("simple", PyprojectParser.ExtractPackageName("simple"));
        Assert.Equal("pkg", PyprojectParser.ExtractPackageName("pkg==1.2.3"));
        Assert.Equal("pkg", PyprojectParser.ExtractPackageName("pkg!=1.0"));
        Assert.Equal("pkg", PyprojectParser.ExtractPackageName("pkg~=1.0"));
    }
}
