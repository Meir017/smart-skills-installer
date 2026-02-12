using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class NpmPackageResolverTests
{
    private static string TestDataPath => Path.Combine(AppContext.BaseDirectory, "TestData", "NodeJs");

    [Fact]
    public void ParseDirectDependencies_ReturnsAllDeps()
    {
        var json = File.ReadAllText(Path.Combine(TestDataPath, "package.json"));
        var deps = NpmPackageResolver.ParseDirectDependencies(json);

        Assert.Equal(3, deps.Count);
        Assert.True(deps.ContainsKey("@azure/identity"));
        Assert.True(deps.ContainsKey("express"));
        Assert.True(deps.ContainsKey("typescript"));
    }

    [Fact]
    public void ParseLockFile_V3_ReturnsAllPackages()
    {
        var packageJson = File.ReadAllText(Path.Combine(TestDataPath, "package.json"));
        var lockFile = File.ReadAllText(Path.Combine(TestDataPath, "package-lock.json"));
        var directDeps = NpmPackageResolver.ParseDirectDependencies(packageJson);

        var packages = NpmPackageResolver.ParseLockFile(lockFile, directDeps);

        Assert.Equal(5, packages.Count);
        Assert.All(packages, p => Assert.Equal(Ecosystems.JavaScript, p.Ecosystem));
    }

    [Fact]
    public void ParseLockFile_DirectVsTransitive_ClassifiedCorrectly()
    {
        var packageJson = File.ReadAllText(Path.Combine(TestDataPath, "package.json"));
        var lockFile = File.ReadAllText(Path.Combine(TestDataPath, "package-lock.json"));
        var directDeps = NpmPackageResolver.ParseDirectDependencies(packageJson);

        var packages = NpmPackageResolver.ParseLockFile(lockFile, directDeps);

        var express = packages.First(p => p.Name == "express");
        Assert.False(express.IsTransitive);

        var bodyParser = packages.First(p => p.Name == "body-parser");
        Assert.True(bodyParser.IsTransitive);
    }

    [Fact]
    public void ParseLockFile_ScopedPackage_HandledCorrectly()
    {
        var packageJson = File.ReadAllText(Path.Combine(TestDataPath, "package.json"));
        var lockFile = File.ReadAllText(Path.Combine(TestDataPath, "package-lock.json"));
        var directDeps = NpmPackageResolver.ParseDirectDependencies(packageJson);

        var packages = NpmPackageResolver.ParseLockFile(lockFile, directDeps);

        var azureIdentity = packages.First(p => p.Name == "@azure/identity");
        Assert.Equal("3.4.2", azureIdentity.Version);
        Assert.False(azureIdentity.IsTransitive);
    }

    [Fact]
    public void ExtractPackageName_SimplePackage()
    {
        Assert.Equal("express", NpmPackageResolver.ExtractPackageName("node_modules/express"));
    }

    [Fact]
    public void ExtractPackageName_ScopedPackage()
    {
        Assert.Equal("@azure/identity", NpmPackageResolver.ExtractPackageName("node_modules/@azure/identity"));
    }

    [Fact]
    public void ExtractPackageName_NestedNodeModules()
    {
        Assert.Equal("bar", NpmPackageResolver.ExtractPackageName("node_modules/foo/node_modules/bar"));
    }
}
