using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartSkills.Core.Registry;

namespace SmartSkills.Core.Providers.GitHub;

/// <summary>
/// ISkillSourceProvider implementation using GitHub REST API.
/// </summary>
public sealed class GitHubSkillSourceProvider : ISkillSourceProvider
{
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _branch;
    private readonly string _registryIndexPath;
    private readonly GitHubHttpClient _client;
    private readonly ILogger<GitHubSkillSourceProvider> _logger;

    public string ProviderType => "github";

    public GitHubSkillSourceProvider(
        string owner,
        string repo,
        string? branch,
        string? registryIndexPath,
        ILogger<GitHubSkillSourceProvider> logger,
        GitHubHttpClient httpClient)
    {
        _owner = owner;
        _repo = repo;
        _branch = branch ?? "main";
        _registryIndexPath = registryIndexPath ?? "skills-registry.json";
        _logger = logger;
        _client = httpClient;
    }

    public async Task<IReadOnlyList<RegistryEntry>> GetRegistryIndexAsync(CancellationToken cancellationToken = default)
    {
        var url = $"https://raw.githubusercontent.com/{_owner}/{_repo}/{_branch}/{_registryIndexPath}";
        _logger.LogInformation("Fetching registry index from {Url}", url);
        var json = await _client.GetStringAsync(url, cancellationToken);
        return RegistryIndexParser.Parse(json);
    }

    public async Task<IReadOnlyList<string>> ListSkillFilesAsync(string skillPath, string? commitSha = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(skillPath);

        var treeRef = commitSha ?? _branch;
        // Use Git Trees API for recursive listing
        var url = $"https://api.github.com/repos/{_owner}/{_repo}/git/trees/{treeRef}?recursive=1";
        using var doc = await _client.GetJsonAsync(url, cancellationToken);

        var files = new List<string>();
        var prefix = skillPath.TrimEnd('/') + "/";

        if (doc.RootElement.TryGetProperty("tree", out var tree))
        {
            foreach (var item in tree.EnumerateArray())
            {
                var path = item.GetProperty("path").GetString()!;
                var type = item.GetProperty("type").GetString();

                if (type == "blob" && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(path[prefix.Length..]);
                }
            }
        }

        return files;
    }

    public async Task<Stream> DownloadFileAsync(string filePath, string? commitSha = null, CancellationToken cancellationToken = default)
    {
        var fileRef = commitSha ?? _branch;
        var url = $"https://raw.githubusercontent.com/{_owner}/{_repo}/{fileRef}/{filePath}";
        return await _client.GetStreamAsync(url, cancellationToken);
    }

    public async Task<string> GetLatestCommitShaAsync(string skillPath, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.github.com/repos/{_owner}/{_repo}/commits?path={Uri.EscapeDataString(skillPath)}&per_page=1&sha={_branch}";
        using var doc = await _client.GetJsonAsync(url, cancellationToken);

        var commits = doc.RootElement;
        if (commits.GetArrayLength() == 0)
            throw new InvalidOperationException($"No commits found for path: {skillPath}");

        return commits[0].GetProperty("sha").GetString()!;
    }
}
