using Microsoft.Extensions.Logging;
using SmartSkills.Core.Providers;
using SmartSkills.Core.Providers.GitHub;
using SmartSkills.Core.Registry;
using SmartSkills.Core.Scanning;

namespace SmartSkills.Core.Installation;

/// <summary>
/// Orchestrates the full skill installation pipeline.
/// </summary>
public sealed class SkillInstaller : ISkillInstaller
{
    private readonly ILibraryScanner _scanner;
    private readonly ISkillRegistry _registry;
    private readonly ISkillMatcher _matcher;
    private readonly ISkillStore _store;
    private readonly ISkillLockFileStore _lockFileStore;
    private readonly ISkillMetadataParser _metadataParser;
    private readonly ISkillSourceProviderFactory _providerFactory;
    private readonly ILogger<SkillInstaller> _logger;

    public SkillInstaller(
        ILibraryScanner scanner,
        ISkillRegistry registry,
        ISkillMatcher matcher,
        ISkillStore store,
        ISkillLockFileStore lockFileStore,
        ISkillMetadataParser metadataParser,
        ISkillSourceProviderFactory providerFactory,
        ILogger<SkillInstaller> logger)
    {
        _scanner = scanner;
        _registry = registry;
        _matcher = matcher;
        _store = store;
        _lockFileStore = lockFileStore;
        _metadataParser = metadataParser;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<InstallResult> InstallAsync(InstallOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var projectPath = options.ProjectPath ?? Directory.GetCurrentDirectory();
        _logger.LogInformation("Starting skill installation for {Path}", projectPath);

        var baseDir = Directory.Exists(projectPath) ? projectPath : Path.GetDirectoryName(projectPath)!;

        // Load lock file
        var lockFile = await _lockFileStore.LoadAsync(baseDir, cancellationToken);

        // 1. Resolve packages
        IReadOnlyList<ProjectPackages> projectPackages;
        if (Directory.Exists(projectPath))
        {
            projectPackages = await _scanner.ScanDirectoryAsync(projectPath, cancellationToken);
        }
        else if (projectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                 projectPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
        {
            projectPackages = await _scanner.ScanSolutionAsync(projectPath, cancellationToken);
        }
        else
        {
            var single = await _scanner.ScanProjectAsync(projectPath, cancellationToken);
            projectPackages = [single];
        }

        var allPackages = projectPackages.SelectMany(p => p.Packages).ToList();

        // 2. Match skills
        var registryEntries = await _registry.GetRegistryEntriesAsync(cancellationToken);
        var matched = _matcher.Match(allPackages, registryEntries);

        _logger.LogInformation("Found {Count} matching skills", matched.Count);

        var installed = new List<InstalledSkill>();
        var updated = new List<InstalledSkill>();
        var skipped = new List<string>();
        var failed = new List<SkillInstallFailure>();
        var lockFileChanged = false;

        // 3. For each matched skill, check lock file, fetch, validate, install
        foreach (var match in matched)
        {
            try
            {
                var provider = match.RegistryEntry.SourceProvider
                    ?? (match.RegistryEntry.RepoUrl is not null
                        ? _providerFactory.CreateFromRepoUrl(match.RegistryEntry.RepoUrl)
                        : null);

                if (provider is null)
                {
                    failed.Add(new SkillInstallFailure(match.RegistryEntry.SkillPath, "No source provider or RepoUrl configured for this skill"));
                    continue;
                }

                var skillName = Path.GetFileName(match.RegistryEntry.SkillPath);
                var installDir = Path.Combine(baseDir, ".agents", "skills", skillName);
                var repoUrl = match.RegistryEntry.RepoUrl ?? "";

                // Check lock file for existing entry
                var latestSha = await provider.GetLatestCommitShaAsync(match.RegistryEntry.SkillPath, cancellationToken);

                if (lockFile.Skills.TryGetValue(skillName, out var lockEntry) &&
                    lockEntry.CommitSha == latestSha)
                {
                    // Remote hasn't changed — check for local edits
                    if (Directory.Exists(installDir))
                    {
                        var currentHash = SkillContentHasher.ComputeHash(installDir);
                        if (currentHash == lockEntry.LocalContentHash)
                        {
                            _logger.LogInformation("Skill {Skill} is up-to-date (SHA: {Sha})", skillName, latestSha);
                            skipped.Add(match.RegistryEntry.SkillPath);
                            continue;
                        }

                        // Local files modified
                        if (!options.Force)
                        {
                            _logger.LogWarning("Skill {Skill} has been locally modified. Use --force to overwrite.", skillName);
                            skipped.Add(match.RegistryEntry.SkillPath);
                            continue;
                        }

                        _logger.LogInformation("Skill {Skill} has local modifications, overwriting (--force)", skillName);
                    }
                }

                if (options.DryRun)
                {
                    _logger.LogInformation("[DRY-RUN] Would install/update skill: {Skill}", match.RegistryEntry.SkillPath);
                    continue;
                }

                // Fetch and install
                var isUpdate = lockFile.Skills.ContainsKey(skillName);
                await DownloadSkillAsync(provider, match.RegistryEntry.SkillPath, installDir, cancellationToken);

                // Compute content hash of downloaded files
                var contentHash = SkillContentHasher.ComputeHash(installDir);

                // Update lock file entry
                lockFile.Skills[skillName] = new SkillLockEntry
                {
                    RemoteUrl = repoUrl,
                    SkillPath = match.RegistryEntry.SkillPath,
                    Language = match.RegistryEntry.Language,
                    CommitSha = latestSha,
                    LocalContentHash = contentHash
                };
                lockFileChanged = true;

                // Also update legacy store for backward compatibility
                var metadata = await ParseSkillMetadataAsync(installDir, skillName, cancellationToken);
                var skill = new InstalledSkill
                {
                    Name = metadata.Name,
                    Metadata = metadata,
                    InstallPath = installDir,
                    InstalledAt = DateTimeOffset.UtcNow,
                    SourceProviderType = provider.ProviderType,
                    SourceUrl = match.RegistryEntry.SkillPath,
                    CommitSha = latestSha
                };

                await _store.SaveAsync(skill, cancellationToken);

                if (isUpdate)
                    updated.Add(skill);
                else
                    installed.Add(skill);

                _logger.LogInformation("Installed skill: {Skill}", skill.Name);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.LogError(ex, "Failed to install skill: {Skill}", match.RegistryEntry.SkillPath);
                failed.Add(new SkillInstallFailure(match.RegistryEntry.SkillPath, ex.Message));
            }
        }

        // Write lock file if anything changed
        if (lockFileChanged)
        {
            await _lockFileStore.SaveAsync(baseDir, lockFile, cancellationToken);
        }

        return new InstallResult
        {
            Installed = installed,
            Updated = updated,
            SkippedUpToDate = skipped,
            Failed = failed
        };
    }

    public async Task UninstallAsync(string skillName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uninstalling skill: {SkillName}", skillName);
        await _store.RemoveAsync(skillName, cancellationToken);
    }

    public async Task<RestoreResult> RestoreAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        var baseDir = Directory.Exists(projectPath) ? projectPath : Path.GetDirectoryName(projectPath)!;
        var lockFile = await _lockFileStore.LoadAsync(baseDir, cancellationToken);

        if (lockFile.Skills.Count == 0)
        {
            _logger.LogInformation("Lock file is empty or not found. Nothing to restore.");
            return new RestoreResult { Restored = [], SkippedUpToDate = [], Failed = [] };
        }

        var restored = new List<string>();
        var skipped = new List<string>();
        var failed = new List<SkillInstallFailure>();

        foreach (var (skillName, entry) in lockFile.Skills)
        {
            try
            {
                var installDir = Path.Combine(baseDir, ".agents", "skills", skillName);

                // Check if already up-to-date
                if (Directory.Exists(installDir))
                {
                    var currentHash = SkillContentHasher.ComputeHash(installDir);
                    if (currentHash == entry.LocalContentHash)
                    {
                        _logger.LogInformation("Skill {Skill} is up-to-date", skillName);
                        skipped.Add(skillName);
                        continue;
                    }
                }

                // Download at the specific commit SHA
                var provider = _providerFactory.CreateFromRepoUrl(entry.RemoteUrl);
                await DownloadSkillAsync(provider, entry.SkillPath, installDir, cancellationToken, entry.CommitSha);

                // Verify content hash
                var downloadedHash = SkillContentHasher.ComputeHash(installDir);
                if (downloadedHash != entry.LocalContentHash)
                {
                    _logger.LogWarning("Content hash mismatch for {Skill} after restore (expected {Expected}, got {Actual})",
                        skillName, entry.LocalContentHash, downloadedHash);
                }

                restored.Add(skillName);
                _logger.LogInformation("Restored skill: {Skill}", skillName);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.LogError(ex, "Failed to restore skill: {Skill}", skillName);
                failed.Add(new SkillInstallFailure(skillName, ex.Message));
            }
        }

        return new RestoreResult { Restored = restored, SkippedUpToDate = skipped, Failed = failed };
    }

    private static async Task DownloadSkillAsync(ISkillSourceProvider provider, string skillPath, string installDir, CancellationToken cancellationToken, string? commitSha = null)
    {
        var files = await provider.ListSkillFilesAsync(skillPath, commitSha, cancellationToken);
        Directory.CreateDirectory(installDir);

        foreach (var file in files)
        {
            var remotePath = $"{skillPath}/{file}";
            using var stream = await provider.DownloadFileAsync(remotePath, commitSha, cancellationToken);
            var localPath = Path.Combine(installDir, file.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            using var fs = File.Create(localPath);
            await stream.CopyToAsync(fs, cancellationToken);
        }
    }

    private async Task<SkillMetadata> ParseSkillMetadataAsync(string installDir, string skillName, CancellationToken cancellationToken)
    {
        var skillMdPath = Path.Combine(installDir, "SKILL.md");
        if (File.Exists(skillMdPath))
        {
            var content = await File.ReadAllTextAsync(skillMdPath, cancellationToken);
            var metadata = _metadataParser.Parse(content, out var errors);
            if (metadata is not null)
                return metadata;

            _logger.LogWarning("SKILL.md validation failed: {Errors}", string.Join("; ", errors));
        }

        return new SkillMetadata { Name = skillName, Description = "Unknown" };
    }
}
