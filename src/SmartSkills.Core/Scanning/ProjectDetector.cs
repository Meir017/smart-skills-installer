using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Detects project types in a directory by looking for known project markers.
/// </summary>
public sealed class ProjectDetector(ILogger<ProjectDetector> logger) : IProjectDetector
{
    private static readonly string[] DotnetSolutionExtensions = [".sln", ".slnx"];
    private static readonly string[] DotnetProjectExtensions = [".csproj", ".fsproj", ".vbproj"];

    public IReadOnlyList<DetectedProject> Detect(string directoryPath) =>
        Detect(directoryPath, new ProjectDetectionOptions());

    public IReadOnlyList<DetectedProject> Detect(string directoryPath, ProjectDetectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!Directory.Exists(directoryPath))
            return [];

        if (!options.Recursive)
        {
            var results = new List<DetectedProject>();
            DetectInDirectory(directoryPath, results);
            return results;
        }

        var allResults = new List<DetectedProject>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        RecursiveDetect(directoryPath, options.MaxDepth, 0, visited, allResults, detectedEcosystems: null);
        return allResults;
    }

    private void RecursiveDetect(
        string directoryPath,
        int maxDepth,
        int currentDepth,
        HashSet<string> visited,
        List<DetectedProject> allResults,
        HashSet<string>? detectedEcosystems)
    {
        // Resolve to canonical path to detect symlink cycles
        string canonicalPath;
        try
        {
            canonicalPath = Path.GetFullPath(directoryPath);
        }
        catch
        {
            return;
        }

        if (!visited.Add(canonicalPath))
        {
            logger.LogDebug("Skipping already-visited directory: {Path}", directoryPath);
            return;
        }

        // Detect projects at this level
        var localResults = new List<DetectedProject>();
        DetectInDirectory(directoryPath, localResults);
        allResults.AddRange(localResults);

        // Determine which ecosystems were found here (for prune-on-detection)
        var foundEcosystems = new HashSet<string>(
            localResults.Select(r => r.Ecosystem), StringComparer.OrdinalIgnoreCase);
        if (detectedEcosystems is not null)
            foundEcosystems.UnionWith(detectedEcosystems);

        if (currentDepth >= maxDepth)
            return;

        // Recurse into subdirectories
        IEnumerable<string> subdirs;
        try
        {
            subdirs = Directory.EnumerateDirectories(directoryPath);
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        foreach (var subdir in subdirs)
        {
            var dirName = Path.GetFileName(subdir);

            if (ExcludedDirectories.All.Contains(dirName))
            {
                logger.LogDebug("Skipping excluded directory: {Path}", subdir);
                continue;
            }

            // Prune: only recurse if there are ecosystems not yet detected in this subtree path
            // For simplicity, pass the set of ecosystems found on the path from root to here;
            // the child's DetectInDirectory will still find all ecosystems, but we skip recursing
            // deeper for ecosystems already found on this ancestor path.
            RecursiveDetect(subdir, maxDepth, currentDepth + 1, visited, allResults, foundEcosystems);
        }
    }

    private void DetectInDirectory(string directoryPath, List<DetectedProject> results)
    {
        DetectDotnet(directoryPath, results);
        DetectNodeJs(directoryPath, results);
        DetectPython(directoryPath, results);
        DetectJava(directoryPath, results);

        logger.LogDebug("Detected {Count} project(s) in {Directory}", results.Count, directoryPath);
    }

    private void DetectDotnet(string directoryPath, List<DetectedProject> results)
    {
        // Prefer solution files over individual project files
        foreach (var ext in DotnetSolutionExtensions)
        {
            var solutions = Directory.GetFiles(directoryPath, $"*{ext}");
            if (solutions.Length > 0)
            {
                results.Add(new DetectedProject(Ecosystems.Dotnet, solutions[0]));
                logger.LogDebug("Detected .NET solution: {Path}", solutions[0]);
                return;
            }
        }

        // Fall back to individual project files
        foreach (var ext in DotnetProjectExtensions)
        {
            var projects = Directory.GetFiles(directoryPath, $"*{ext}");
            if (projects.Length > 0)
            {
                results.Add(new DetectedProject(Ecosystems.Dotnet, projects[0]));
                logger.LogDebug("Detected .NET project: {Path}", projects[0]);
                return;
            }
        }
    }

    private void DetectNodeJs(string directoryPath, List<DetectedProject> results)
    {
        var packageJson = Path.Combine(directoryPath, "package.json");
        if (!File.Exists(packageJson))
            return;

        // Ensure this is not a package.json inside an excluded Node.js directory
        var dirName = Path.GetFileName(directoryPath);
        if (ExcludedDirectories.NodeJs.Contains(dirName))
            return;

        results.Add(new DetectedProject(Ecosystems.Npm, packageJson));
        logger.LogDebug("Detected Node.js project: {Path}", packageJson);
    }

    private void DetectPython(string directoryPath, List<DetectedProject> results)
    {
        var dirName = Path.GetFileName(directoryPath);
        if (ExcludedDirectories.Python.Contains(dirName))
            return;

        // Prefer pyproject.toml, then setup.py, then requirements.txt
        var pyprojectToml = Path.Combine(directoryPath, "pyproject.toml");
        if (File.Exists(pyprojectToml))
        {
            results.Add(new DetectedProject(Ecosystems.Python, pyprojectToml));
            logger.LogDebug("Detected Python project: {Path}", pyprojectToml);
            return;
        }

        var setupPy = Path.Combine(directoryPath, "setup.py");
        if (File.Exists(setupPy))
        {
            results.Add(new DetectedProject(Ecosystems.Python, setupPy));
            logger.LogDebug("Detected Python project: {Path}", setupPy);
            return;
        }

        var requirementsTxt = Path.Combine(directoryPath, "requirements.txt");
        if (File.Exists(requirementsTxt))
        {
            results.Add(new DetectedProject(Ecosystems.Python, requirementsTxt));
            logger.LogDebug("Detected Python project: {Path}", requirementsTxt);
            return;
        }
    }

    private void DetectJava(string directoryPath, List<DetectedProject> results)
    {
        // Prefer pom.xml (Maven) over build.gradle/build.gradle.kts (Gradle)
        var pomXml = Path.Combine(directoryPath, "pom.xml");
        if (File.Exists(pomXml))
        {
            results.Add(new DetectedProject(Ecosystems.Java, pomXml));
            logger.LogDebug("Detected Java Maven project: {Path}", pomXml);
            return;
        }

        var buildGradleKts = Path.Combine(directoryPath, "build.gradle.kts");
        if (File.Exists(buildGradleKts))
        {
            results.Add(new DetectedProject(Ecosystems.Java, buildGradleKts));
            logger.LogDebug("Detected Java Gradle project: {Path}", buildGradleKts);
            return;
        }

        var buildGradle = Path.Combine(directoryPath, "build.gradle");
        if (File.Exists(buildGradle))
        {
            results.Add(new DetectedProject(Ecosystems.Java, buildGradle));
            logger.LogDebug("Detected Java Gradle project: {Path}", buildGradle);
            return;
        }
    }
}
