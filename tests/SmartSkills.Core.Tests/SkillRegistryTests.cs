using SmartSkills.Core.Providers;
using SmartSkills.Core.Registry;
using Xunit;

namespace SmartSkills.Core.Tests;

public class SkillRegistryTests
{
    [Fact]
    public async Task GetRegistryEntriesAsync_NoProviders_ReturnsOnlyEmbeddedEntries()
    {
        var registry = new SkillRegistry([]);

        var entries = await registry.GetRegistryEntriesAsync(TestContext.Current.CancellationToken);

        Assert.NotEmpty(entries);
        Assert.All(entries, e => Assert.Null(e.SourceProvider));
    }

    [Fact]
    public async Task GetRegistryEntriesAsync_WithProvider_MergesProviderEntries()
    {
        var providerEntry = CreateEntry("custom-skill");
        var provider = new FakeSkillSourceProvider([providerEntry]);
        var registry = new SkillRegistry([provider]);

        var entries = await registry.GetRegistryEntriesAsync(TestContext.Current.CancellationToken);

        var embeddedCount = RegistryIndexParser.LoadEmbedded().Count;
        Assert.Equal(embeddedCount + 1, entries.Count);
        Assert.Contains(entries, e => e.SkillPath == "custom-skill");
    }

    [Fact]
    public async Task GetRegistryEntriesAsync_ProviderEntries_HaveSourceProviderStamped()
    {
        var providerEntry = CreateEntry("provider-skill");
        var provider = new FakeSkillSourceProvider([providerEntry]);
        var registry = new SkillRegistry([provider]);

        var entries = await registry.GetRegistryEntriesAsync(TestContext.Current.CancellationToken);

        var fromProvider = entries.Where(e => e.SkillPath == "provider-skill").ToList();
        Assert.Single(fromProvider);
        Assert.Same(provider, fromProvider[0].SourceProvider);
    }

    [Fact]
    public async Task GetRegistryEntriesAsync_EmbeddedEntries_HaveNullSourceProvider()
    {
        var provider = new FakeSkillSourceProvider([CreateEntry("extra")]);
        var registry = new SkillRegistry([provider]);

        var entries = await registry.GetRegistryEntriesAsync(TestContext.Current.CancellationToken);

        var embedded = entries.Where(e => e.SkillPath != "extra").ToList();
        Assert.NotEmpty(embedded);
        Assert.All(embedded, e => Assert.Null(e.SourceProvider));
    }

    [Fact]
    public async Task GetRegistryEntriesAsync_MultipleProviders_MergesAll()
    {
        var entry1 = CreateEntry("skill-a");
        var entry2 = CreateEntry("skill-b");
        var provider1 = new FakeSkillSourceProvider([entry1]);
        var provider2 = new FakeSkillSourceProvider([entry2]);
        var registry = new SkillRegistry([provider1, provider2]);

        var entries = await registry.GetRegistryEntriesAsync(TestContext.Current.CancellationToken);

        var embeddedCount = RegistryIndexParser.LoadEmbedded().Count;
        Assert.Equal(embeddedCount + 2, entries.Count);
        Assert.Contains(entries, e => e.SkillPath == "skill-a");
        Assert.Contains(entries, e => e.SkillPath == "skill-b");
    }

    private static RegistryEntry CreateEntry(string skillPath) => new()
    {
        Type = "package",
        MatchCriteria = ["test-package"],
        SkillPath = skillPath,
        Language = "dotnet",
    };

    private sealed class FakeSkillSourceProvider : ISkillSourceProvider
    {
        private readonly IReadOnlyList<RegistryEntry> _entries;

        public FakeSkillSourceProvider(IReadOnlyList<RegistryEntry> entries)
        {
            _entries = entries;
        }

        public string ProviderType => "fake";

        public Task<IReadOnlyList<RegistryEntry>> GetRegistryIndexAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_entries);

        public Task<IReadOnlyList<string>> ListSkillFilesAsync(string skillPath, string? commitSha = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<Stream> DownloadFileAsync(string filePath, string? commitSha = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<string> GetLatestCommitShaAsync(string skillPath, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
