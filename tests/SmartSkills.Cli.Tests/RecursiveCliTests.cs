using Xunit;

namespace SmartSkills.Cli.Tests;

[Collection("CLI")]
public class RecursiveCliTests : IDisposable
{
    private readonly string _root;

    public RecursiveCliTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"smartskills-cli-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Scan_Recursive_ShowsNestedProjects()
    {
        var ct = TestContext.Current.CancellationToken;
        // Root has a csproj
        await File.WriteAllTextAsync(Path.Combine(_root, "Root.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>", ct);

        // Sub has a package.json
        var sub = Path.Combine(_root, "web");
        Directory.CreateDirectory(sub);
        await File.WriteAllTextAsync(Path.Combine(sub, "package.json"), "{\"name\":\"web\",\"version\":\"1.0.0\"}", ct);

        var (exitCode, output) = await CliFixture.RunCliAsync($"scan --recursive --project \"{_root}\"");

        // Should find at least one project without error
        Assert.Equal(0, exitCode);
        Assert.Contains("Project:", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Root.csproj", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("package.json", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Scan_WithoutRecursive_BehavesAsDefault()
    {
        var ct = TestContext.Current.CancellationToken;
        await File.WriteAllTextAsync(Path.Combine(_root, "Root.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>", ct);

        var sub = Path.Combine(_root, "child");
        Directory.CreateDirectory(sub);
        await File.WriteAllTextAsync(Path.Combine(sub, "package.json"), "{\"name\":\"child\",\"version\":\"1.0.0\"}", ct);

        var (exitCode, output) = await CliFixture.RunCliAsync($"scan --project \"{_root}\"");

        Assert.Equal(0, exitCode);
        Assert.Contains("Project:", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Root.csproj", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Scan_RecursiveDepth0_BehavesLikeNonRecursive()
    {
        await File.WriteAllTextAsync(Path.Combine(_root, "Root.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>", TestContext.Current.CancellationToken);

        var (exitCode, output) = await CliFixture.RunCliAsync($"scan --recursive --depth 0 --project \"{_root}\"");

        Assert.Equal(0, exitCode);
        Assert.Contains("Project:", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Root.csproj", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Scan_RecursiveJson_ReturnsValidJson()
    {
        await File.WriteAllTextAsync(Path.Combine(_root, "package.json"), "{\"name\":\"test\",\"version\":\"1.0.0\"}", TestContext.Current.CancellationToken);

        var (exitCode, stdout, _) = await CliFixture.RunCliWithStreamsAsync($"scan --recursive --json --project \"{_root}\"");

        Assert.Equal(0, exitCode);
        // stdout should contain a JSON object with Projects and MatchedSkills
        var trimmed = stdout.Trim();
        Assert.StartsWith("{", trimmed, StringComparison.Ordinal);
        Assert.Contains("\"Projects\"", trimmed, StringComparison.Ordinal);
        Assert.Contains("\"MatchedSkills\"", trimmed, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ScanHelp_ShowsRecursiveOption()
    {
        var (exitCode, output) = await CliFixture.RunCliAsync("scan --help");

        Assert.Equal(0, exitCode);
        Assert.Contains("--recursive", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--depth", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InstallHelp_ShowsRecursiveOption()
    {
        var (exitCode, output) = await CliFixture.RunCliAsync("install --help");

        Assert.Equal(0, exitCode);
        Assert.Contains("--recursive", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--depth", output, StringComparison.OrdinalIgnoreCase);
    }
}
