using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartSkills.Core.Resilience;
using Xunit;

namespace SmartSkills.Core.Tests;

public sealed class LocalCacheTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LocalCache _cache;

    public LocalCacheTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SmartSkillsTests", Guid.NewGuid().ToString("N"));
        var options = Options.Create(new LocalCacheOptions { CacheDirectory = _tempDir });
        _cache = new LocalCache(options, NullLogger<LocalCache>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetAsync_CacheMiss_ReturnsNull()
    {
        // Arrange
        var key = "nonexistent-key";

        // Act
        var result = await _cache.GetAsync(key, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsStoredValue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        var ct = TestContext.Current.CancellationToken;

        // Act
        await _cache.SetAsync(key, value, ct);
        var result = await _cache.GetAsync(key, ct);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task Invalidate_ExistingKey_RemovesEntry()
    {
        // Arrange
        var key = "key-to-invalidate";
        var ct = TestContext.Current.CancellationToken;
        await _cache.SetAsync(key, "some-value", ct);

        // Act
        _cache.Invalidate(key);
        var result = await _cache.GetAsync(key, ct);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Invalidate_NonExistentKey_DoesNotThrow()
    {
        // Arrange
        var key = "missing-key";

        // Act & Assert
        _cache.Invalidate(key);
    }

    [Fact]
    public async Task GetCachePath_SameKey_ProducesSamePath()
    {
        // Arrange
        var key = "deterministic-key";
        var value = "hello";

        // Act
        var ct = TestContext.Current.CancellationToken;
        await _cache.SetAsync(key, value, ct);
        var first = await _cache.GetAsync(key, ct);
        var second = await _cache.GetAsync(key, ct);

        // Assert
        Assert.Equal(value, first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Constructor_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var dir = Path.Combine(Path.GetTempPath(), "SmartSkillsTests", Guid.NewGuid().ToString("N"));

        try
        {
            // Act
            var options = Options.Create(new LocalCacheOptions { CacheDirectory = dir });
            _ = new LocalCache(options, NullLogger<LocalCache>.Instance);

            // Assert
            Assert.True(Directory.Exists(dir));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}
