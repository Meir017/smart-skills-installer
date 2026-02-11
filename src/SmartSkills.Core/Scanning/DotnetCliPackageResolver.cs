using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Resolves packages by shelling out to 'dotnet list package --include-transitive --format json'.
/// </summary>
public sealed class DotnetCliPackageResolver : IPackageResolver
{
    private readonly ILogger<DotnetCliPackageResolver> _logger;

    public DotnetCliPackageResolver(ILogger<DotnetCliPackageResolver> logger)
    {
        _logger = logger;
    }

    public async Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Resolving packages for {ProjectPath}", projectPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"list \"{projectPath}\" package --include-transitive --format json",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to start 'dotnet' CLI. Ensure the .NET SDK is installed and on PATH.", ex);
        }

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            _logger.LogError("dotnet list package failed: {StdErr}", stderr);
            throw new InvalidOperationException($"'dotnet list package' exited with code {process.ExitCode}: {stderr}");
        }

        return ParseJsonOutput(projectPath, stdout);
    }

    public static ProjectPackages ParseJsonOutput(string projectPath, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var packages = new List<ResolvedPackage>();

        if (!root.TryGetProperty("projects", out var projects))
            return new ProjectPackages(projectPath, packages);

        foreach (var project in projects.EnumerateArray())
        {
            if (!project.TryGetProperty("frameworks", out var frameworks))
                continue;

            foreach (var framework in frameworks.EnumerateArray())
            {
                var tfm = framework.TryGetProperty("framework", out var fwProp) ? fwProp.GetString() : null;

                if (framework.TryGetProperty("topLevelPackages", out var topLevel))
                {
                    foreach (var pkg in topLevel.EnumerateArray())
                    {
                        packages.Add(new ResolvedPackage
                        {
                            Name = pkg.GetProperty("id").GetString()!,
                            Version = pkg.GetProperty("resolvedVersion").GetString()!,
                            IsTransitive = false,
                            TargetFramework = tfm,
                            RequestedVersion = pkg.TryGetProperty("requestedVersion", out var rv) ? rv.GetString() : null
                        });
                    }
                }

                if (framework.TryGetProperty("transitivePackages", out var transitive))
                {
                    foreach (var pkg in transitive.EnumerateArray())
                    {
                        packages.Add(new ResolvedPackage
                        {
                            Name = pkg.GetProperty("id").GetString()!,
                            Version = pkg.GetProperty("resolvedVersion").GetString()!,
                            IsTransitive = true,
                            TargetFramework = tfm
                        });
                    }
                }
            }
        }

        return new ProjectPackages(projectPath, packages);
    }
}
