using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class UvLockPackageResolverTests
{
    private static string TestDataPath => Path.Combine(AppContext.BaseDirectory, "TestData", "Python");

    [Fact]
    public void ParseUvLock_ReturnsAllPackages()
    {
        var toml = File.ReadAllText(Path.Combine(TestDataPath, "uv.lock"));
        var directDeps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "azure-identity", "fastapi", "pydantic"
        };

        var packages = UvLockPackageResolver.ParseUvLock(toml, directDeps);

        Assert.Equal(5, packages.Count);
        Assert.All(packages, p => Assert.Equal(Ecosystems.Python, p.Ecosystem));
    }

    [Fact]
    public void ParseUvLock_DirectVsTransitive()
    {
        var toml = File.ReadAllText(Path.Combine(TestDataPath, "uv.lock"));
        var directDeps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "azure-identity", "fastapi", "pydantic"
        };

        var packages = UvLockPackageResolver.ParseUvLock(toml, directDeps);

        var azureIdentity = packages.First(p => p.Name == "azure-identity");
        Assert.False(azureIdentity.IsTransitive);
        Assert.Equal("1.16.0", azureIdentity.Version);

        var starlette = packages.First(p => p.Name == "starlette");
        Assert.True(starlette.IsTransitive);

        var msal = packages.First(p => p.Name == "msal");
        Assert.True(msal.IsTransitive);
    }

    [Fact]
    public void ParseUvLock_NamesAreNormalized()
    {
        var toml = """
            version = 1

            [[package]]
            name = "Azure_Identity"
            version = "1.0.0"
            """;
        var packages = UvLockPackageResolver.ParseUvLock(toml, []);

        Assert.Single(packages);
        Assert.Equal("azure-identity", packages[0].Name);
    }
}
