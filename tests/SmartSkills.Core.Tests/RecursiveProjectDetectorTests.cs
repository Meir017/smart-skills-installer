using Microsoft.Extensions.Logging.Abstractions;
using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class RecursiveProjectDetectorTests : IDisposable
{
    private readonly ProjectDetector _detector = new(NullLogger<ProjectDetector>.Instance);
    private readonly string _root;

    public RecursiveProjectDetectorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"smartskills-recursive-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Recursive_FlatLayout_FindsSingleProject()
    {
        File.WriteAllText(Path.Combine(_root, "MyApp.csproj"), "<Project />");

        var result = _detector.Detect(_root, new ProjectDetectionOptions { Recursive = true });

        Assert.Single(result);
        Assert.Equal(Ecosystems.Dotnet, result[0].Ecosystem);
    }

    [Fact]
    public void Recursive_NestedMonorepo_FindsAllProjects()
    {
        // Root has a solution
        File.WriteAllText(Path.Combine(_root, "Monorepo.sln"), "");

        // Depth 1: frontend
        var frontend = Path.Combine(_root, "frontend");
        Directory.CreateDirectory(frontend);
        File.WriteAllText(Path.Combine(frontend, "package.json"), "{}");

        // Depth 2: backend/api
        var backendApi = Path.Combine(_root, "backend", "api");
        Directory.CreateDirectory(backendApi);
        File.WriteAllText(Path.Combine(backendApi, "Api.csproj"), "<Project />");

        // Depth 2: services/python-svc
        var pythonSvc = Path.Combine(_root, "services", "python-svc");
        Directory.CreateDirectory(pythonSvc);
        File.WriteAllText(Path.Combine(pythonSvc, "pyproject.toml"), "[project]\nname = \"svc\"");

        var result = _detector.Detect(_root, new ProjectDetectionOptions { Recursive = true });

        Assert.Contains(result, r => r.Ecosystem == Ecosystems.Dotnet && r.ProjectFilePath.EndsWith(".sln", StringComparison.Ordinal));
        Assert.Contains(result, r => r.Ecosystem == Ecosystems.JavaScript);
        Assert.Contains(result, r => r.Ecosystem == Ecosystems.Dotnet && r.ProjectFilePath.EndsWith(".csproj", StringComparison.Ordinal));
        Assert.Contains(result, r => r.Ecosystem == Ecosystems.Python);
    }

    [Fact]
    public void Recursive_SkipsNodeModules()
    {
        File.WriteAllText(Path.Combine(_root, "package.json"), "{}");

        var nodeModules = Path.Combine(_root, "node_modules", "some-package");
        Directory.CreateDirectory(nodeModules);
        File.WriteAllText(Path.Combine(nodeModules, "package.json"), "{}");

        var result = _detector.Detect(_root, new ProjectDetectionOptions { Recursive = true });

        // Only the root package.json should be found
        var npmResults = result.Where(r => r.Ecosystem == Ecosystems.JavaScript).ToList();
        Assert.Single(npmResults);
        Assert.DoesNotContain(npmResults, r => r.ProjectFilePath.Contains("node_modules", StringComparison.Ordinal));
    }

    [Fact]
    public void Recursive_SkipsGitDirectory()
    {
        File.WriteAllText(Path.Combine(_root, "MyApp.csproj"), "<Project />");

        var gitDir = Path.Combine(_root, ".git", "hooks");
        Directory.CreateDirectory(gitDir);
        File.WriteAllText(Path.Combine(_root, ".git", "package.json"), "{}");

        var result = _detector.Detect(_root, new ProjectDetectionOptions { Recursive = true });

        Assert.DoesNotContain(result, r => r.ProjectFilePath.Contains(".git", StringComparison.Ordinal));
        Assert.Single(result); // only the .csproj
    }

    [Fact]
    public void Recursive_RespectsMaxDepth()
    {
        // Project at depth 3 (root -> a -> b -> c)
        var deep = Path.Combine(_root, "a", "b", "c");
        Directory.CreateDirectory(deep);
        File.WriteAllText(Path.Combine(deep, "package.json"), "{}");

        // MaxDepth = 2 should NOT find it (depth 0=root, 1=a, 2=b, needs 3 for c)
        var result = _detector.Detect(_root, new ProjectDetectionOptions { Recursive = true, MaxDepth = 2 });
        Assert.Empty(result);

        // MaxDepth = 3 should find it
        var result2 = _detector.Detect(_root, new ProjectDetectionOptions { Recursive = true, MaxDepth = 3 });
        Assert.Single(result2);
    }

    [Fact]
    public void Recursive_DepthBoundary_ProjectAtDepth6WithMaxDepth5_NotFound()
    {
        var deep = Path.Combine(_root, "a", "b", "c", "d", "e", "f");
        Directory.CreateDirectory(deep);
        File.WriteAllText(Path.Combine(deep, "package.json"), "{}");

        var result = _detector.Detect(_root, new ProjectDetectionOptions { Recursive = true, MaxDepth = 5 });
        Assert.Empty(result);
    }

    [Fact]
    public void Recursive_MixedEcosystems_BothFound()
    {
        // .csproj at root
        File.WriteAllText(Path.Combine(_root, "MyApp.csproj"), "<Project />");

        // package.json in subdirectory
        var subdir = Path.Combine(_root, "web");
        Directory.CreateDirectory(subdir);
        File.WriteAllText(Path.Combine(subdir, "package.json"), "{}");

        var result = _detector.Detect(_root, new ProjectDetectionOptions { Recursive = true });

        Assert.Contains(result, r => r.Ecosystem == Ecosystems.Dotnet);
        Assert.Contains(result, r => r.Ecosystem == Ecosystems.JavaScript);
    }

    [Fact]
    public void NonRecursive_DefaultBehavior_OnlyScansCurrentDirectory()
    {
        File.WriteAllText(Path.Combine(_root, "MyApp.csproj"), "<Project />");

        var subdir = Path.Combine(_root, "sub");
        Directory.CreateDirectory(subdir);
        File.WriteAllText(Path.Combine(subdir, "package.json"), "{}");

        var result = _detector.Detect(_root, new ProjectDetectionOptions { Recursive = false });

        Assert.Single(result);
        Assert.Equal(Ecosystems.Dotnet, result[0].Ecosystem);
    }

    [Fact]
    public void Recursive_SkipsMultipleExcludedDirectories()
    {
        // Create various excluded directories with projects inside
        var excluded = new[] { "bin", "obj", ".venv", "__pycache__", "target", "dist" };
        foreach (var dir in excluded)
        {
            var path = Path.Combine(_root, dir);
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "package.json"), "{}");
        }

        // Only a real project at root
        File.WriteAllText(Path.Combine(_root, "MyApp.csproj"), "<Project />");

        var result = _detector.Detect(_root, new ProjectDetectionOptions { Recursive = true });

        // Should only find the root .csproj, none from excluded dirs
        Assert.Single(result);
        Assert.Equal(Ecosystems.Dotnet, result[0].Ecosystem);
    }
}
