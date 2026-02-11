using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartSkills.Core.Resilience;

namespace SmartSkills.Core.Providers.GitHub;

/// <summary>
/// HTTP client wrapper for GitHub REST API (public repos only).
/// </summary>
public sealed class GitHubHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly RetryPolicy _retryPolicy;
    private readonly ILogger<GitHubHttpClient> _logger;

    public GitHubHttpClient(ILogger<GitHubHttpClient> logger, RetryPolicy? retryPolicy = null)
    {
        _logger = logger;
        _retryPolicy = retryPolicy ?? new RetryPolicy(logger: logger);
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SmartSkills", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
    }

#pragma warning disable CA1054 // URI parameters should not be strings
    public async Task<JsonDocument> GetJsonAsync(string url, CancellationToken cancellationToken = default)
#pragma warning restore CA1054 // URI parameters should not be strings
    {
        _logger.LogDebug("GET {Url}", url);
        return await _retryPolicy.ExecuteAsync(async ct =>
        {
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }, cancellationToken);
    }

#pragma warning disable CA1054 // URI parameters should not be strings
    public async Task<Stream> GetStreamAsync(string url, CancellationToken cancellationToken = default)
#pragma warning restore CA1054 // URI parameters should not be strings
    {
        _logger.LogDebug("GET (stream) {Url}", url);
        return await _retryPolicy.ExecuteAsync(async ct =>
        {
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync(ct);
        }, cancellationToken);
    }

#pragma warning disable CA1054 // URI parameters should not be strings
    public async Task<string> GetStringAsync(string url, CancellationToken cancellationToken = default)
#pragma warning restore CA1054 // URI parameters should not be strings
    {
        _logger.LogDebug("GET (string) {Url}", url);
        return await _retryPolicy.ExecuteAsync(async ct =>
        {
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }, cancellationToken);
    }

    public void Dispose() => _httpClient.Dispose();
}
