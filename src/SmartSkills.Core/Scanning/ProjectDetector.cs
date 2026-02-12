using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Detects project types in a directory by looking for known project markers.
/// </summary>
public sealed class ProjectDetector(ILogger<ProjectDetector> logger) : IProjectDetector
{
    private static readonly string[] DotnetSolutionExtensions = [".sln", ".slnx"];
    private static readonly string[] DotnetProjectExtensions = [".csproj", ".fsproj", ".vbproj"];

    public IReadOnlyList<DetectedProject> Detect(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return [];

        var results = new List<DetectedProject>();

        DetectDotnet(directoryPath, results);
        DetectNodeJs(directoryPath, results);
        DetectPython(directoryPath, results);

        logger.LogDebug("Detected {Count} project(s) in {Directory}", results.Count, directoryPath);
        return results;
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

        // Ensure this is not a package.json inside node_modules
        var dirName = Path.GetFileName(directoryPath);
        if (string.Equals(dirName, "node_modules", StringComparison.OrdinalIgnoreCase))
            return;

        results.Add(new DetectedProject(Ecosystems.Npm, packageJson));
        logger.LogDebug("Detected Node.js project: {Path}", packageJson);
    }

    private static readonly string[] PythonExcludedDirs = ["venv", ".venv", "__pycache__", ".tox"];

    private void DetectPython(string directoryPath, List<DetectedProject> results)
    {
        var dirName = Path.GetFileName(directoryPath);
        if (PythonExcludedDirs.Any(d => string.Equals(dirName, d, StringComparison.OrdinalIgnoreCase)))
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
}
