using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartSkills.Core.Models;
using SmartSkills.Core.Registry;

namespace SmartSkills.Core.Fetching;

public class RegistryFetchService
{
    private readonly IRemoteContentFetcher _fetcher;
    private readonly SkillRegistryParser _parser = new();
    private readonly ILogger<RegistryFetchService> _logger;
    private readonly string _cacheDir;
    private readonly TimeSpan _cacheTtl;

    public RegistryFetchService(
        IRemoteContentFetcher fetcher,
        ILogger<RegistryFetchService> logger,
        string? cacheDir = null,
        TimeSpan? cacheTtl = null)
    {
        _fetcher = fetcher;
        _logger = logger;
        _cacheDir = cacheDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".smart-skills", "cache");
        _cacheTtl = cacheTtl ?? TimeSpan.FromHours(1);
    }

    /// <summary>
    /// Fetches the skill registry from a GitHub source. Format: "github:owner/repo[@branch]"
    /// </summary>
    public async Task<SkillRegistry> FetchRegistryAsync(string source, CancellationToken cancellationToken = default)
    {
        var (owner, repo, branch) = ParseGitHubSource(source);
        var url = GitHubContentFetcher.BuildRawUrl(owner, repo, branch, "skills-registry.json");
        var cacheKey = $"registry_{owner}_{repo}_{branch}.json";

        // Try cache first
        var cached = TryReadCache(cacheKey);
        if (cached is not null)
        {
            _logger.LogDebug("Using cached registry for {Source}", source);
            return cached;
        }

        // Fetch from remote
        _logger.LogDebug("Fetching registry from {Url}", url);
        string json;
        try
        {
            json = await _fetcher.FetchStringAsync(url, cancellationToken);
        }
        catch (Exception ex)
        {
            // Try stale cache on network failure
            var stale = TryReadCache(cacheKey, ignoreExpiry: true);
            if (stale is not null)
            {
                _logger.LogWarning("Network error fetching registry, using stale cache: {Error}", ex.Message);
                return stale;
            }
            throw;
        }

        var registry = _parser.Parse(json);

        // Write to cache
        WriteCache(cacheKey, json);
        _logger.LogDebug("Registry cached with TTL {Ttl}", _cacheTtl);

        return registry;
    }

    public static (string Owner, string Repo, string Branch) ParseGitHubSource(string source)
    {
        // Format: "github:owner/repo[@branch]"
        var value = source;
        if (value.StartsWith("github:", StringComparison.OrdinalIgnoreCase))
            value = value["github:".Length..];

        var branch = "main";
        var atIndex = value.IndexOf('@');
        if (atIndex >= 0)
        {
            branch = value[(atIndex + 1)..];
            value = value[..atIndex];
        }

        var parts = value.Split('/');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            throw new ArgumentException($"Invalid GitHub source format: '{source}'. Expected 'github:owner/repo[@branch]'.");

        return (parts[0], parts[1], branch);
    }

    private SkillRegistry? TryReadCache(string cacheKey, bool ignoreExpiry = false)
    {
        var cachePath = Path.Combine(_cacheDir, cacheKey);
        if (!File.Exists(cachePath))
            return null;

        if (!ignoreExpiry)
        {
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath);
            if (age > _cacheTtl)
            {
                _logger.LogDebug("Cache expired for {Key} (age: {Age})", cacheKey, age);
                return null;
            }
        }

        try
        {
            var json = File.ReadAllText(cachePath);
            return _parser.Parse(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to read cache for {Key}: {Error}", cacheKey, ex.Message);
            return null;
        }
    }

    private void WriteCache(string cacheKey, string json)
    {
        try
        {
            Directory.CreateDirectory(_cacheDir);
            var cachePath = Path.Combine(_cacheDir, cacheKey);
            File.WriteAllText(cachePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to write cache for {Key}: {Error}", cacheKey, ex.Message);
        }
    }
}
