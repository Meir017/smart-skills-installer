using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using SmartSkills.Core.Resilience;

namespace SmartSkills.Core.Providers.AzureDevOps;

/// <summary>
/// HTTP client wrapper for Azure DevOps REST API using DefaultAzureCredential.
/// </summary>
public sealed class AdoHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly RetryPolicy _retryPolicy;
    private readonly ILogger<AdoHttpClient> _logger;
    private readonly TokenCredential _credential;
    private AccessToken? _cachedToken;

    private static readonly string[] AdoScopes = ["499b84ac-1321-427f-aa17-267ca6975798/.default"];

    public AdoHttpClient(ILogger<AdoHttpClient> logger, RetryPolicy? retryPolicy = null)
    {
        _logger = logger;
        _retryPolicy = retryPolicy ?? new RetryPolicy(logger: logger);
        _credential = new DefaultAzureCredential();
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (_cachedToken is null || _cachedToken.Value.ExpiresOn <= DateTimeOffset.UtcNow.AddMinutes(5))
        {
            _logger.LogDebug("Acquiring ADO token via DefaultAzureCredential");
            _cachedToken = await _credential.GetTokenAsync(
                new TokenRequestContext(AdoScopes), cancellationToken);
        }
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _cachedToken.Value.Token);
    }

    public async Task<JsonDocument> GetJsonAsync(string url, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken);
        _logger.LogDebug("GET {Url}", url);
        return await _retryPolicy.ExecuteAsync(async ct =>
        {
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }, cancellationToken);
    }

    public async Task<Stream> GetStreamAsync(string url, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken);
        _logger.LogDebug("GET (stream) {Url}", url);
        return await _retryPolicy.ExecuteAsync(async ct =>
        {
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync(ct);
        }, cancellationToken);
    }

    public async Task<string> GetStringAsync(string url, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken);
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
