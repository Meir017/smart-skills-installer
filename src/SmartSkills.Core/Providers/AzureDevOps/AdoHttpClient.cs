using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Providers.AzureDevOps;

/// <summary>
/// HTTP client wrapper for Azure DevOps REST API with PAT authentication.
/// </summary>
public sealed class AdoHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AdoHttpClient> _logger;

    public AdoHttpClient(string? personalAccessToken, ILogger<AdoHttpClient> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrEmpty(personalAccessToken))
        {
            var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{personalAccessToken}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);
        }
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
