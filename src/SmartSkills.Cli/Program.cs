using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartSkills.Core;
using SmartSkills.Core.Scanning;

// Shared JSON serializer options for consistent formatting
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

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

var rootCommand = new RootCommand("SmartSkills - Intelligent skill installer for .NET and Node.js projects")
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
    if (result.Installed.Count > 0)
    {
        Console.WriteLine($"Installed ({result.Installed.Count}):");
        foreach (var s in result.Installed)
            Console.WriteLine($"  + {s}");
    }
    if (result.Updated.Count > 0)
    {
        Console.WriteLine($"Updated ({result.Updated.Count}):");
        foreach (var s in result.Updated)
            Console.WriteLine($"  ~ {s}");
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
    skillNameArg,
    projectOption
};

rootCommand.Subcommands.Add(uninstallCommand);

uninstallCommand.SetAction(async (parseResult, cancellationToken) =>
{
    bool verbose = parseResult.GetValue(verboseOption);
    string skillName = parseResult.GetValue(skillNameArg)!;
    string? baseDir = parseResult.GetValue(baseDirOption);
    string? projectPath = parseResult.GetValue(projectOption);

    using var host = CreateHost(verbose, baseDir);
    var installer = host.Services.GetRequiredService<SmartSkills.Core.Installation.ISkillInstaller>();

    var resolvedPath = ResolveProjectPath(projectPath);
    await installer.UninstallAsync(skillName, resolvedPath, cancellationToken);
    Console.WriteLine($"Uninstalled skill: {skillName}");
});

// restore command
var restoreCommand = new Command("restore", "Restore skills from the lock file")
{
    projectOption
};

rootCommand.Subcommands.Add(restoreCommand);

restoreCommand.SetAction(async (parseResult, cancellationToken) =>
{
    bool verbose = parseResult.GetValue(verboseOption);
    string? projectPath = parseResult.GetValue(projectOption);
    string? baseDir = parseResult.GetValue(baseDirOption);

    using var host = CreateHost(verbose, baseDir);
    var installer = host.Services.GetRequiredService<SmartSkills.Core.Installation.ISkillInstaller>();

    projectPath = ResolveProjectPath(projectPath);

    var result = await installer.RestoreAsync(projectPath, cancellationToken);

    if (result.Restored.Count > 0)
    {
        Console.WriteLine($"Restored ({result.Restored.Count}):");
        foreach (var s in result.Restored)
            Console.WriteLine($"  + {s}");
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
    if (result.Restored.Count == 0 && result.SkippedUpToDate.Count == 0 && result.Failed.Count == 0)
    {
        Console.WriteLine("No skills found in lock file.");
    }
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

    IReadOnlyList<ProjectPackages> results;

    if (!string.IsNullOrEmpty(projectPath))
    {
        projectPath = Path.GetFullPath(projectPath);
        logger.LogInformation("Scanning: {Path}", projectPath);

        if (Directory.Exists(projectPath))
        {
            results = await scanner.ScanDirectoryAsync(projectPath, cancellationToken);
        }
        else if (projectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                 projectPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
        {
            results = await scanner.ScanSolutionAsync(projectPath, cancellationToken);
        }
        else
        {
            var single = await scanner.ScanProjectAsync(projectPath, cancellationToken);
            results = [single];
        }
    }
    else
    {
        var dir = Directory.GetCurrentDirectory();
        logger.LogInformation("Scanning directory: {Path}", dir);
        results = await scanner.ScanDirectoryAsync(dir, cancellationToken);
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
                p.RequestedVersion,
                p.Ecosystem
            })
        });
        Console.WriteLine(JsonSerializer.Serialize(output, jsonOptions));
    }
    else
    {
        foreach (var project in results)
        {
            Console.WriteLine();
            Console.WriteLine($"Project: {project.ProjectPath}");
            Console.WriteLine(new string('-', 90));
            Console.WriteLine($"{"Package",-40} {"Version",-15} {"Ecosystem",-10} {"Type",-12} {"Framework"}");
            Console.WriteLine(new string('-', 90));

            foreach (var pkg in project.Packages)
            {
                var type = pkg.IsTransitive ? "Transitive" : "Direct";
                Console.WriteLine($"{pkg.Name,-40} {pkg.Version,-15} {pkg.Ecosystem,-10} {type,-12} {pkg.TargetFramework}");
            }
        }
    }
});

// list command
var listCommand = new Command("list", "List installed skills")
{
    projectOption,
    jsonOption
};

rootCommand.Subcommands.Add(listCommand);

listCommand.SetAction(async (parseResult, cancellationToken) =>
{
    bool verbose = parseResult.GetValue(verboseOption);
    bool jsonOutput = parseResult.GetValue(jsonOption);
    string? baseDir = parseResult.GetValue(baseDirOption);
    string? projectPath = parseResult.GetValue(projectOption);

    using var host = CreateHost(verbose, baseDir);
    var lockFileStore = host.Services.GetRequiredService<SmartSkills.Core.Installation.ISkillLockFileStore>();

    var resolvedPath = ResolveProjectPath(projectPath);
    var lockFileDir = Directory.Exists(resolvedPath) ? resolvedPath : Path.GetDirectoryName(resolvedPath)!;
    var lockFile = await lockFileStore.LoadAsync(lockFileDir, cancellationToken);

    if (jsonOutput)
    {
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(lockFile.Skills, jsonOptions));
    }
    else if (lockFile.Skills.Count == 0)
    {
        Console.WriteLine("No skills installed.");
    }
    else
    {
        Console.WriteLine($"{"Name",-30} {"Path",-40} {"SHA"}");
        Console.WriteLine(new string('-', 90));
        foreach (var (name, entry) in lockFile.Skills)
        {
            var sha = entry.CommitSha.Length >= 8 ? entry.CommitSha[..8] : entry.CommitSha;
            Console.WriteLine($"{name,-30} {entry.SkillPath,-40} {sha}");
        }
    }
});

// status command
var checkRemoteOption = new Option<bool>("--check-remote")
{
    Description = "Check remote repositories for available updates"
};

var statusCommand = new Command("status", "Show status of installed skills and available updates")
{
    projectOption,
    jsonOption,
    checkRemoteOption
};

rootCommand.Subcommands.Add(statusCommand);

statusCommand.SetAction(async (parseResult, cancellationToken) =>
{
    bool verbose = parseResult.GetValue(verboseOption);
    bool jsonOutput = parseResult.GetValue(jsonOption);
    bool checkRemote = parseResult.GetValue(checkRemoteOption);
    string? projectPath = parseResult.GetValue(projectOption);
    string? baseDir = parseResult.GetValue(baseDirOption);

    using var host = CreateHost(verbose, baseDir);
    var lockFileStore = host.Services.GetRequiredService<SmartSkills.Core.Installation.ISkillLockFileStore>();
    var providerFactory = host.Services.GetRequiredService<SmartSkills.Core.Providers.ISkillSourceProviderFactory>();

    var resolvedPath = ResolveProjectPath(projectPath);
    var lockFileDir = Directory.Exists(resolvedPath) ? resolvedPath : Path.GetDirectoryName(resolvedPath)!;
    var lockFile = await lockFileStore.LoadAsync(lockFileDir, cancellationToken);

    if (lockFile.Skills.Count == 0)
    {
        Console.WriteLine("No skills in lock file.");
        return;
    }

    var totalCount = lockFile.Skills.Count;
    var upToDateCount = 0;
    var modifiedCount = 0;
    var missingCount = 0;
    var updateAvailableCount = 0;

    Console.WriteLine($"{"Name",-30} {"Status",-20} {"SHA",-12}");
    Console.WriteLine(new string('-', 65));

    foreach (var (skillName, entry) in lockFile.Skills)
    {
        var installDir = Path.Combine(lockFileDir, ".agents", "skills", skillName);
        var sha = entry.CommitSha.Length >= 8 ? entry.CommitSha[..8] : entry.CommitSha;
        string status;

        if (!Directory.Exists(installDir))
        {
            status = "Missing";
            missingCount++;
        }
        else
        {
            var currentHash = SmartSkills.Core.Installation.SkillContentHasher.ComputeHash(installDir);
            if (currentHash != entry.LocalContentHash)
            {
                status = "Modified";
                modifiedCount++;
            }
            else
            {
                status = "Up-to-date";
                upToDateCount++;
            }
        }

        if (checkRemote && status != "Missing")
        {
            try
            {
                var provider = providerFactory.CreateFromRepoUrl(entry.RemoteUrl);
                var remoteSha = await provider.GetLatestCommitShaAsync(entry.SkillPath, cancellationToken);
                if (remoteSha != entry.CommitSha)
                {
                    status += " (update available)";
                    updateAvailableCount++;
                }
            }
#pragma warning disable CA1031
            catch
#pragma warning restore CA1031
            {
                status += " (remote check failed)";
            }
        }

        Console.WriteLine($"{skillName,-30} {status,-20} {sha,-12}");
    }

    Console.WriteLine();
    Console.WriteLine($"Total: {totalCount} | Up-to-date: {upToDateCount} | Modified: {modifiedCount} | Missing: {missingCount}" +
        (checkRemote ? $" | Updates: {updateAvailableCount}" : ""));
});

rootCommand.SetAction(parseResult =>
{
    Console.WriteLine("SmartSkills CLI - Use --help for usage information.");
});

return await rootCommand.Parse(args).InvokeAsync();

static IHost CreateHost(bool verbose, string? baseDir = null)
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

    // Default to current directory — ScanDirectoryAsync / SkillInstaller will auto-detect projects
    return Directory.GetCurrentDirectory();
}
