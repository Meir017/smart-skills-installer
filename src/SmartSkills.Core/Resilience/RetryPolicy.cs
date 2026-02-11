using System.Net;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Resilience;

/// <summary>
/// Configurable retry policy with exponential backoff and jitter for HTTP operations.
/// </summary>
public sealed class RetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;
    private readonly ILogger _logger;

    public RetryPolicy(int maxRetries = 3, TimeSpan? baseDelay = null, ILogger? logger = null)
    {
        _maxRetries = maxRetries;
        _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return await action(cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < _maxRetries && IsTransient(ex))
            {
                var delay = GetDelay(attempt);
                _logger.LogWarning("Transient HTTP failure (attempt {Attempt}/{Max}), retrying in {Delay}ms: {Message}",
                    attempt + 1, _maxRetries, delay.TotalMilliseconds, ex.Message);
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException) when (attempt < _maxRetries && !cancellationToken.IsCancellationRequested)
            {
                var delay = GetDelay(attempt);
                _logger.LogWarning("Request timeout (attempt {Attempt}/{Max}), retrying in {Delay}ms",
                    attempt + 1, _maxRetries, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private TimeSpan GetDelay(int attempt)
    {
        var baseMs = _baseDelay.TotalMilliseconds * Math.Pow(2, attempt);
        var jitter = Random.Shared.Next(0, (int)(baseMs * 0.3));
        return TimeSpan.FromMilliseconds(baseMs + jitter);
    }

    private static bool IsTransient(HttpRequestException ex)
    {
        if (ex.StatusCode is null) return true; // Network-level failure
        return ex.StatusCode >= HttpStatusCode.InternalServerError ||
               ex.StatusCode == HttpStatusCode.RequestTimeout ||
               ex.StatusCode == (HttpStatusCode)429; // Too Many Requests
    }
}
