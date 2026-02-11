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
    private readonly ISkillMetadataParser _metadataParser;
    private readonly ISkillSourceProviderFactory _providerFactory;
    private readonly ILogger<SkillInstaller> _logger;

    public SkillInstaller(
        ILibraryScanner scanner,
        ISkillRegistry registry,
        ISkillMatcher matcher,
        ISkillStore store,
        ISkillMetadataParser metadataParser,
        ISkillSourceProviderFactory providerFactory,
        ILogger<SkillInstaller> logger)
    {
        _scanner = scanner;
        _registry = registry;
        _matcher = matcher;
        _store = store;
        _metadataParser = metadataParser;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<InstallResult> InstallAsync(InstallOptions options, CancellationToken cancellationToken = default)
    {
        var projectPath = options.ProjectPath ?? Directory.GetCurrentDirectory();
        _logger.LogInformation("Starting skill installation for {Path}", projectPath);

        // 1. Resolve packages
        IReadOnlyList<ProjectPackages> projectPackages;
        if (projectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
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

        // 3. For each matched skill, check cache, fetch, validate, install
        foreach (var match in matched)
        {
            try
            {
                // Use the provider attached to the registry entry, or create one from the RepoUrl
                var provider = match.RegistryEntry.SourceProvider
                    ?? (match.RegistryEntry.RepoUrl is not null
                        ? _providerFactory.CreateFromRepoUrl(match.RegistryEntry.RepoUrl)
                        : null);

                if (provider is null)
                {
                    failed.Add(new SkillInstallFailure(match.RegistryEntry.SkillPath, "No source provider or RepoUrl configured for this skill"));
                    continue;
                }

                // Check cache
                var latestSha = await provider.GetLatestCommitShaAsync(match.RegistryEntry.SkillPath, cancellationToken);
                var existingSkill = await _store.GetByNameAsync(Path.GetFileName(match.RegistryEntry.SkillPath), cancellationToken);

                if (existingSkill is not null && existingSkill.CommitSha == latestSha)
                {
                    _logger.LogInformation("Skill {Skill} is up-to-date (SHA: {Sha})", match.RegistryEntry.SkillPath, latestSha);
                    skipped.Add(match.RegistryEntry.SkillPath);
                    continue;
                }

                if (options.DryRun)
                {
                    _logger.LogInformation("[DRY-RUN] Would install/update skill: {Skill}", match.RegistryEntry.SkillPath);
                    continue;
                }

                // Fetch and install
                var files = await provider.ListSkillFilesAsync(match.RegistryEntry.SkillPath, cancellationToken);
                var skillName = Path.GetFileName(match.RegistryEntry.SkillPath);
                var installDir = Path.Combine(Path.GetDirectoryName(projectPath)!, ".agents", "skills", skillName);

                Directory.CreateDirectory(installDir);

                SkillMetadata? metadata = null;

                foreach (var file in files)
                {
                    var remotePath = $"{match.RegistryEntry.SkillPath}/{file}";
                    using var stream = await provider.DownloadFileAsync(remotePath, cancellationToken);
                    var localPath = Path.Combine(installDir, file.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                    using var fs = File.Create(localPath);
                    await stream.CopyToAsync(fs, cancellationToken);

                    // Parse SKILL.md
                    if (string.Equals(file, "SKILL.md", StringComparison.OrdinalIgnoreCase))
                    {
                        fs.Position = 0;
                        using var reader = new StreamReader(fs);
                        var content = await reader.ReadToEndAsync(cancellationToken);
                        metadata = _metadataParser.Parse(content, out var errors);
                        if (metadata is null)
                        {
                            _logger.LogWarning("SKILL.md validation failed: {Errors}", string.Join("; ", errors));
                        }
                    }
                }

                metadata ??= new SkillMetadata { Name = skillName, Description = "Unknown" };

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

                if (existingSkill is not null)
                    updated.Add(skill);
                else
                    installed.Add(skill);

                _logger.LogInformation("Installed skill: {Skill}", skill.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to install skill: {Skill}", match.RegistryEntry.SkillPath);
                failed.Add(new SkillInstallFailure(match.RegistryEntry.SkillPath, ex.Message));
            }
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
}
