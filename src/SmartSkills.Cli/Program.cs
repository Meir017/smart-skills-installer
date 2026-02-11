using System.CommandLine;

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
    Console.WriteLine("Smart Skills Installer CLI");
    Console.WriteLine($"  Verbose: {verbose}");
    Console.WriteLine($"  Config:  {config?.FullName ?? "(default)"}");
    Console.WriteLine($"  Dry Run: {dryRun}");
}, verboseOption, configOption, dryRunOption);

return await rootCommand.InvokeAsync(args);
