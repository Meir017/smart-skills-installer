using System.Diagnostics;
using Xunit;

namespace SmartSkills.Cli.Tests;

public class CliEndToEndTests
{
    private static readonly string CliProjectPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "SmartSkills.Cli", "SmartSkills.Cli.csproj"));

    [Fact]
    public async Task Help_DisplaysUsageInfo()
    {
        var (exitCode, output) = await RunCliAsync("--help");

        Assert.Equal(0, exitCode);
        Assert.Contains("SmartSkills", output);
        Assert.Contains("scan", output);
        Assert.Contains("install", output);
    }

    [Fact]
    public async Task Version_DisplaysVersion()
    {
        var (exitCode, output) = await RunCliAsync("--version");

        Assert.Equal(0, exitCode);
        Assert.NotEmpty(output.Trim());
    }

    [Fact]
    public async Task List_WhenNoSkillsInstalled_ShowsMessage()
    {
        var (exitCode, output) = await RunCliAsync("list");

        Assert.Equal(0, exitCode);
        Assert.Contains("No skills installed", output);
    }

    [Fact]
    public async Task UnknownCommand_ReturnsError()
    {
        var (exitCode, _) = await RunCliAsync("nonexistent");

        Assert.NotEqual(0, exitCode);
    }

    private static async Task<(int ExitCode, string Output)> RunCliAsync(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{CliProjectPath}\" -- {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        return (process.ExitCode, stdout + stderr);
    }
}
