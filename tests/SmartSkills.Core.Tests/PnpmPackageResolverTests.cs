using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class PnpmPackageResolverTests
{
    private static string TestDataPath => Path.Combine(AppContext.BaseDirectory, "TestData", "NodeJs");

    [Fact]
    public void ParsePnpmLock_ReturnsPackages()
    {
        var packageJson = File.ReadAllText(Path.Combine(TestDataPath, "package.json"));
        var lockText = File.ReadAllText(Path.Combine(TestDataPath, "pnpm-lock.yaml"));
        var directDeps = NpmPackageResolver.ParseDirectDependencies(packageJson);

        var packages = PnpmPackageResolver.ParsePnpmLock(lockText, directDeps);

        Assert.NotEmpty(packages);
        Assert.All(packages, p => Assert.Equal(Ecosystems.JavaScript, p.Ecosystem));
    }

    [Fact]
    public void ParsePnpmLock_DirectVsTransitive()
    {
        var packageJson = File.ReadAllText(Path.Combine(TestDataPath, "package.json"));
        var lockText = File.ReadAllText(Path.Combine(TestDataPath, "pnpm-lock.yaml"));
        var directDeps = NpmPackageResolver.ParseDirectDependencies(packageJson);

        var packages = PnpmPackageResolver.ParsePnpmLock(lockText, directDeps);

        var express = packages.FirstOrDefault(p => p.Name == "express");
        Assert.NotNull(express);
        Assert.False(express.IsTransitive);

        var bodyParser = packages.FirstOrDefault(p => p.Name == "body-parser");
        Assert.NotNull(bodyParser);
        Assert.True(bodyParser.IsTransitive);
    }

    [Fact]
    public void ParsePnpmPackageKey_SimplePackage()
    {
        var (name, version) = PnpmPackageResolver.ParsePnpmPackageKey("/express@4.18.2");
        Assert.Equal("express", name);
        Assert.Equal("4.18.2", version);
    }

    [Fact]
    public void ParsePnpmPackageKey_ScopedPackage()
    {
        var (name, version) = PnpmPackageResolver.ParsePnpmPackageKey("/@azure/identity@3.4.2");
        Assert.Equal("@azure/identity", name);
        Assert.Equal("3.4.2", version);
    }

    [Fact]
    public void ParsePnpmPackageKey_NoLeadingSlash()
    {
        var (name, version) = PnpmPackageResolver.ParsePnpmPackageKey("@azure/identity@3.4.2");
        Assert.Equal("@azure/identity", name);
        Assert.Equal("3.4.2", version);
    }

    [Fact]
    public void ParsePnpmPackageKey_WithPeerInfo()
    {
        var (name, version) = PnpmPackageResolver.ParsePnpmPackageKey("express@4.18.2(supports-color@5.5.0)");
        Assert.Equal("express", name);
        Assert.Equal("4.18.2", version);
    }
}
