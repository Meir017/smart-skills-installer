using System.Diagnostics;
using Xunit;

namespace SmartSkills.Cli.Tests;

/// <summary>
/// Shared helper for CLI end-to-end tests. Builds the CLI project once,
/// then all tests use <c>dotnet run --no-build</c> to avoid MSBuild
/// output polluting stdout.
/// </summary>
public sealed class CliFixture : IAsyncLifetime
{
    internal static readonly string CliProjectPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "SmartSkills.Cli", "SmartSkills.Cli.csproj"));

    public async ValueTask InitializeAsync()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{CliProjectPath}\" --no-restore",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        if (process.ExitCode != 0)
        {
            var stderr = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
            throw new InvalidOperationException($"dotnet build failed ({process.ExitCode}): {stderr}");
        }
    }

    public ValueTask DisposeAsync() => default;

    internal static async Task<(int ExitCode, string Output)> RunCliAsync(string arguments)
    {
        var (exitCode, stdout, stderr) = await RunCliWithStreamsAsync(arguments);
        return (exitCode, stdout + stderr);
    }

    internal static async Task<(int ExitCode, string Stdout, string Stderr)> RunCliWithStreamsAsync(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-build --project \"{CliProjectPath}\" -- {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.Environment["MSBUILDTERMINALLOGGER"] = "off";
        psi.Environment["DOTNET_NOLOGO"] = "1";

        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        return (process.ExitCode, stdout, stderr);
    }
}

[CollectionDefinition("CLI")]
public class CliTestsDefinition : ICollectionFixture<CliFixture>;
