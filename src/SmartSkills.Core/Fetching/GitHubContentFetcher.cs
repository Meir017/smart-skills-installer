using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Fetching;

public class GitHubContentFetcher : IRemoteContentFetcher, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubContentFetcher> _logger;

    public GitHubContentFetcher(ILogger<GitHubContentFetcher> logger, string? token = null)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SmartSkillsInstaller", "0.1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _logger.LogDebug("GitHub client configured with authentication token");
        }
    }

    public async Task<string> FetchStringAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching: {Url}", url);
        var response = await SendWithRateLimitRetryAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Downloading: {Url} -> {Path}", url, destinationPath);
        var response = await SendWithRateLimitRetryAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dir = Path.GetDirectoryName(destinationPath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        await using var fs = File.Create(destinationPath);
        await response.Content.CopyToAsync(fs, cancellationToken);
    }

    /// <summary>
    /// Builds a raw.githubusercontent.com URL for fetching file content.
    /// </summary>
    public static string BuildRawUrl(string owner, string repo, string branch, string path)
    {
        return $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{path}";
    }

    /// <summary>
    /// Builds a GitHub API URL for listing repository contents.
    /// </summary>
    public static string BuildApiContentsUrl(string owner, string repo, string path, string? branch = null)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}";
        if (branch is not null)
            url += $"?ref={branch}";
        return url;
    }

    private async Task<HttpResponseMessage> SendWithRateLimitRetryAsync(string url, CancellationToken cancellationToken)
    {
        const int maxRetries = 3;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new HttpRequestException($"Resource not found: {url}", null, HttpStatusCode.NotFound);
            }

            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
            {
                if (attempt == maxRetries)
                {
                    _logger.LogError("Rate limit exceeded after {MaxRetries} retries for {Url}", maxRetries, url);
                    response.EnsureSuccessStatusCode();
                }

                var retryAfter = GetRetryAfterSeconds(response);
                _logger.LogWarning("Rate limited. Waiting {Seconds}s before retry (attempt {Attempt}/{Max})",
                    retryAfter, attempt + 1, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(retryAfter), cancellationToken);
                continue;
            }

            return response;
        }

        throw new InvalidOperationException("Unexpected: exceeded retry loop without returning.");
    }

    private static int GetRetryAfterSeconds(HttpResponseMessage response)
    {
        // Check Retry-After header
        if (response.Headers.RetryAfter?.Delta is { } delta)
            return (int)Math.Ceiling(delta.TotalSeconds);

        // Check X-RateLimit-Reset (Unix timestamp)
        if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues))
        {
            if (long.TryParse(resetValues.FirstOrDefault(), out var resetTimestamp))
            {
                var resetTime = DateTimeOffset.FromUnixTimeSeconds(resetTimestamp);
                var wait = (int)Math.Ceiling((resetTime - DateTimeOffset.UtcNow).TotalSeconds);
                return Math.Max(wait, 1);
            }
        }

        // Default exponential backoff: 1s, 2s, 4s
        return 2;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
