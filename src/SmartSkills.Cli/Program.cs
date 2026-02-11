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

var outputOption = new Option<string?>(
    "--output",
    "Output format (table or json)");

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

return await rootCommand.InvokeAsync(args);
