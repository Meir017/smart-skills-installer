using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace SmartSkills.Cli;

public static class LoggingSetup
{
    public static ILoggerFactory CreateLoggerFactory(bool verbose)
    {
        return LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
            builder.AddConsole(options =>
            {
                options.FormatterName = ConsoleFormatterNames.Simple;
            })
            .AddSimpleConsole(options =>
            {
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                options.IncludeScopes = true;
                options.SingleLine = true;
            });
        });
    }
}
