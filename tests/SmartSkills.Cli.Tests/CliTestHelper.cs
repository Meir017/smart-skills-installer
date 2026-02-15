using System.Diagnostics;
using Xunit;

namespace SmartSkills.Cli.Tests;

/// <summary>
/// Shared helper for CLI end-to-end tests. Builds the CLI project once,
/// then all tests invoke the compiled DLL directly via <c>dotnet exec</c>
/// to avoid per-test SDK overhead.
/// </summary>
public sealed class CliFixture : IAsyncLifetime
{
    internal static readonly string CliProjectPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "SmartSkills.Cli", "SmartSkills.Cli.csproj"));

    /// <summary>Path to the compiled CLI DLL, resolved after build.</summary>
    internal static string CliDllPath { get; private set; } = "";

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

        // Resolve the compiled DLL path from the CLI project's output directory
        var cliDir = Path.GetDirectoryName(CliProjectPath)!;
        CliDllPath = Path.Combine(cliDir, "bin", "Debug", "net10.0", "SmartSkills.Cli.dll");
        if (!File.Exists(CliDllPath))
            throw new FileNotFoundException($"CLI DLL not found after build: {CliDllPath}");
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
            Arguments = $"exec \"{CliDllPath}\" {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        return (process.ExitCode, stdout, stderr);
    }
}

[CollectionDefinition("CLI")]
public class CliTestsDefinition : ICollectionFixture<CliFixture>;
