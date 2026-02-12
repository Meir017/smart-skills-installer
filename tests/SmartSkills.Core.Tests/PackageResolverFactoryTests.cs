using Microsoft.Extensions.Logging.Abstractions;
using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class PackageResolverFactoryTests
{
    private readonly PackageResolverFactory _factory;

    public PackageResolverFactoryTests()
    {
        _factory = new PackageResolverFactory(
            new DotnetCliPackageResolver(NullLogger<DotnetCliPackageResolver>.Instance),
            new NpmPackageResolver(NullLogger<NpmPackageResolver>.Instance),
            new YarnPackageResolver(NullLogger<YarnPackageResolver>.Instance),
            new PnpmPackageResolver(NullLogger<PnpmPackageResolver>.Instance),
            new BunPackageResolver(NullLogger<BunPackageResolver>.Instance),
            new UvLockPackageResolver(NullLogger<UvLockPackageResolver>.Instance),
            new PoetryLockPackageResolver(NullLogger<PoetryLockPackageResolver>.Instance),
            new PipfileLockPackageResolver(NullLogger<PipfileLockPackageResolver>.Instance),
            new RequirementsTxtPackageResolver(NullLogger<RequirementsTxtPackageResolver>.Instance),
            new MavenPomPackageResolver(NullLogger<MavenPomPackageResolver>.Instance),
            new GradlePackageResolver(NullLogger<GradlePackageResolver>.Instance),
            NullLogger<PackageResolverFactory>.Instance);
    }

    [Fact]
    public void GetResolver_Dotnet_ReturnsDotnetResolver()
    {
        var project = new DetectedProject(Ecosystems.Dotnet, "test.csproj");
        var resolver = _factory.GetResolver(project);
        Assert.IsType<DotnetCliPackageResolver>(resolver);
    }

    [Fact]
    public void GetResolver_Npm_ReturnsNpmResolver()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "package.json"), "{}");
            var project = new DetectedProject(Ecosystems.JavaScript, Path.Combine(dir, "package.json"));
            var resolver = _factory.GetResolver(project);
            Assert.IsType<NpmPackageResolver>(resolver);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void GetResolver_PythonWithUvLock_ReturnsUvLockResolver()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "pyproject.toml"), "[project]\nname = \"test\"");
            File.WriteAllText(Path.Combine(dir, "uv.lock"), "version = 1");
            var project = new DetectedProject(Ecosystems.Python, Path.Combine(dir, "pyproject.toml"));
            var resolver = _factory.GetResolver(project);
            Assert.IsType<UvLockPackageResolver>(resolver);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void GetResolver_PythonWithPoetryLock_ReturnsPoetryResolver()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "pyproject.toml"), "[project]\nname = \"test\"");
            File.WriteAllText(Path.Combine(dir, "poetry.lock"), "[[package]]");
            var project = new DetectedProject(Ecosystems.Python, Path.Combine(dir, "pyproject.toml"));
            var resolver = _factory.GetResolver(project);
            Assert.IsType<PoetryLockPackageResolver>(resolver);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void GetResolver_PythonWithPipfileLock_ReturnsPipfileResolver()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "pyproject.toml"), "[project]\nname = \"test\"");
            File.WriteAllText(Path.Combine(dir, "Pipfile.lock"), "{}");
            var project = new DetectedProject(Ecosystems.Python, Path.Combine(dir, "pyproject.toml"));
            var resolver = _factory.GetResolver(project);
            Assert.IsType<PipfileLockPackageResolver>(resolver);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void GetResolver_PythonWithOnlyRequirementsTxt_ReturnsFallback()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "requirements.txt"), "requests==2.31.0");
            var project = new DetectedProject(Ecosystems.Python, Path.Combine(dir, "requirements.txt"));
            var resolver = _factory.GetResolver(project);
            Assert.IsType<RequirementsTxtPackageResolver>(resolver);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void GetResolver_JavaPomXml_ReturnsMavenResolver()
    {
        var project = new DetectedProject(Ecosystems.Java, "pom.xml");
        var resolver = _factory.GetResolver(project);
        Assert.IsType<MavenPomPackageResolver>(resolver);
    }

    [Fact]
    public void GetResolver_JavaBuildGradle_ReturnsGradleResolver()
    {
        var project = new DetectedProject(Ecosystems.Java, "build.gradle");
        var resolver = _factory.GetResolver(project);
        Assert.IsType<GradlePackageResolver>(resolver);
    }

    [Fact]
    public void GetResolver_JavaBuildGradleKts_ReturnsGradleResolver()
    {
        var project = new DetectedProject(Ecosystems.Java, "build.gradle.kts");
        var resolver = _factory.GetResolver(project);
        Assert.IsType<GradlePackageResolver>(resolver);
    }

    [Fact]
    public void GetResolver_UnsupportedEcosystem_Throws()
    {
        var project = new DetectedProject("ruby", "Gemfile");
        Assert.Throws<NotSupportedException>(() => _factory.GetResolver(project));
    }

    [Fact]
    public void GetResolver_BunLock_ReturnsBunResolver()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "package.json"), "{}");
            File.WriteAllText(Path.Combine(dir, "bun.lock"), "{}");
            var project = new DetectedProject(Ecosystems.JavaScript, Path.Combine(dir, "package.json"));
            var resolver = _factory.GetResolver(project);
            Assert.IsType<BunPackageResolver>(resolver);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void GetResolver_BunLockb_ReturnsBunResolver()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "package.json"), "{}");
            File.WriteAllText(Path.Combine(dir, "bun.lockb"), "binary");
            var project = new DetectedProject(Ecosystems.JavaScript, Path.Combine(dir, "package.json"));
            var resolver = _factory.GetResolver(project);
            Assert.IsType<BunPackageResolver>(resolver);
        }
        finally { Directory.Delete(dir, true); }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"smartskills-factory-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
