using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Resilience;

/// <summary>
/// File-system-based cache for registry indexes and skill manifests.
/// </summary>
public sealed class LocalCache
{
    private readonly string _cacheDirectory;
    private readonly ILogger<LocalCache> _logger;

    public LocalCache(string cacheDirectory, ILogger<LocalCache> logger)
    {
        _cacheDirectory = cacheDirectory;
        _logger = logger;
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = GetCachePath(key);
        if (!File.Exists(path))
            return null;

        _logger.LogDebug("Cache hit: {Key}", key);
        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var path = GetCachePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, value, cancellationToken);
        _logger.LogDebug("Cached: {Key}", key);
    }

    public void Invalidate(string key)
    {
        var path = GetCachePath(key);
        if (File.Exists(path))
        {
            File.Delete(path);
            _logger.LogDebug("Cache invalidated: {Key}", key);
        }
    }

    private string GetCachePath(string key)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..16];
        return Path.Combine(_cacheDirectory, hash);
    }
}
