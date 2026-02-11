using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartSkills.Core;

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

rootCommand.SetAction(parseResult =>
{
    bool verbose = parseResult.GetValue(verboseOption);

    var host = Host.CreateDefaultBuilder()
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

    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("SmartSkills CLI started");
    logger.LogDebug("Verbose logging enabled");

    host.Dispose();
});

return rootCommand.Parse(args).Invoke();
