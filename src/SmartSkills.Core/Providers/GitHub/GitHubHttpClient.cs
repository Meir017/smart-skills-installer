using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Providers.GitHub;

/// <summary>
/// HTTP client wrapper for GitHub REST API (public repos only).
/// </summary>
public sealed class GitHubHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubHttpClient> _logger;

    public GitHubHttpClient(ILogger<GitHubHttpClient> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SmartSkills", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
    }

    public async Task<JsonDocument> GetJsonAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GET {Url}", url);
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    public async Task<Stream> GetStreamAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GET (stream) {Url}", url);
        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task<string> GetStringAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GET (string) {Url}", url);
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public void Dispose() => _httpClient.Dispose();
}
