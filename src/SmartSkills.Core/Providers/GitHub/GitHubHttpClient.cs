using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Providers.GitHub;

/// <summary>
/// HTTP client wrapper for GitHub REST API (public repos only).
/// The underlying <see cref="HttpClient"/> is configured and managed by <c>IHttpClientFactory</c>.
/// </summary>
public sealed class GitHubHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubHttpClient> _logger;

    public GitHubHttpClient(HttpClient httpClient, ILogger<GitHubHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

#pragma warning disable CA1054 // URI parameters should not be strings
    public async Task<JsonDocument> GetJsonAsync(string url, CancellationToken cancellationToken = default)
#pragma warning restore CA1054 // URI parameters should not be strings
    {
        _logger.LogDebug("GET {Url}", url);
        var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

#pragma warning disable CA1054 // URI parameters should not be strings
    public async Task<Stream> GetStreamAsync(string url, CancellationToken cancellationToken = default)
#pragma warning restore CA1054 // URI parameters should not be strings
    {
        _logger.LogDebug("GET (stream) {Url}", url);
        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }

#pragma warning disable CA1054 // URI parameters should not be strings
    public async Task<string> GetStringAsync(string url, CancellationToken cancellationToken = default)
#pragma warning restore CA1054 // URI parameters should not be strings
    {
        _logger.LogDebug("GET (string) {Url}", url);
        var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}
