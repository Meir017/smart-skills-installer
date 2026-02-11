using System.CommandLine;
using Microsoft.Extensions.Logging;
using SmartSkills.Cli;
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

// scan command
var pathOption = new Option<DirectoryInfo?>(
    "--path",
    "Path to a .NET project or solution directory");

var scanCommand = new Command("scan", "Scan project for installed libraries")
{
    pathOption,
};

scanCommand.SetHandler((verbose, path) =>
{
    using var loggerFactory = LoggingSetup.CreateLoggerFactory(verbose);
    var logger = loggerFactory.CreateLogger("SmartSkills.Scan");

    var targetPath = path?.FullName ?? Directory.GetCurrentDirectory();
    logger.LogDebug("Scanning path: {Path}", targetPath);

    try
    {
        var resolvedPath = ProjectDiscovery.ResolvePath(targetPath);
        logger.LogInformation("Found project: {ProjectPath}", resolvedPath);
        logger.LogInformation("Scanning for packages...");
        // Scanning pipeline will be implemented in subsequent tasks
        logger.LogInformation("Scan complete.");
    }
    catch (Exception ex) when (ex is DirectoryNotFoundException or InvalidOperationException)
    {
        logger.LogError("{Message}", ex.Message);
    }
}, verboseOption, pathOption);

rootCommand.AddCommand(scanCommand);

return await rootCommand.InvokeAsync(args);
