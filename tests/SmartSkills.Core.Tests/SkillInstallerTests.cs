using Microsoft.Extensions.Logging.Abstractions;
using SmartSkills.Core.Installation;
using SmartSkills.Core.Providers;
using SmartSkills.Core.Registry;
using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public sealed class SkillInstallerTests : IDisposable
{
    private readonly string _tempDir;

    public SkillInstallerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"skillinstaller-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static SkillInstaller CreateInstaller(
        FakeLibraryScanner? scanner = null,
        FakeSkillRegistry? registry = null,
        FakeSkillMatcher? matcher = null,
        FakeSkillLockFileStore? lockFileStore = null,
        FakeSkillMetadataParser? metadataParser = null,
        FakeSkillSourceProviderFactory? providerFactory = null)
    {
        return new SkillInstaller(
            scanner ?? new FakeLibraryScanner(),
            registry ?? new FakeSkillRegistry(),
            matcher ?? new FakeSkillMatcher(),
            lockFileStore ?? new FakeSkillLockFileStore(),
            metadataParser ?? new FakeSkillMetadataParser(),
            providerFactory ?? new FakeSkillSourceProviderFactory(),
            NullLogger<SkillInstaller>.Instance);
    }

    [Fact]
    public async Task InstallAsync_NullOptions_ThrowsArgumentNullException()
    {
        var installer = CreateInstaller();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => installer.InstallAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InstallAsync_NoMatchedSkills_ReturnsEmptyResult()
    {
        var scanner = new FakeLibraryScanner(
        [
            new ProjectPackages("proj.csproj",
            [
                new ResolvedPackage { Name = "SomePackage", Version = "1.0.0", IsTransitive = false }
            ])
        ]);
        var registry = new FakeSkillRegistry([
            new RegistryEntry
            {
                Type = "package",
                MatchCriteria = ["OtherPackage"],
                SkillPath = "skills/other",
                Language = "dotnet"
            }
        ]);
        var matcher = new FakeSkillMatcher([]); // no matches

        var installer = CreateInstaller(scanner: scanner, registry: registry, matcher: matcher);
        var result = await installer.InstallAsync(
            new InstallOptions { ProjectPath = _tempDir },
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Installed);
        Assert.Empty(result.Updated);
        Assert.Empty(result.SkippedUpToDate);
        Assert.Empty(result.Failed);
    }

    [Fact]
    public async Task InstallAsync_DryRun_DoesNotInstallFiles()
    {
        var entry = new RegistryEntry
        {
            Type = "package",
            MatchCriteria = ["TestPkg"],
            SkillPath = "skills/test-skill",
            RepoUrl = "https://github.com/org/repo",
            Language = "dotnet"
        };
        var scanner = new FakeLibraryScanner(
        [
            new ProjectPackages("proj.csproj",
            [
                new ResolvedPackage { Name = "TestPkg", Version = "1.0.0", IsTransitive = false }
            ])
        ]);
        var matcher = new FakeSkillMatcher(
        [
            new MatchedSkill { RegistryEntry = entry, MatchedPatterns = ["TestPkg"] }
        ]);
        var provider = new FakeSkillSourceProvider(latestSha: "abc123", files: ["SKILL.md"]);
        var providerFactory = new FakeSkillSourceProviderFactory(provider);

        var installer = CreateInstaller(
            scanner: scanner,
            matcher: matcher,
            providerFactory: providerFactory);

        var result = await installer.InstallAsync(
            new InstallOptions { ProjectPath = _tempDir, DryRun = true },
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Installed);
        Assert.Empty(result.Updated);

        var skillDir = Path.Combine(_tempDir, ".agents", "skills", "test-skill");
        Assert.False(Directory.Exists(skillDir));
    }

    [Fact]
    public async Task InstallAsync_MatchWithNoProvider_ReportsFailure()
    {
        var entry = new RegistryEntry
        {
            Type = "package",
            MatchCriteria = ["TestPkg"],
            SkillPath = "skills/orphan-skill",
            RepoUrl = null,
            SourceProvider = null,
            Language = "dotnet"
        };
        var scanner = new FakeLibraryScanner(
        [
            new ProjectPackages("proj.csproj",
            [
                new ResolvedPackage { Name = "TestPkg", Version = "1.0.0", IsTransitive = false }
            ])
        ]);
        var matcher = new FakeSkillMatcher(
        [
            new MatchedSkill { RegistryEntry = entry, MatchedPatterns = ["TestPkg"] }
        ]);

        var installer = CreateInstaller(scanner: scanner, matcher: matcher);
        var result = await installer.InstallAsync(
            new InstallOptions { ProjectPath = _tempDir },
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Installed);
        var failure = Assert.Single(result.Failed);
        Assert.Equal("skills/orphan-skill", failure.SkillPath);
        Assert.Contains("No source provider", failure.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UninstallAsync_RemovesDirectoryAndLockEntry()
    {
        var skillDir = Path.Combine(_tempDir, ".agents", "skills", "my-skill");
        Directory.CreateDirectory(skillDir);
        await File.WriteAllTextAsync(Path.Combine(skillDir, "SKILL.md"), "test", TestContext.Current.CancellationToken);

        var lockFileStore = new FakeSkillLockFileStore();
        var lockFile = new SkillsLockFile();
        lockFile.Skills["my-skill"] = new SkillLockEntry
        {
            RemoteUrl = "https://github.com/org/repo",
            SkillPath = "skills/my-skill",
            CommitSha = "abc123",
            LocalContentHash = "sha256:hash"
        };
        await lockFileStore.SaveAsync(_tempDir, lockFile, CancellationToken.None);

        var installer = CreateInstaller(lockFileStore: lockFileStore);
        await installer.UninstallAsync("my-skill", _tempDir, TestContext.Current.CancellationToken);

        Assert.False(Directory.Exists(skillDir));
        var loaded = await lockFileStore.LoadAsync(_tempDir, CancellationToken.None);
        Assert.DoesNotContain("my-skill", loaded.Skills.Keys);
    }

    [Fact]
    public async Task RestoreAsync_EmptyLockFile_ReturnsEmptyResult()
    {
        var installer = CreateInstaller();
        var result = await installer.RestoreAsync(_tempDir, TestContext.Current.CancellationToken);

        Assert.Empty(result.Restored);
        Assert.Empty(result.SkippedUpToDate);
        Assert.Empty(result.Failed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RestoreAsync_NullOrWhitespacePath_ThrowsArgumentException(string? path)
    {
        var installer = CreateInstaller();

        await Assert.ThrowsAsync<ArgumentException>(
            () => installer.RestoreAsync(path!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RestoreAsync_NullPath_ThrowsArgumentNullException()
    {
        var installer = CreateInstaller();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => installer.RestoreAsync(null!, TestContext.Current.CancellationToken));
    }

    // ── Fakes ──────────────────────────────────────────────────────────

    private sealed class FakeLibraryScanner(IReadOnlyList<ProjectPackages>? results = null) : ILibraryScanner
    {
        private readonly IReadOnlyList<ProjectPackages> _results = results ?? [];

        public Task<ProjectPackages> ScanProjectAsync(string projectPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(_results.Count > 0 ? _results[0] : new ProjectPackages(projectPath, []));

        public Task<IReadOnlyList<ProjectPackages>> ScanSolutionAsync(string solutionPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(_results);

        public Task<IReadOnlyList<ProjectPackages>> ScanDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(_results);

        public Task<IReadOnlyList<ProjectPackages>> ScanDirectoryAsync(string directoryPath, ProjectDetectionOptions options, CancellationToken cancellationToken = default) =>
            Task.FromResult(_results);
    }

    private sealed class FakeSkillRegistry(IReadOnlyList<RegistryEntry>? entries = null) : ISkillRegistry
    {
        public Task<IReadOnlyList<RegistryEntry>> GetRegistryEntriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RegistryEntry>>(entries ?? []);
    }

    private sealed class FakeSkillMatcher(IReadOnlyList<MatchedSkill>? matches = null) : ISkillMatcher
    {
        public IReadOnlyList<MatchedSkill> Match(
            IEnumerable<ResolvedPackage> packages,
            IEnumerable<RegistryEntry> registryEntries,
            IReadOnlyList<string>? rootFileNames = null) =>
            matches ?? [];
    }

    private sealed class FakeSkillLockFileStore : ISkillLockFileStore
    {
        private readonly Dictionary<string, SkillsLockFile> _store = new(StringComparer.OrdinalIgnoreCase);

        public Task<SkillsLockFile> LoadAsync(string projectRootPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.TryGetValue(projectRootPath, out var lf) ? lf : new SkillsLockFile());

        public Task SaveAsync(string projectRootPath, SkillsLockFile lockFile, CancellationToken cancellationToken = default)
        {
            _store[projectRootPath] = lockFile;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSkillMetadataParser : ISkillMetadataParser
    {
        public SkillMetadata? Parse(string skillMdContent, out IReadOnlyList<string> validationErrors)
        {
            validationErrors = [];
            return null;
        }
    }

    private sealed class FakeSkillSourceProviderFactory(FakeSkillSourceProvider? provider = null) : ISkillSourceProviderFactory
    {
        public ISkillSourceProvider CreateFromRepoUrl(string repoUrl) =>
            provider ?? new FakeSkillSourceProvider();
    }

    private sealed class FakeSkillSourceProvider(
        string latestSha = "fake-sha",
        IReadOnlyList<string>? files = null) : ISkillSourceProvider
    {
        public string ProviderType => "fake";

        public Task<IReadOnlyList<RegistryEntry>> GetRegistryIndexAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RegistryEntry>>([]);

        public Task<IReadOnlyList<string>> ListSkillFilesAsync(string skillPath, string? commitSha = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(files ?? ["SKILL.md"]);

        public Task<Stream> DownloadFileAsync(string filePath, string? commitSha = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream("# test"u8.ToArray()));

        public Task<string> GetLatestCommitShaAsync(string skillPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(latestSha);
    }
}
