using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class YarnPackageResolverTests
{
    private static string TestDataPath => Path.Combine(AppContext.BaseDirectory, "TestData", "NodeJs");

    [Fact]
    public void ParseYarnClassic_ReturnsPackages()
    {
        var packageJson = File.ReadAllText(Path.Combine(TestDataPath, "package.json"));
        var lockText = File.ReadAllText(Path.Combine(TestDataPath, "yarn.lock"));
        var directDeps = NpmPackageResolver.ParseDirectDependencies(packageJson);

        var packages = YarnPackageResolver.ParseYarnLock(lockText, directDeps);

        Assert.NotEmpty(packages);
        Assert.All(packages, p => Assert.Equal(Ecosystems.JavaScript, p.Ecosystem));
    }

    [Fact]
    public void ParseYarnClassic_DirectVsTransitive()
    {
        var packageJson = File.ReadAllText(Path.Combine(TestDataPath, "package.json"));
        var lockText = File.ReadAllText(Path.Combine(TestDataPath, "yarn.lock"));
        var directDeps = NpmPackageResolver.ParseDirectDependencies(packageJson);

        var packages = YarnPackageResolver.ParseYarnLock(lockText, directDeps);

        var express = packages.FirstOrDefault(p => p.Name == "express");
        Assert.NotNull(express);
        Assert.False(express.IsTransitive);

        var bodyParser = packages.FirstOrDefault(p => p.Name == "body-parser");
        Assert.NotNull(bodyParser);
        Assert.True(bodyParser.IsTransitive);
    }

    [Fact]
    public void ParseYarnClassic_ScopedPackages()
    {
        var packageJson = File.ReadAllText(Path.Combine(TestDataPath, "package.json"));
        var lockText = File.ReadAllText(Path.Combine(TestDataPath, "yarn.lock"));
        var directDeps = NpmPackageResolver.ParseDirectDependencies(packageJson);

        var packages = YarnPackageResolver.ParseYarnLock(lockText, directDeps);

        var azureIdentity = packages.FirstOrDefault(p => p.Name == "@azure/identity");
        Assert.NotNull(azureIdentity);
        Assert.Equal("3.4.2", azureIdentity.Version);
    }

    [Fact]
    public void ExtractPackageNameFromClassicHeader_Simple()
    {
        Assert.Equal("express", YarnPackageResolver.ExtractPackageNameFromClassicHeader("express@^4.18.0:"));
    }

    [Fact]
    public void ExtractPackageNameFromClassicHeader_Scoped()
    {
        Assert.Equal("@azure/identity", YarnPackageResolver.ExtractPackageNameFromClassicHeader(@"""@azure/identity@^3.4.0"":"));
    }
}
