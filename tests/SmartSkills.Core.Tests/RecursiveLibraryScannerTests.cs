using Microsoft.Extensions.Logging.Abstractions;
using SmartSkills.Core.Scanning;
using SmartSkills.Core.Tests.Fakes;
using Xunit;

namespace SmartSkills.Core.Tests;

public class RecursiveLibraryScannerTests : IDisposable
{
    private readonly string _root;

    public RecursiveLibraryScannerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"smartskills-scanner-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ScanDirectoryAsync_WithDefaultOptions_WorksAsExistingBehavior()
    {
        var detected = new List<DetectedProject>
        {
            new(Ecosystems.JavaScript, Path.Combine(_root, "package.json"))
        };

        var packages = new ProjectPackages(
            Path.Combine(_root, "package.json"),
            [new ResolvedPackage { Name = "express", Version = "4.18.0", IsTransitive = false, Ecosystem = Ecosystems.JavaScript }]);

        var detector = new FakeProjectDetector(detected);
        var resolver = new FakePackageResolver(packages);
        var resolverFactory = new FakePackageResolverFactory(resolver);
        var scanner = new LibraryScanner(resolverFactory, resolver, detector, NullLogger<LibraryScanner>.Instance);

        var result = await scanner.ScanDirectoryAsync(_root, TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal("express", result[0].Packages[0].Name);
    }

    [Fact]
    public async Task ScanDirectoryAsync_WithRecursiveOptions_PassesOptionsToDetector()
    {
        var project1 = new DetectedProject(Ecosystems.JavaScript, Path.Combine(_root, "package.json"));
        var project2 = new DetectedProject(Ecosystems.JavaScript, Path.Combine(_root, "sub", "package.json"));

        var detector = new FakeProjectDetector([project1, project2]);
        var resolver = new FakePackageResolver(path => new ProjectPackages(
            path,
            [new ResolvedPackage { Name = $"pkg-{Path.GetDirectoryName(path)!.Split(Path.DirectorySeparatorChar)[^1]}", Version = "1.0.0", IsTransitive = false, Ecosystem = Ecosystems.JavaScript }]));
        var resolverFactory = new FakePackageResolverFactory(resolver);
        var scanner = new LibraryScanner(resolverFactory, resolver, detector, NullLogger<LibraryScanner>.Instance);

        var options = new ProjectDetectionOptions { Recursive = true, MaxDepth = 3 };
        var result = await scanner.ScanDirectoryAsync(_root, options, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.True(detector.LastOptionsUsed?.Recursive);
        Assert.Equal(3, detector.LastOptionsUsed?.MaxDepth);
    }

    [Fact]
    public async Task ScanDirectoryAsync_DeduplicatesSameProjectPath()
    {
        var projectPath = Path.Combine(_root, "MyApp.csproj");

        // Detector returns the same project twice (simulating it being found via multiple paths)
        var detector = new FakeProjectDetector([
            new(Ecosystems.Dotnet, projectPath),
            new(Ecosystems.Dotnet, projectPath)
        ]);

        var resolver = new FakePackageResolver(new ProjectPackages(
            projectPath,
            [new ResolvedPackage { Name = "Newtonsoft.Json", Version = "13.0.0", IsTransitive = false }]));
        var resolverFactory = new FakePackageResolverFactory(resolver);
        var scanner = new LibraryScanner(resolverFactory, resolver, detector, NullLogger<LibraryScanner>.Instance);

        var result = await scanner.ScanDirectoryAsync(_root, new ProjectDetectionOptions { Recursive = true }, TestContext.Current.CancellationToken);

        // Should only have one result despite two detections
        Assert.Single(result);
    }
}
