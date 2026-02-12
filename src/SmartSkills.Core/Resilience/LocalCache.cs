using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmartSkills.Core.Resilience;

/// <summary>
/// Configuration options for <see cref="LocalCache"/>.
/// </summary>
public sealed class LocalCacheOptions
{
    /// <summary>
    /// The directory where cache files are stored.
    /// Defaults to {LocalApplicationData}/SmartSkills/cache.
    /// </summary>
    public string CacheDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartSkills", "cache");
}

/// <summary>
/// File-system-based cache for registry indexes and skill manifests.
/// </summary>
public sealed class LocalCache
{
    private readonly string _cacheDirectory;
    private readonly ILogger<LocalCache> _logger;

    public LocalCache(IOptions<LocalCacheOptions> options, ILogger<LocalCache> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _cacheDirectory = options.Value.CacheDirectory;
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
