using System.CommandLine;
using Microsoft.Extensions.Logging;
using SmartSkills.Cli;

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
    logger.LogDebug("Verbose mode enabled");
    logger.LogDebug("Config: {ConfigPath}", config?.FullName ?? "(default)");
    logger.LogDebug("Dry run: {DryRun}", dryRun);
    logger.LogInformation("No command specified. Run with --help for usage information.");
}, verboseOption, configOption, dryRunOption);

return await rootCommand.InvokeAsync(args);
