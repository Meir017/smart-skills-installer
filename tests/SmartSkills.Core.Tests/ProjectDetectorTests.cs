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
            Assert.Equal(Ecosystems.JavaScript, result[0].Ecosystem);
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
            Assert.Contains(result, r => r.Ecosystem == Ecosystems.JavaScript);
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
            Assert.EndsWith(".sln", dotnet.ProjectFilePath, StringComparison.Ordinal);
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

    [Fact]
    public void Detect_PythonPyprojectToml_ReturnsPython()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "pyproject.toml"), "[project]\nname = \"test\"");

            var result = _detector.Detect(dir);

            Assert.Single(result);
            Assert.Equal(Ecosystems.Python, result[0].Ecosystem);
            Assert.EndsWith("pyproject.toml", result[0].ProjectFilePath, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Detect_PythonRequirementsTxt_ReturnsPython()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "requirements.txt"), "requests==2.31.0");

            var result = _detector.Detect(dir);

            Assert.Single(result);
            Assert.Equal(Ecosystems.Python, result[0].Ecosystem);
            Assert.EndsWith("requirements.txt", result[0].ProjectFilePath, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Detect_PythonPrefersPyprojectOverSetupPy()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "pyproject.toml"), "[project]\nname = \"test\"");
            File.WriteAllText(Path.Combine(dir, "setup.py"), "from setuptools import setup");

            var result = _detector.Detect(dir);

            var python = result.Single(r => r.Ecosystem == Ecosystems.Python);
            Assert.EndsWith("pyproject.toml", python.ProjectFilePath, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Detect_PolyglotDirectory_ReturnsThree()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "MyApp.sln"), "");
            File.WriteAllText(Path.Combine(dir, "package.json"), "{}");
            File.WriteAllText(Path.Combine(dir, "pyproject.toml"), "[project]\nname = \"test\"");

            var result = _detector.Detect(dir);

            Assert.Equal(3, result.Count);
            Assert.Contains(result, r => r.Ecosystem == Ecosystems.Dotnet);
            Assert.Contains(result, r => r.Ecosystem == Ecosystems.JavaScript);
            Assert.Contains(result, r => r.Ecosystem == Ecosystems.Python);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Detect_JavaPomXml_ReturnsJava()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "pom.xml"), "<project />");

            var result = _detector.Detect(dir);

            Assert.Single(result);
            Assert.Equal(Ecosystems.Java, result[0].Ecosystem);
            Assert.EndsWith("pom.xml", result[0].ProjectFilePath, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Detect_JavaBuildGradle_ReturnsJava()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "build.gradle"), "plugins {}");

            var result = _detector.Detect(dir);

            Assert.Single(result);
            Assert.Equal(Ecosystems.Java, result[0].Ecosystem);
            Assert.EndsWith("build.gradle", result[0].ProjectFilePath, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Detect_JavaBuildGradleKts_ReturnsJava()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "build.gradle.kts"), "plugins {}");

            var result = _detector.Detect(dir);

            Assert.Single(result);
            Assert.Equal(Ecosystems.Java, result[0].Ecosystem);
            Assert.EndsWith("build.gradle.kts", result[0].ProjectFilePath, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Detect_JavaPrefersPomOverGradle()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "pom.xml"), "<project />");
            File.WriteAllText(Path.Combine(dir, "build.gradle"), "plugins {}");
            File.WriteAllText(Path.Combine(dir, "build.gradle.kts"), "plugins {}");

            var result = _detector.Detect(dir);

            var java = result.Single(r => r.Ecosystem == Ecosystems.Java);
            Assert.EndsWith("pom.xml", java.ProjectFilePath, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Detect_PolyglotFourWay_ReturnsFour()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "MyApp.sln"), "");
            File.WriteAllText(Path.Combine(dir, "package.json"), "{}");
            File.WriteAllText(Path.Combine(dir, "pyproject.toml"), "[project]\nname = \"test\"");
            File.WriteAllText(Path.Combine(dir, "pom.xml"), "<project />");

            var result = _detector.Detect(dir);

            Assert.Equal(4, result.Count);
            Assert.Contains(result, r => r.Ecosystem == Ecosystems.Dotnet);
            Assert.Contains(result, r => r.Ecosystem == Ecosystems.JavaScript);
            Assert.Contains(result, r => r.Ecosystem == Ecosystems.Python);
            Assert.Contains(result, r => r.Ecosystem == Ecosystems.Java);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"smartskills-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
