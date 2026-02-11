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

var dryRunOption = new Option<bool>("--dry-run")
{
    Description = "Preview actions without executing them",
    Recursive = true
};

var baseDirOption = new Option<string?>("--base-dir")
{
    Description = "Base directory where the .agents/skills directory will be created (defaults to current directory)"
};

var rootCommand = new RootCommand("SmartSkills - Intelligent skill installer for .NET projects")
{
    verboseOption,
    dryRunOption,
    baseDirOption
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

// install command
var installCommand = new Command("install", "Install skills based on detected packages")
{
    projectOption
};

rootCommand.Subcommands.Add(installCommand);

installCommand.SetAction(async (parseResult, cancellationToken) =>
{
    bool verbose = parseResult.GetValue(verboseOption);
    string? projectPath = parseResult.GetValue(projectOption);
    bool dryRun = parseResult.GetValue(dryRunOption);
    string? baseDir = parseResult.GetValue(baseDirOption);

    using var host = CreateHost(verbose, baseDir);
    var installer = host.Services.GetRequiredService<SmartSkills.Core.Installation.ISkillInstaller>();

    projectPath = ResolveProjectPath(projectPath);

    var result = await installer.InstallAsync(new SmartSkills.Core.Installation.InstallOptions
    {
        ProjectPath = projectPath,
        DryRun = dryRun
    }, cancellationToken);

    Console.WriteLine();
    if (result.Installed.Count > 0)
    {
        Console.WriteLine($"Installed ({result.Installed.Count}):");
        foreach (var s in result.Installed)
            Console.WriteLine($"  + {s.Name}");
    }
    if (result.Updated.Count > 0)
    {
        Console.WriteLine($"Updated ({result.Updated.Count}):");
        foreach (var s in result.Updated)
            Console.WriteLine($"  ~ {s.Name}");
    }
    if (result.SkippedUpToDate.Count > 0)
    {
        Console.WriteLine($"Up-to-date ({result.SkippedUpToDate.Count}):");
        foreach (var s in result.SkippedUpToDate)
            Console.WriteLine($"  = {s}");
    }
    if (result.Failed.Count > 0)
    {
        Console.WriteLine($"Failed ({result.Failed.Count}):");
        foreach (var f in result.Failed)
            Console.WriteLine($"  x {f.SkillPath}: {f.Reason}");
    }
});

// uninstall command
var skillNameArg = new Argument<string>("skill-name")
{
    Description = "Name of the skill to uninstall"
};
var uninstallCommand = new Command("uninstall", "Remove an installed skill")
{
    skillNameArg
};

rootCommand.Subcommands.Add(uninstallCommand);

uninstallCommand.SetAction(async (parseResult, cancellationToken) =>
{
    bool verbose = parseResult.GetValue(verboseOption);
    string skillName = parseResult.GetValue(skillNameArg)!;
    string? baseDir = parseResult.GetValue(baseDirOption);

    using var host = CreateHost(verbose, baseDir);
    var installer = host.Services.GetRequiredService<SmartSkills.Core.Installation.ISkillInstaller>();

    await installer.UninstallAsync(skillName, cancellationToken);
    Console.WriteLine($"Uninstalled skill: {skillName}");
});

scanCommand.SetAction(async (parseResult, cancellationToken) =>
{
    bool verbose = parseResult.GetValue(verboseOption);
    string? projectPath = parseResult.GetValue(projectOption);
    bool jsonOutput = parseResult.GetValue(jsonOption);
    string? baseDir = parseResult.GetValue(baseDirOption);

    using var host = CreateHost(verbose, baseDir);
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

// list command
var listCommand = new Command("list", "List installed skills")
{
    jsonOption
};

rootCommand.Subcommands.Add(listCommand);

listCommand.SetAction(async (parseResult, cancellationToken) =>
{
    bool verbose = parseResult.GetValue(verboseOption);
    bool jsonOutput = parseResult.GetValue(jsonOption);
    string? baseDir = parseResult.GetValue(baseDirOption);

    using var host = CreateHost(verbose, baseDir);
    var store = host.Services.GetRequiredService<SmartSkills.Core.Installation.ISkillStore>();

    var skills = await store.GetInstalledSkillsAsync(cancellationToken);

    if (jsonOutput)
    {
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(skills, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }
    else if (skills.Count == 0)
    {
        Console.WriteLine("No skills installed.");
    }
    else
    {
        Console.WriteLine($"{"Name",-30} {"Provider",-15} {"Installed",-25} {"SHA"}");
        Console.WriteLine(new string('-', 90));
        foreach (var s in skills)
        {
            Console.WriteLine($"{s.Name,-30} {s.SourceProviderType,-15} {s.InstalledAt:yyyy-MM-dd HH:mm,-25} {s.CommitSha[..8]}");
        }
    }
});

// status command
var statusCommand = new Command("status", "Show status of installed skills and available updates")
{
    projectOption,
    jsonOption
};

rootCommand.Subcommands.Add(statusCommand);

statusCommand.SetAction(async (parseResult, cancellationToken) =>
{
    bool verbose = parseResult.GetValue(verboseOption);
    bool jsonOutput = parseResult.GetValue(jsonOption);
    string? baseDir = parseResult.GetValue(baseDirOption);

    using var host = CreateHost(verbose, baseDir);
    var store = host.Services.GetRequiredService<SmartSkills.Core.Installation.ISkillStore>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    var skills = await store.GetInstalledSkillsAsync(cancellationToken);

    if (skills.Count == 0)
    {
        Console.WriteLine("No skills installed.");
        return;
    }

    Console.WriteLine($"{"Name",-30} {"Status",-15} {"SHA",-12} {"Installed"}");
    Console.WriteLine(new string('-', 80));

    foreach (var s in skills)
    {
        Console.WriteLine($"{s.Name,-30} {"Installed",-15} {s.CommitSha[..8],-12} {s.InstalledAt:yyyy-MM-dd HH:mm}");
    }
});

rootCommand.SetAction(parseResult =>
{
    Console.WriteLine("SmartSkills CLI - Use --help for usage information.");
});

return await rootCommand.Parse(args).InvokeAsync();

static IHost CreateHost(bool verbose, string? baseDir = null)
{
    var skillsDir = baseDir is not null
        ? Path.Combine(Path.GetFullPath(baseDir), ".agents", "skills")
        : null;

    return Host.CreateDefaultBuilder()
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
        })
        .ConfigureServices(services =>
        {
            services.AddSmartSkills(skillsDir);
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
