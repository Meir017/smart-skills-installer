using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class PoetryLockPackageResolverTests
{
    private static string TestDataPath => Path.Combine(AppContext.BaseDirectory, "TestData", "Python");

    [Fact]
    public void ParsePoetryLock_ReturnsAllPackages()
    {
        var toml = File.ReadAllText(Path.Combine(TestDataPath, "poetry.lock"));
        var directDeps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "azure-identity", "fastapi"
        };

        var packages = PoetryLockPackageResolver.ParsePoetryLock(toml, directDeps);

        Assert.Equal(4, packages.Count);
        Assert.All(packages, p => Assert.Equal(Ecosystems.Python, p.Ecosystem));
    }

    [Fact]
    public void ParsePoetryLock_DirectVsTransitive()
    {
        var toml = File.ReadAllText(Path.Combine(TestDataPath, "poetry.lock"));
        var directDeps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "azure-identity", "fastapi"
        };

        var packages = PoetryLockPackageResolver.ParsePoetryLock(toml, directDeps);

        var azureIdentity = packages.First(p => p.Name == "azure-identity");
        Assert.False(azureIdentity.IsTransitive);
        Assert.Equal("1.16.0", azureIdentity.Version);

        var starlette = packages.First(p => p.Name == "starlette");
        Assert.True(starlette.IsTransitive);
    }

    [Fact]
    public void ParsePoetryLock_NamesAreNormalized()
    {
        var toml = """
            [[package]]
            name = "Azure_Identity"
            version = "1.0.0"
            """;
        var packages = PoetryLockPackageResolver.ParsePoetryLock(toml, []);

        Assert.Single(packages);
        Assert.Equal("azure-identity", packages[0].Name);
    }
}
