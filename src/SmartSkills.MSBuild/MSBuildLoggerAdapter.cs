using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using MEL = Microsoft.Extensions.Logging;

namespace SmartSkills.MSBuild;

/// <summary>
/// Adapts MSBuild's <see cref="TaskLoggingHelper"/> to <see cref="MEL.ILogger"/>
/// so Core services can log through MSBuild's diagnostic infrastructure.
/// </summary>
internal sealed class MSBuildLoggerAdapter : MEL.ILogger
{
    private readonly TaskLoggingHelper _log;
    private readonly string _categoryName;

    public MSBuildLoggerAdapter(TaskLoggingHelper log, string categoryName)
    {
        _log = log;
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(MEL.LogLevel logLevel) => logLevel switch
    {
        MEL.LogLevel.Trace => true,
        MEL.LogLevel.Debug => true,
        MEL.LogLevel.Information => true,
        MEL.LogLevel.Warning => true,
        MEL.LogLevel.Error => true,
        MEL.LogLevel.Critical => true,
        _ => false
    };

    public void Log<TState>(MEL.LogLevel logLevel, MEL.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = $"[{_categoryName}] {formatter(state, exception)}";

        switch (logLevel)
        {
            case MEL.LogLevel.Trace:
            case MEL.LogLevel.Debug:
                _log.LogMessage(MessageImportance.Low, message);
                break;
            case MEL.LogLevel.Information:
                _log.LogMessage(MessageImportance.Normal, message);
                break;
            case MEL.LogLevel.Warning:
                _log.LogWarning(message);
                break;
            case MEL.LogLevel.Error:
            case MEL.LogLevel.Critical:
                if (exception != null)
                    _log.LogErrorFromException(exception, showStackTrace: true);
                else
                    _log.LogError(message);
                break;
        }
    }
}

/// <summary>
/// Factory that creates <see cref="MSBuildLoggerAdapter"/> instances for Core services.
/// </summary>
internal sealed class MSBuildLoggerFactory : MEL.ILoggerFactory
{
    private readonly TaskLoggingHelper _log;

    public MSBuildLoggerFactory(TaskLoggingHelper log) => _log = log;

    public MEL.ILogger CreateLogger(string categoryName) => new MSBuildLoggerAdapter(_log, categoryName);

    public void AddProvider(MEL.ILoggerProvider provider) { }
    public void Dispose() { }
}
