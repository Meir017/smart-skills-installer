using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Resolves Java packages from Gradle build files (build.gradle and build.gradle.kts).
/// </summary>
public sealed partial class GradlePackageResolver(ILogger<GradlePackageResolver> logger) : IPackageResolver
{
    private static readonly string[] GradleFileNames = ["build.gradle.kts", "build.gradle"];

    /// <summary>
    /// Known Gradle dependency configurations.
    /// </summary>
    private static readonly HashSet<string> KnownConfigurations = new(StringComparer.OrdinalIgnoreCase)
    {
        "implementation", "api", "compileOnly", "runtimeOnly",
        "testImplementation", "testCompileOnly", "testRuntimeOnly",
        "annotationProcessor", "testAnnotationProcessor",
        "compileOnlyApi", "kapt"
    };

    public async Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectPath);

        var buildFilePath = FindBuildFile(projectPath);
        if (buildFilePath is null)
            throw new FileNotFoundException($"No Gradle build file found at: {projectPath}");

        var buildFileText = await File.ReadAllTextAsync(buildFilePath, cancellationToken).ConfigureAwait(false);
        var packages = ParseBuildFile(buildFileText);

        logger.LogInformation("Resolved {Count} Gradle packages from {Path}", packages.Count, buildFilePath);
        return new ProjectPackages(buildFilePath, packages);
    }

    private static string? FindBuildFile(string projectPath)
    {
        // If path points directly to a build file
        foreach (var name in GradleFileNames)
        {
            if (projectPath.EndsWith(name, StringComparison.OrdinalIgnoreCase) && File.Exists(projectPath))
                return projectPath;
        }

        // Search directory for build files
        foreach (var name in GradleFileNames)
        {
            var path = Path.Combine(projectPath, name);
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    /// <summary>
    /// Parse a Gradle build file and extract dependency declarations.
    /// Supports both Groovy DSL (build.gradle) and Kotlin DSL (build.gradle.kts).
    /// </summary>
    internal static List<ResolvedPackage> ParseBuildFile(string buildFileText)
    {
        var packages = new List<ResolvedPackage>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Match patterns like:
        //   implementation 'group:artifact:version'
        //   implementation "group:artifact:version"
        //   implementation("group:artifact:version")
        //   implementation("group:artifact") // no version (BOM-managed)
        //   testImplementation 'group:artifact:version'
        foreach (var match in DependencyPattern().Matches(buildFileText).Cast<Match>())
        {
            var group = match.Groups["group"].Value;
            var artifact = match.Groups["artifact"].Value;
            var version = match.Groups["version"].Success ? match.Groups["version"].Value : "";

            if (string.IsNullOrEmpty(group) || string.IsNullOrEmpty(artifact))
                continue;

            var config = match.Groups["config"].Value;
            if (!KnownConfigurations.Contains(config))
                continue;

            var name = $"{group}:{artifact}";
            if (!seen.Add(name))
                continue;

            packages.Add(new ResolvedPackage
            {
                Name = name,
                Version = version,
                IsTransitive = false,
                Ecosystem = Ecosystems.Java
            });
        }

        return packages;
    }

    /// <summary>
    /// Regex matching Gradle dependency declarations in both Groovy and Kotlin DSL.
    /// Matches: configuration 'group:artifact:version', configuration("group:artifact:version"),
    ///          configuration 'group:artifact', configuration("group:artifact")
    /// </summary>
    [GeneratedRegex(
        @"(?<config>\w+)\s*[\(]?\s*['""](?<group>[a-zA-Z0-9._-]+):(?<artifact>[a-zA-Z0-9._-]+)(?::(?<version>[^'""]+))?['""]",
        RegexOptions.Multiline)]
    private static partial Regex DependencyPattern();
}
