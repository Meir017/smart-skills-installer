using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartSkills.Core;
using SmartSkills.Core.Scanning;

var verboseOption = new Option<bool>("--verbose", "-v")
{
    Description = "Enable detailed logging output",
    Recursive = true
};

var configOption = new Option<string?>("--config", "-c")
{
    Description = "Path to a custom configuration file",
    Recursive = true
};

var dryRunOption = new Option<bool>("--dry-run")
{
    Description = "Preview actions without executing them",
    Recursive = true
};

var rootCommand = new RootCommand("SmartSkills - Intelligent skill installer for .NET projects")
{
    verboseOption,
    configOption,
    dryRunOption
};

// scan command
var projectOption = new Option<string?>("--project", "-p")
{
    Description = "Path to a project or solution file (defaults to current directory)"
};

var jsonOption = new Option<bool>("--json")
{
    Description = "Output results in JSON format"
};

var scanCommand = new Command("scan", "Scan a project or solution for installed packages")
{
    projectOption,
    jsonOption
};

rootCommand.Subcommands.Add(scanCommand);

scanCommand.SetAction(async (parseResult, cancellationToken) =>
{
    bool verbose = parseResult.GetValue(verboseOption);
    string? projectPath = parseResult.GetValue(projectOption);
    bool jsonOutput = parseResult.GetValue(jsonOption);

    using var host = CreateHost(verbose);
    var scanner = host.Services.GetRequiredService<ILibraryScanner>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    projectPath = ResolveProjectPath(projectPath);
    logger.LogInformation("Scanning: {Path}", projectPath);

    IReadOnlyList<ProjectPackages> results;

    if (projectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
        projectPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
    {
        results = await scanner.ScanSolutionAsync(projectPath, cancellationToken);
    }
    else
    {
        var single = await scanner.ScanProjectAsync(projectPath, cancellationToken);
        results = [single];
    }

    if (jsonOutput)
    {
        var output = results.Select(r => new
        {
            r.ProjectPath,
            Packages = r.Packages.Select(p => new
            {
                p.Name,
                p.Version,
                p.IsTransitive,
                p.TargetFramework,
                p.RequestedVersion
            })
        });
        Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
    }
    else
    {
        foreach (var project in results)
        {
            Console.WriteLine();
            Console.WriteLine($"Project: {project.ProjectPath}");
            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"{"Package",-45} {"Version",-15} {"Type",-12} {"Framework"}");
            Console.WriteLine(new string('-', 80));

            foreach (var pkg in project.Packages)
            {
                var type = pkg.IsTransitive ? "Transitive" : "Direct";
                Console.WriteLine($"{pkg.Name,-45} {pkg.Version,-15} {type,-12} {pkg.TargetFramework}");
            }
        }
    }
});

rootCommand.SetAction(parseResult =>
{
    Console.WriteLine("SmartSkills CLI - Use --help for usage information.");
});

return await rootCommand.Parse(args).InvokeAsync();

static IHost CreateHost(bool verbose)
{
    return Host.CreateDefaultBuilder()
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
        })
        .ConfigureServices(services =>
        {
            services.AddSmartSkills();
        })
        .Build();
}

static string ResolveProjectPath(string? path)
{
    if (!string.IsNullOrEmpty(path))
        return Path.GetFullPath(path);

    var dir = Directory.GetCurrentDirectory();

    var slnFiles = Directory.GetFiles(dir, "*.sln");
    if (slnFiles.Length == 1) return slnFiles[0];

    var slnxFiles = Directory.GetFiles(dir, "*.slnx");
    if (slnxFiles.Length == 1) return slnxFiles[0];

    var csprojFiles = Directory.GetFiles(dir, "*.csproj");
    if (csprojFiles.Length == 1) return csprojFiles[0];

    throw new FileNotFoundException("No project or solution file found in the current directory. Use --project to specify one.");
}
