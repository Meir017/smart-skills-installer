using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Scans projects, solutions, and directories for resolved packages across ecosystems.
/// </summary>
public sealed class LibraryScanner : ILibraryScanner
{
    private readonly IPackageResolverFactory _resolverFactory;
    private readonly IPackageResolver _defaultResolver;
    private readonly IProjectDetector _projectDetector;
    private readonly ILogger<LibraryScanner> _logger;

    public LibraryScanner(
        IPackageResolverFactory resolverFactory,
        IPackageResolver defaultResolver,
        IProjectDetector projectDetector,
        ILogger<LibraryScanner> logger)
    {
        _resolverFactory = resolverFactory;
        _defaultResolver = defaultResolver;
        _projectDetector = projectDetector;
        _logger = logger;
    }

    public Task<ProjectPackages> ScanProjectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectPath);

        if (!File.Exists(projectPath))
            throw new FileNotFoundException($"Project file not found: {projectPath}");

        _logger.LogInformation("Scanning project: {ProjectPath}", projectPath);

        var project = InferDetectedProject(projectPath);
        var resolver = project is not null
            ? _resolverFactory.GetResolver(project)
            : _defaultResolver;

        return resolver.ResolvePackagesAsync(projectPath, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectPackages>> ScanSolutionAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(solutionPath))
            throw new FileNotFoundException($"Solution file not found: {solutionPath}");

        _logger.LogInformation("Scanning solution: {SolutionPath}", solutionPath);

        var solution = await SolutionSerializers.GetSerializerByMoniker(solutionPath)!
            .OpenAsync(solutionPath, cancellationToken);

        var results = new List<ProjectPackages>();

        foreach (var project in solution.SolutionProjects)
        {
            var projectPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(solutionPath)!, project.FilePath));

            if (!projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) &&
                !projectPath.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase) &&
                !projectPath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Skipping non-.NET project: {ProjectPath}", projectPath);
                continue;
            }

            try
            {
                var packages = await _defaultResolver.ResolvePackagesAsync(projectPath, cancellationToken);
                results.Add(packages);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve packages for {ProjectPath}", projectPath);
            }
        }

        return results;
    }

    public Task<IReadOnlyList<ProjectPackages>> ScanDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default) =>
        ScanDirectoryAsync(directoryPath, new ProjectDetectionOptions(), cancellationToken);

    public async Task<IReadOnlyList<ProjectPackages>> ScanDirectoryAsync(string directoryPath, ProjectDetectionOptions options, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        _logger.LogInformation("Scanning directory: {DirectoryPath}", directoryPath);

        var detected = _projectDetector.Detect(directoryPath, options);
        var results = new List<ProjectPackages>();
        var seenProjectPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in detected)
        {
            // Deduplicate by project file path
            if (!seenProjectPaths.Add(Path.GetFullPath(project.ProjectFilePath)))
                continue;

            try
            {
                // .NET solutions need the special ScanSolutionAsync path
                if (string.Equals(project.Ecosystem, Ecosystems.Dotnet, StringComparison.OrdinalIgnoreCase)
                    && (project.ProjectFilePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
                        || project.ProjectFilePath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)))
                {
                    var solutionResults = await ScanSolutionAsync(project.ProjectFilePath, cancellationToken);
                    foreach (var sr in solutionResults)
                    {
                        if (seenProjectPaths.Add(Path.GetFullPath(sr.ProjectPath)))
                            results.Add(sr);
                    }
                }
                else
                {
                    var resolver = _resolverFactory.GetResolver(project);
                    var packages = await resolver.ResolvePackagesAsync(project.ProjectFilePath, cancellationToken);
                    results.Add(packages);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan {Ecosystem} project: {Path}", project.Ecosystem, project.ProjectFilePath);
            }
        }

        return results;
    }

    private static DetectedProject? InferDetectedProject(string filePath)
    {
        if (filePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
            filePath.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase) ||
            filePath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase))
        {
            return new DetectedProject(Ecosystems.Dotnet, filePath);
        }

        if (filePath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase))
        {
            return new DetectedProject(Ecosystems.JavaScript, filePath);
        }

        return null;
    }
}
