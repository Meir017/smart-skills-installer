using Microsoft.Extensions.Logging.Abstractions;
using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class ProjectDetectorTests
{
    private readonly ProjectDetector _detector = new(NullLogger<ProjectDetector>.Instance);

    [Fact]
    public void Detect_EmptyDirectory_ReturnsEmpty()
    {
        var dir = CreateTempDir();
        try
        {
            var result = _detector.Detect(dir);
            Assert.Empty(result);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Detect_DotnetOnly_ReturnsDotnet()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "MyApp.csproj"), "<Project />");

            var result = _detector.Detect(dir);

            Assert.Single(result);
            Assert.Equal(Ecosystems.Dotnet, result[0].Ecosystem);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Detect_NodeJsOnly_ReturnsNpm()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "package.json"), "{}");

            var result = _detector.Detect(dir);

            Assert.Single(result);
            Assert.Equal(Ecosystems.Npm, result[0].Ecosystem);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Detect_MixedDirectory_ReturnsBoth()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "MyApp.sln"), "");
            File.WriteAllText(Path.Combine(dir, "package.json"), "{}");

            var result = _detector.Detect(dir);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Ecosystem == Ecosystems.Dotnet);
            Assert.Contains(result, r => r.Ecosystem == Ecosystems.Npm);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Detect_PrefersSolutionOverProject()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "MyApp.sln"), "");
            File.WriteAllText(Path.Combine(dir, "MyApp.csproj"), "<Project />");

            var result = _detector.Detect(dir);

            var dotnet = result.Single(r => r.Ecosystem == Ecosystems.Dotnet);
            Assert.EndsWith(".sln", dotnet.ProjectFilePath);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Detect_NonExistentDirectory_ReturnsEmpty()
    {
        var result = _detector.Detect(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        Assert.Empty(result);
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"smartskills-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
