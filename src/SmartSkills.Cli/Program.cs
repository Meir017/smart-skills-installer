using System.CommandLine;
using Microsoft.Extensions.Logging;
using SmartSkills.Cli;
using SmartSkills.Core.Fetching;
using SmartSkills.Core.Installation;
using SmartSkills.Core.Registry;
using SmartSkills.Core.Scanning;

var verboseOption = new Option<bool>(
    "--verbose",
    "Enable detailed logging");

var configOption = new Option<FileInfo?>(
    "--config",
    "Path to a custom configuration file");

var dryRunOption = new Option<bool>(
    "--dry-run",
    "Preview actions without executing");

var rootCommand = new RootCommand("Smart Skills Installer - Automatically install agent skills based on project dependencies")
{
    verboseOption,
    configOption,
    dryRunOption,
};

rootCommand.SetHandler((verbose, config, dryRun) =>
{
    using var loggerFactory = LoggingSetup.CreateLoggerFactory(verbose);
    var logger = loggerFactory.CreateLogger("SmartSkills");

    logger.LogInformation("Smart Skills Installer CLI started");
    logger.LogDebug("Config: {ConfigPath}", config?.FullName ?? "(default)");
    logger.LogDebug("Dry run: {DryRun}", dryRun);
    logger.LogInformation("No command specified. Run with --help for usage information.");
}, verboseOption, configOption, dryRunOption);

// Shared options
var pathOption = new Option<DirectoryInfo?>(
    "--path",
    "Path to a .NET project or solution directory");

var outputOption = new Option<string?>(
    "--output",
    "Output format (table or json)");

// scan command
var scanCommand = new Command("scan", "Scan project for installed libraries")
{
    pathOption,
    outputOption,
};

scanCommand.SetHandler(async (verbose, path, output) =>
{
    using var loggerFactory = LoggingSetup.CreateLoggerFactory(verbose);
    var logger = loggerFactory.CreateLogger("SmartSkills.Scan");
    var scanner = new ProjectScanner(loggerFactory.CreateLogger<ProjectScanner>());

    var targetPath = path?.FullName ?? Directory.GetCurrentDirectory();
    logger.LogDebug("Scanning path: {Path}", targetPath);

    try
    {
        var resolvedPath = ProjectDiscovery.ResolvePath(targetPath);
        logger.LogInformation("Found project: {ProjectPath}", resolvedPath);

        var packages = await scanner.ScanAsync(resolvedPath);

        if (string.Equals(output, "json", StringComparison.OrdinalIgnoreCase))
        {
            ScanOutputFormatter.WriteJson(packages, Console.Out);
        }
        else
        {
            ScanOutputFormatter.WriteTable(packages, Console.Out);
        }
    }
    catch (Exception ex) when (ex is DirectoryNotFoundException or InvalidOperationException)
    {
        logger.LogError("{Message}", ex.Message);
    }
}, verboseOption, pathOption, outputOption);

rootCommand.AddCommand(scanCommand);

// install command
var sourceOption = new Option<string?>(
    "--source",
    "Registry source (e.g., github:owner/repo or ado:org/project/repo)");

var forceOption = new Option<bool>(
    "--force",
    "Force reinstall of already-installed skills");

var yesOption = new Option<bool>(
    "--yes",
    "Skip confirmation prompt");

var installCommand = new Command("install", "Install skills matching project libraries")
{
    pathOption,
    sourceOption,
    forceOption,
    yesOption,
};

installCommand.SetHandler(async (verbose, dryRun, path, source, force, yes) =>
{
    using var loggerFactory = LoggingSetup.CreateLoggerFactory(verbose);
    var logger = loggerFactory.CreateLogger("SmartSkills.Install");

    if (source is null)
    {
        logger.LogError("No source specified. Use --source to provide a registry source (e.g., --source github:owner/repo).");
        return;
    }

    var targetPath = path?.FullName ?? Directory.GetCurrentDirectory();

    try
    {
        // 1. Scan
        var scanner = new ProjectScanner(loggerFactory.CreateLogger<ProjectScanner>());
        var resolvedPath = ProjectDiscovery.ResolvePath(targetPath);
        logger.LogInformation("Scanning: {Path}", resolvedPath);
        var packages = await scanner.ScanAsync(resolvedPath);
        logger.LogInformation("Found {Count} package(s)", packages.Count);

        if (packages.Count == 0)
        {
            logger.LogInformation("No packages found. Nothing to install.");
            return;
        }

        // 2. Fetch registry
        using var fetcher = new GitHubContentFetcher(loggerFactory.CreateLogger<GitHubContentFetcher>());
        var registryService = new RegistryFetchService(fetcher, loggerFactory.CreateLogger<RegistryFetchService>());
        var registry = await registryService.FetchRegistryAsync(source);

        // 3. Find manifest URLs
        var registryParser = new SkillRegistryParser();
        var manifestUrls = registryParser.FindAllManifestUrls(registry, packages.Select(p => p.Name));

        if (manifestUrls.Count == 0)
        {
            logger.LogInformation("No matching skills found in registry.");
            return;
        }

        // 4. Fetch manifests and match
        var manifests = new List<SmartSkills.Core.Models.SkillManifest>();
        foreach (var url in manifestUrls)
        {
            try
            {
                var json = await fetcher.FetchStringAsync(url);
                var (manifest, errors) = SkillManifestValidator.ParseAndValidate(json);
                if (manifest is not null)
                    manifests.Add(manifest);
                else
                    logger.LogWarning("Invalid manifest at {Url}: {Errors}", url, string.Join("; ", errors));
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to fetch manifest from {Url}: {Error}", url, ex.Message);
            }
        }

        var engine = new SkillMatchingEngine();
        var resolved = engine.Match(packages, registry, manifests);

        if (resolved.Count == 0)
        {
            logger.LogInformation("No compatible skills found for your project dependencies.");
            return;
        }

        // 5. Filter already installed
        var storage = new LocalSkillStorage();
        var toInstall = force
            ? resolved.ToList()
            : resolved.Where(s => !storage.IsInstalled(s.SkillId, s.Version)).ToList();

        if (toInstall.Count == 0)
        {
            logger.LogInformation("All matching skills are already installed.");
            return;
        }

        // 6. Preview
        Console.WriteLine();
        Console.WriteLine("Skills to install:");
        foreach (var skill in toInstall)
        {
            Console.WriteLine($"  {skill.Name} v{skill.Version} (matched by: {string.Join(", ", skill.MatchedLibraries)})");
        }
        Console.WriteLine();

        if (dryRun)
        {
            logger.LogInformation("Dry run: no changes made.");
            return;
        }

        // 7. Confirm
        if (!yes)
        {
            Console.Write($"Install {toInstall.Count} skill(s)? [y/N] ");
            var response = Console.ReadLine();
            if (!string.Equals(response?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Installation cancelled.");
                return;
            }
        }

        // 8. Download and install
        var downloader = new SkillPackageDownloader(fetcher, loggerFactory.CreateLogger<SkillPackageDownloader>());
        foreach (var skill in toInstall)
        {
            if (skill.Manifest is null) continue;
            var skillDir = storage.GetSkillDir(skill.SkillId);
            await downloader.DownloadSkillAsync(skill.Manifest, skillDir);
            storage.RecordInstall(skill.SkillId, skill.Version, source);
        }

        logger.LogInformation("Successfully installed {Count} skill(s).", toInstall.Count);
    }
    catch (Exception ex)
    {
        logger.LogError("Installation failed: {Message}", ex.Message);
    }
}, verboseOption, dryRunOption, pathOption, sourceOption, forceOption, yesOption);

rootCommand.AddCommand(installCommand);

return await rootCommand.InvokeAsync(args);
