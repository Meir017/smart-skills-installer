using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Scans .NET projects and solutions for resolved packages.
/// </summary>
public sealed class LibraryScanner : ILibraryScanner
{
    private readonly IPackageResolver _packageResolver;
    private readonly ILogger<LibraryScanner> _logger;

    public LibraryScanner(IPackageResolver packageResolver, ILogger<LibraryScanner> logger)
    {
        _packageResolver = packageResolver;
        _logger = logger;
    }

    public Task<ProjectPackages> ScanProjectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(projectPath))
            throw new FileNotFoundException($"Project file not found: {projectPath}");

        _logger.LogInformation("Scanning project: {ProjectPath}", projectPath);
        return _packageResolver.ResolvePackagesAsync(projectPath, cancellationToken);
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
                var packages = await _packageResolver.ResolvePackagesAsync(projectPath, cancellationToken);
                results.Add(packages);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve packages for {ProjectPath}", projectPath);
            }
        }

        return results;
    }
}
