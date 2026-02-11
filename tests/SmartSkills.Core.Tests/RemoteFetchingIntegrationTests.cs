using System.Net;
using System.Text;
using SmartSkills.Core.Fetching;
using SmartSkills.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SmartSkills.Core.Tests;

public class RemoteFetchingIntegrationTests
{
    private readonly ILogger<RegistryFetchService> _regLogger = NullLogger<RegistryFetchService>.Instance;

    /// <summary>
    /// Mock content fetcher that simulates remote responses.
    /// </summary>
    private class MockContentFetcher : IRemoteContentFetcher
    {
        private readonly Dictionary<string, string> _responses = new();
        private readonly HashSet<string> _failUrls = new();
        public int FetchCount { get; private set; }

        public void AddResponse(string url, string content) => _responses[url] = content;
        public void AddFailure(string url) => _failUrls.Add(url);

        public Task<string> FetchStringAsync(string url, CancellationToken cancellationToken = default)
        {
            FetchCount++;
            if (_failUrls.Contains(url))
                throw new HttpRequestException($"Simulated failure for {url}");
            if (_responses.TryGetValue(url, out var content))
                return Task.FromResult(content);
            throw new HttpRequestException($"Not found: {url}", null, HttpStatusCode.NotFound);
        }

        public Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken = default)
        {
            FetchCount++;
            if (_failUrls.Contains(url))
                throw new HttpRequestException($"Simulated failure for {url}");
            if (_responses.TryGetValue(url, out var content))
            {
                var dir = Path.GetDirectoryName(destinationPath);
                if (dir is not null) Directory.CreateDirectory(dir);
                File.WriteAllText(destinationPath, content);
                return Task.CompletedTask;
            }
            throw new HttpRequestException($"Not found: {url}", null, HttpStatusCode.NotFound);
        }
    }

    private static readonly string ValidRegistryJson = """
        {
          "registryVersion": "1.0",
          "lastUpdated": "2024-01-01",
          "sourceType": "github",
          "entries": [
            {
              "libraryPattern": "Microsoft.EntityFrameworkCore*",
              "skillManifestUrls": ["https://raw.githubusercontent.com/org/repo/main/skills/ef-core/manifest.json"]
            },
            {
              "libraryPattern": "Serilog*",
              "skillManifestUrls": ["https://raw.githubusercontent.com/org/repo/main/skills/serilog/manifest.json"]
            }
          ]
        }
        """;

    [Fact]
    public async Task FetchRegistry_FromGitHubSource_ReturnsValidRegistry()
    {
        var fetcher = new MockContentFetcher();
        var expectedUrl = GitHubContentFetcher.BuildRawUrl("org", "repo", "main", "skills-registry.json");
        fetcher.AddResponse(expectedUrl, ValidRegistryJson);

        var tempCache = Path.Combine(Path.GetTempPath(), "smartskills-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = new RegistryFetchService(fetcher, _regLogger, tempCache);
            var registry = await service.FetchRegistryAsync("github:org/repo", CancellationToken.None);

            Assert.NotNull(registry);
            Assert.Equal("1.0", registry.RegistryVersion);
            Assert.Equal(2, registry.Entries.Count);
            Assert.Equal("Microsoft.EntityFrameworkCore*", registry.Entries[0].LibraryPattern);
        }
        finally
        {
            if (Directory.Exists(tempCache)) Directory.Delete(tempCache, true);
        }
    }

    [Fact]
    public async Task FetchRegistry_CachesResults_SecondCallUsesCache()
    {
        var fetcher = new MockContentFetcher();
        var expectedUrl = GitHubContentFetcher.BuildRawUrl("org", "repo", "main", "skills-registry.json");
        fetcher.AddResponse(expectedUrl, ValidRegistryJson);

        var tempCache = Path.Combine(Path.GetTempPath(), "smartskills-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = new RegistryFetchService(fetcher, _regLogger, tempCache);

            var r1 = await service.FetchRegistryAsync("github:org/repo", CancellationToken.None);
            Assert.Equal(1, fetcher.FetchCount);

            var r2 = await service.FetchRegistryAsync("github:org/repo", CancellationToken.None);
            // Should still be 1 - second call uses cache
            Assert.Equal(1, fetcher.FetchCount);
            Assert.Equal(r1.Entries.Count, r2.Entries.Count);
        }
        finally
        {
            if (Directory.Exists(tempCache)) Directory.Delete(tempCache, true);
        }
    }

    [Fact]
    public async Task FetchRegistry_NetworkFailure_ThrowsWhenNoCacheAvailable()
    {
        var fetcher = new MockContentFetcher();
        var expectedUrl = GitHubContentFetcher.BuildRawUrl("org", "repo", "main", "skills-registry.json");
        fetcher.AddFailure(expectedUrl);

        var tempCache = Path.Combine(Path.GetTempPath(), "smartskills-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = new RegistryFetchService(fetcher, _regLogger, tempCache);

            await Assert.ThrowsAsync<HttpRequestException>(
                () => service.FetchRegistryAsync("github:org/repo", CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(tempCache)) Directory.Delete(tempCache, true);
        }
    }

    [Fact]
    public async Task FetchRegistry_NetworkFailure_FallsBackToStaleCache()
    {
        var fetcher = new MockContentFetcher();
        var expectedUrl = GitHubContentFetcher.BuildRawUrl("org", "repo", "main", "skills-registry.json");
        fetcher.AddResponse(expectedUrl, ValidRegistryJson);

        var tempCache = Path.Combine(Path.GetTempPath(), "smartskills-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            // First fetch populates cache
            var service = new RegistryFetchService(fetcher, _regLogger, tempCache, TimeSpan.Zero);
            var r1 = await service.FetchRegistryAsync("github:org/repo", CancellationToken.None);
            Assert.NotNull(r1);

            // Now fail the fetcher
            fetcher.AddFailure(expectedUrl);

            // Should fall back to stale cache
            var r2 = await service.FetchRegistryAsync("github:org/repo", CancellationToken.None);
            Assert.NotNull(r2);
            Assert.Equal(r1.Entries.Count, r2.Entries.Count);
        }
        finally
        {
            if (Directory.Exists(tempCache)) Directory.Delete(tempCache, true);
        }
    }

    [Fact]
    public async Task FetchRegistry_WithBranch_UsesCorrectUrl()
    {
        var fetcher = new MockContentFetcher();
        var expectedUrl = GitHubContentFetcher.BuildRawUrl("org", "repo", "release/v2", "skills-registry.json");
        fetcher.AddResponse(expectedUrl, ValidRegistryJson);

        var tempCache = Path.Combine(Path.GetTempPath(), "smartskills-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = new RegistryFetchService(fetcher, _regLogger, tempCache);
            var registry = await service.FetchRegistryAsync("github:org/repo@release/v2", CancellationToken.None);
            Assert.NotNull(registry);
            Assert.Equal(1, fetcher.FetchCount);
        }
        finally
        {
            if (Directory.Exists(tempCache)) Directory.Delete(tempCache, true);
        }
    }

    [Fact]
    public async Task DownloadFile_SavesContentToLocalPath()
    {
        var fetcher = new MockContentFetcher();
        fetcher.AddResponse("https://example.com/skill.json", """{"id":"test","name":"Test"}""");

        var tempDir = Path.Combine(Path.GetTempPath(), "smartskills-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var dest = Path.Combine(tempDir, "skill.json");
            await fetcher.DownloadFileAsync("https://example.com/skill.json", dest, TestContext.Current.CancellationToken);

            Assert.True(File.Exists(dest));
            var content = File.ReadAllText(dest);
            Assert.Contains("test", content);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Theory]
    [InlineData("github:org/repo", "org", "repo", "main")]
    [InlineData("github:my-org/my-repo@develop", "my-org", "my-repo", "develop")]
    [InlineData("org/repo", "org", "repo", "main")]
    public void ParseGitHubSource_ValidFormats(string source, string expectedOwner, string expectedRepo, string expectedBranch)
    {
        var (owner, repo, branch) = RegistryFetchService.ParseGitHubSource(source);
        Assert.Equal(expectedOwner, owner);
        Assert.Equal(expectedRepo, repo);
        Assert.Equal(expectedBranch, branch);
    }

    [Theory]
    [InlineData("github:")]
    [InlineData("github:invalid")]
    [InlineData("")]
    public void ParseGitHubSource_InvalidFormats_Throws(string source)
    {
        Assert.Throws<ArgumentException>(() => RegistryFetchService.ParseGitHubSource(source));
    }
}
