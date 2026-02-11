using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Fetching;

public class AdoContentFetcher : IRemoteContentFetcher, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AdoContentFetcher> _logger;

    public AdoContentFetcher(ILogger<AdoContentFetcher> logger, string token)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> FetchStringAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching from ADO: {Url}", url);
        var response = await _httpClient.GetAsync(url, cancellationToken);
        HandleErrorResponse(response, url);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Downloading from ADO: {Url} -> {Path}", url, destinationPath);
        var response = await _httpClient.GetAsync(url, cancellationToken);
        HandleErrorResponse(response, url);

        var dir = Path.GetDirectoryName(destinationPath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        await using var fs = File.Create(destinationPath);
        await response.Content.CopyToAsync(fs, cancellationToken);
    }

    /// <summary>
    /// Builds an ADO Git Items API URL for fetching file content.
    /// </summary>
    public static string BuildItemUrl(string org, string project, string repo, string path, string? branch = null)
    {
        var url = $"https://dev.azure.com/{org}/{project}/_apis/git/repositories/{repo}/items?path={Uri.EscapeDataString(path)}&api-version=7.0";
        if (branch is not null)
            url += $"&versionDescriptor.version={Uri.EscapeDataString(branch)}&versionDescriptor.versionType=branch";
        return url;
    }

    /// <summary>
    /// Builds an ADO Git Items API URL for listing directory contents.
    /// </summary>
    public static string BuildListUrl(string org, string project, string repo, string path, string? branch = null)
    {
        var url = $"https://dev.azure.com/{org}/{project}/_apis/git/repositories/{repo}/items?scopePath={Uri.EscapeDataString(path)}&recursionLevel=Full&api-version=7.0";
        if (branch is not null)
            url += $"&versionDescriptor.version={Uri.EscapeDataString(branch)}&versionDescriptor.versionType=branch";
        return url;
    }

    /// <summary>
    /// Parses "ado:org/project/repo[@branch]" source format.
    /// </summary>
    public static (string Org, string Project, string Repo, string Branch) ParseAdoSource(string source)
    {
        var value = source;
        if (value.StartsWith("ado:", StringComparison.OrdinalIgnoreCase))
            value = value["ado:".Length..];

        var branch = "main";
        var atIndex = value.IndexOf('@');
        if (atIndex >= 0)
        {
            branch = value[(atIndex + 1)..];
            value = value[..atIndex];
        }

        var parts = value.Split('/');
        if (parts.Length != 3 || parts.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException($"Invalid ADO source format: '{source}'. Expected 'ado:org/project/repo[@branch]'.");

        return (parts[0], parts[1], parts[2], branch);
    }

    private static void HandleErrorResponse(HttpResponseMessage response, string url)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => $"Authentication failed for ADO resource: {url}. Check your ADO token.",
            HttpStatusCode.Forbidden => $"Access denied for ADO resource: {url}. Verify token permissions.",
            HttpStatusCode.NotFound => $"Resource not found in ADO: {url}.",
            _ => $"ADO request failed with status {(int)response.StatusCode} for: {url}."
        };

        throw new HttpRequestException(message, null, response.StatusCode);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
