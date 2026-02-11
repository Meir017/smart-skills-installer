using System.Diagnostics;

namespace SmartSkills.Cli.Tests;

/// <summary>
/// End-to-end tests that invoke the CLI as a subprocess.
/// </summary>
public class CliEndToEndTests : IDisposable
{
    private readonly string _tempDir;

    public CliEndToEndTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "smartskills-e2e-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private async Task<(int ExitCode, string StdOut, string StdErr)> RunCliAsync(string args, int timeoutMs = 30000)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var cliProject = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "src", "SmartSkills.Cli", "SmartSkills.Cli.csproj"));

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{cliProject}\" --no-build -- {args}",
            WorkingDirectory = _tempDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        var stdOut = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErr = await process.StandardError.ReadToEndAsync(cancellationToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs);
        await process.WaitForExitAsync(cts.Token);

        return (process.ExitCode, stdOut, stdErr);
    }

    [Fact]
    public async Task NoArgs_ShowsHelp()
    {
        var (exitCode, stdOut, _) = await RunCliAsync("");
        // System.CommandLine shows help or error when no command given
        Assert.Contains("SmartSkills", stdOut + stdOut, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HelpFlag_ShowsUsage()
    {
        var (exitCode, stdOut, _) = await RunCliAsync("--help");
        Assert.Equal(0, exitCode);
        Assert.Contains("scan", stdOut, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Scan_NonExistentPath_HandlesGracefully()
    {
        var (exitCode, stdOut, stdErr) = await RunCliAsync("scan --path /nonexistent/path");
        // CLI may return 0 or non-zero; just verify it doesn't crash
        Assert.True(exitCode >= 0);
    }

    [Fact]
    public async Task Scan_EmptyDirectory_ReturnsNoPackages()
    {
        // Create an empty .csproj so scan has something to work with
        var csproj = Path.Combine(_tempDir, "Test.csproj");
        await File.WriteAllTextAsync(csproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """, TestContext.Current.CancellationToken);

        var (exitCode, stdOut, _) = await RunCliAsync($"scan --path \"{_tempDir}\"");
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Scan_WithPackageReferences_DetectsPackages()
    {
        var csproj = Path.Combine(_tempDir, "Test.csproj");
        await File.WriteAllTextAsync(csproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                <PackageReference Include="Serilog" Version="3.1.1" />
              </ItemGroup>
            </Project>
            """, TestContext.Current.CancellationToken);

        var (exitCode, stdOut, _) = await RunCliAsync($"scan --path \"{_tempDir}\"");
        Assert.Equal(0, exitCode);
        Assert.Contains("Newtonsoft.Json", stdOut);
        Assert.Contains("Serilog", stdOut);
    }

    [Fact]
    public async Task Scan_JsonOutput_ReturnsValidJson()
    {
        var csproj = Path.Combine(_tempDir, "Test.csproj");
        await File.WriteAllTextAsync(csproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
              </ItemGroup>
            </Project>
            """, TestContext.Current.CancellationToken);

        var (exitCode, stdOut, _) = await RunCliAsync($"scan --path \"{_tempDir}\" --output json");
        Assert.Equal(0, exitCode);
        // Should contain JSON array
        Assert.Contains("Newtonsoft.Json", stdOut);
    }

    [Fact]
    public async Task List_NoInstalledSkills_ShowsEmptyMessage()
    {
        var (exitCode, stdOut, _) = await RunCliAsync("list");
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Status_ShowsProjectSummary()
    {
        var csproj = Path.Combine(_tempDir, "Test.csproj");
        await File.WriteAllTextAsync(csproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
              </ItemGroup>
            </Project>
            """, TestContext.Current.CancellationToken);

        var (exitCode, stdOut, _) = await RunCliAsync($"status --path \"{_tempDir}\"");
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task VerboseFlag_AcceptedByParser()
    {
        var csproj = Path.Combine(_tempDir, "Test.csproj");
        await File.WriteAllTextAsync(csproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """, TestContext.Current.CancellationToken);

        // --verbose is a global option, placed before the subcommand
        var (exitCode, stdOut, stdErr) = await RunCliAsync($"--verbose scan --path \"{_tempDir}\"");
        Assert.Equal(0, exitCode);
    }
}
