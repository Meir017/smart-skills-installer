using System.CommandLine;

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
    Console.WriteLine("SmartSkills CLI - Use --help for usage information.");
});

return rootCommand.Parse(args).Invoke();
