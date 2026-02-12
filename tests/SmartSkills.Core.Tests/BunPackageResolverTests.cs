using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class BunPackageResolverTests
{
    private static string TestDataPath => Path.Combine(AppContext.BaseDirectory, "TestData", "NodeJs");

    [Fact]
    public void ParseBunLock_ReturnsAllPackages()
    {
        var packageJson = File.ReadAllText(Path.Combine(TestDataPath, "package.json"));
        var lockFile = File.ReadAllText(Path.Combine(TestDataPath, "bun.lock"));
        var directDeps = NpmPackageResolver.ParseDirectDependencies(packageJson);

        var packages = BunPackageResolver.ParseBunLock(lockFile, directDeps);

        Assert.Equal(5, packages.Count);
        Assert.All(packages, p => Assert.Equal(Ecosystems.JavaScript, p.Ecosystem));
    }

    [Fact]
    public void ParseBunLock_DirectVsTransitive_ClassifiedCorrectly()
    {
        var packageJson = File.ReadAllText(Path.Combine(TestDataPath, "package.json"));
        var lockFile = File.ReadAllText(Path.Combine(TestDataPath, "bun.lock"));
        var directDeps = NpmPackageResolver.ParseDirectDependencies(packageJson);

        var packages = BunPackageResolver.ParseBunLock(lockFile, directDeps);

        var express = packages.First(p => p.Name == "express");
        Assert.False(express.IsTransitive);

        var bodyParser = packages.First(p => p.Name == "body-parser");
        Assert.True(bodyParser.IsTransitive);
    }

    [Fact]
    public void ParseBunLock_ScopedPackage_HandledCorrectly()
    {
        var packageJson = File.ReadAllText(Path.Combine(TestDataPath, "package.json"));
        var lockFile = File.ReadAllText(Path.Combine(TestDataPath, "bun.lock"));
        var directDeps = NpmPackageResolver.ParseDirectDependencies(packageJson);

        var packages = BunPackageResolver.ParseBunLock(lockFile, directDeps);

        var azureIdentity = packages.First(p => p.Name == "@azure/identity");
        Assert.Equal("3.4.2", azureIdentity.Version);
        Assert.False(azureIdentity.IsTransitive);
    }

    [Fact]
    public void ExtractVersionFromResolved_SimplePackage()
    {
        Assert.Equal("4.18.2", BunPackageResolver.ExtractVersionFromResolved("express@4.18.2"));
    }

    [Fact]
    public void ExtractVersionFromResolved_ScopedPackage()
    {
        Assert.Equal("3.4.2", BunPackageResolver.ExtractVersionFromResolved("@azure/identity@3.4.2"));
    }

    [Fact]
    public void ExtractVersionFromResolved_NullInput()
    {
        Assert.Null(BunPackageResolver.ExtractVersionFromResolved(""));
    }

    [Fact]
    public void ParseBunLock_WithJsoncComments_ParsesCorrectly()
    {
        var lockText = """
            {
              // This is a comment
              "lockfileVersion": 0,
              "workspaces": {
                "": {
                  "dependencies": {
                    "express": "^4.18.0"
                  }
                }
              },
              "packages": {
                "express": ["express@4.18.2", {}],
              }
            }
            """;
        var directDeps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["express"] = "^4.18.0" };

        var packages = BunPackageResolver.ParseBunLock(lockText, directDeps);

        Assert.Single(packages);
        Assert.Equal("express", packages[0].Name);
        Assert.Equal("4.18.2", packages[0].Version);
    }

    [Fact]
    public void ParseBunLock_EmptyPackages_ReturnsEmpty()
    {
        var lockText = """
            {
              "lockfileVersion": 0,
              "packages": {}
            }
            """;
        var directDeps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var packages = BunPackageResolver.ParseBunLock(lockText, directDeps);

        Assert.Empty(packages);
    }
}
