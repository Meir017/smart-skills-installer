using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartSkills.Core.Registry;

namespace SmartSkills.Core.Providers.AzureDevOps;

/// <summary>
/// ISkillSourceProvider implementation using Azure DevOps Git REST API.
/// </summary>
public sealed class AdoSkillSourceProvider : ISkillSourceProvider, IDisposable
{
    private readonly string _organization;
    private readonly string _project;
    private readonly string _repository;
    private readonly string _branch;
    private readonly string _registryIndexPath;
    private readonly AdoHttpClient _client;
    private readonly ILogger<AdoSkillSourceProvider> _logger;

    public string ProviderType => "azuredevops";

    public AdoSkillSourceProvider(
        string organization,
        string project,
        string repository,
        string? branch,
        string? registryIndexPath,
        ILogger<AdoSkillSourceProvider> logger,
        ILogger<AdoHttpClient> httpClientLogger)
    {
        _organization = organization;
        _project = project;
        _repository = repository;
        _branch = branch ?? "main";
        _registryIndexPath = registryIndexPath ?? "skills-registry.json";
        _logger = logger;
        _client = new AdoHttpClient(httpClientLogger);
    }

    private string BaseUrl => $"https://dev.azure.com/{_organization}/{_project}/_apis/git/repositories/{_repository}";

    public async Task<IReadOnlyList<RegistryEntry>> GetRegistryIndexAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/items?path={Uri.EscapeDataString(_registryIndexPath)}&versionDescriptor.version={_branch}&api-version=7.0";
        _logger.LogInformation("Fetching registry index from ADO: {Url}", url);
        var json = await _client.GetStringAsync(url, cancellationToken);
        return RegistryIndexParser.Parse(json);
    }

    public async Task<IReadOnlyList<string>> ListSkillFilesAsync(string skillPath, string? commitSha = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(skillPath);

        var versionType = commitSha is not null ? "commit" : "branch";
        var versionValue = commitSha ?? _branch;
        var url = $"{BaseUrl}/items?scopePath={Uri.EscapeDataString(skillPath)}&recursionLevel=Full&versionDescriptor.versionType={versionType}&versionDescriptor.version={versionValue}&api-version=7.0";
        using var doc = await _client.GetJsonAsync(url, cancellationToken);

        var files = new List<string>();
        var prefix = skillPath.TrimEnd('/') + "/";

        if (doc.RootElement.TryGetProperty("value", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                var isFolder = item.TryGetProperty("isFolder", out var f) && f.GetBoolean();
                if (isFolder)
                    continue;

                var path = item.GetProperty("path").GetString()!;
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(path[prefix.Length..]);
                }
            }
        }

        return files;
    }

    public async Task<Stream> DownloadFileAsync(string filePath, string? commitSha = null, CancellationToken cancellationToken = default)
    {
        var versionType = commitSha is not null ? "commit" : "branch";
        var versionValue = commitSha ?? _branch;
        var url = $"{BaseUrl}/items?path={Uri.EscapeDataString(filePath)}&download=true&versionDescriptor.versionType={versionType}&versionDescriptor.version={versionValue}&api-version=7.0";
        return await _client.GetStreamAsync(url, cancellationToken);
    }

    public async Task<string> GetLatestCommitShaAsync(string skillPath, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/commits?searchCriteria.itemPath={Uri.EscapeDataString(skillPath)}&searchCriteria.itemVersion.version={_branch}&$top=1&api-version=7.0";
        using var doc = await _client.GetJsonAsync(url, cancellationToken);

        if (doc.RootElement.TryGetProperty("value", out var commits) && commits.GetArrayLength() > 0)
        {
            return commits[0].GetProperty("commitId").GetString()!;
        }

        throw new InvalidOperationException($"No commits found for path: {skillPath}");
    }

    public void Dispose() => _client.Dispose();
}
