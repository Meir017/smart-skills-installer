using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class PipfileLockPackageResolverTests
{
    private static string TestDataPath => Path.Combine(AppContext.BaseDirectory, "TestData", "Python");

    [Fact]
    public void ParsePipfileLock_ReturnsAllPackages()
    {
        var json = File.ReadAllText(Path.Combine(TestDataPath, "Pipfile.lock"));
        var packages = PipfileLockPackageResolver.ParsePipfileLock(json);

        Assert.Equal(4, packages.Count); // 3 default + 1 develop
        Assert.All(packages, p => Assert.Equal(Ecosystems.Python, p.Ecosystem));
        Assert.All(packages, p => Assert.False(p.IsTransitive)); // All direct in Pipfile.lock
    }

    [Fact]
    public void ParsePipfileLock_DefaultAndDevelopSections()
    {
        var json = File.ReadAllText(Path.Combine(TestDataPath, "Pipfile.lock"));
        var packages = PipfileLockPackageResolver.ParsePipfileLock(json);

        Assert.Contains(packages, p => p.Name == "azure-identity" && p.Version == "1.16.0");
        Assert.Contains(packages, p => p.Name == "fastapi" && p.Version == "0.115.0");
        Assert.Contains(packages, p => p.Name == "pytest" && p.Version == "8.0.0");
    }

    [Fact]
    public void ParsePipfileLock_VersionPrefixStripped()
    {
        var json = """
            {
              "default": {
                "requests": { "version": "==2.31.0" }
              },
              "develop": {}
            }
            """;
        var packages = PipfileLockPackageResolver.ParsePipfileLock(json);

        Assert.Single(packages);
        Assert.Equal("2.31.0", packages[0].Version);
    }

    [Fact]
    public void ParsePipfileLock_NamesAreNormalized()
    {
        var json = """
            {
              "default": {
                "Azure_Identity": { "version": "==1.0.0" }
              },
              "develop": {}
            }
            """;
        var packages = PipfileLockPackageResolver.ParsePipfileLock(json);

        Assert.Single(packages);
        Assert.Equal("azure-identity", packages[0].Name);
    }
}
