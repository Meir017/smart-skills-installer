using SmartSkills.Core.Registry;

namespace SmartSkills.Core.Tests;

public class SkillRegistryParserTests
{
    private readonly SkillRegistryParser _parser = new();

    private const string SampleRegistry = """
        {
          "registryVersion": "1.0",
          "lastUpdated": "2026-01-01T00:00:00Z",
          "sourceType": "github",
          "entries": [
            {
              "libraryPattern": "Microsoft.EntityFrameworkCore",
              "skillManifestUrls": ["https://example.com/skills/ef-core/manifest.json"]
            },
            {
              "libraryPattern": "Microsoft.Extensions.*",
              "skillManifestUrls": [
                "https://example.com/skills/extensions-logging/manifest.json",
                "https://example.com/skills/extensions-di/manifest.json"
              ]
            },
            {
              "libraryPattern": "Newtonsoft.Json",
              "skillManifestUrls": ["https://example.com/skills/newtonsoft/manifest.json"]
            },
            {
              "libraryPattern": "Microsoft.Extensions.Logging",
              "skillManifestUrls": ["https://example.com/skills/extensions-logging/manifest.json"]
            }
          ]
        }
        """;

    [Fact]
    public void Parse_ValidRegistry_ReturnsMetadata()
    {
        var registry = _parser.Parse(SampleRegistry);

        Assert.Equal("1.0", registry.RegistryVersion);
        Assert.Equal("github", registry.SourceType);
        Assert.Equal(4, registry.Entries.Count);
    }

    [Fact]
    public void FindManifestUrls_ExactMatch_ReturnsCorrectUrls()
    {
        var registry = _parser.Parse(SampleRegistry);
        var urls = _parser.FindManifestUrls(registry, "Newtonsoft.Json");

        Assert.Single(urls);
        Assert.Contains("newtonsoft/manifest.json", urls[0]);
    }

    [Fact]
    public void FindManifestUrls_GlobPattern_MatchesCorrectly()
    {
        var registry = _parser.Parse(SampleRegistry);
        var urls = _parser.FindManifestUrls(registry, "Microsoft.Extensions.Logging");

        // Should match both the glob "Microsoft.Extensions.*" and the exact "Microsoft.Extensions.Logging"
        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, u => u.Contains("extensions-logging"));
        Assert.Contains(urls, u => u.Contains("extensions-di"));
    }

    [Fact]
    public void FindManifestUrls_MultipleSkillsForOneLibrary_ReturnsAll()
    {
        var registry = _parser.Parse(SampleRegistry);
        var urls = _parser.FindManifestUrls(registry, "Microsoft.Extensions.DependencyInjection");

        // Matches glob "Microsoft.Extensions.*" which has 2 manifest URLs
        Assert.Equal(2, urls.Count);
    }

    [Fact]
    public void FindAllManifestUrls_DeduplicatesAcrossLibraries()
    {
        var registry = _parser.Parse(SampleRegistry);
        var urls = _parser.FindAllManifestUrls(registry, [
            "Microsoft.Extensions.Logging",
            "Microsoft.Extensions.DependencyInjection"
        ]);

        // Both match the glob which has 2 URLs, plus exact match for Logging also has the same logging URL
        // Should be deduplicated
        Assert.Equal(2, urls.Count);
    }

    [Fact]
    public void FindManifestUrls_NoMatch_ReturnsEmpty()
    {
        var registry = _parser.Parse(SampleRegistry);
        var urls = _parser.FindManifestUrls(registry, "SomeUnknownPackage");

        Assert.Empty(urls);
    }
}
