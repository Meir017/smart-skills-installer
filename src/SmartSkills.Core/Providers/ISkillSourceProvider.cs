using SmartSkills.Core.Registry;

namespace SmartSkills.Core.Providers;

/// <summary>
/// Abstracts all interaction with a remote skill repository.
/// </summary>
public interface ISkillSourceProvider
{
    /// <summary>Identifies this provider type for configuration and state tracking.</summary>
    string ProviderType { get; }

    /// <summary>
    /// Fetch and return the parsed registry index from this source.
    /// </summary>
    Task<IReadOnlyList<RegistryEntry>> GetRegistryIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerate all files in a skill directory tree.
    /// </summary>
    Task<IReadOnlyList<string>> ListSkillFilesAsync(string skillPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download the raw content of a single file.
    /// </summary>
    Task<Stream> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Return the SHA of the most recent commit that touched the given skill directory.
    /// </summary>
    Task<string> GetLatestCommitShaAsync(string skillPath, CancellationToken cancellationToken = default);
}
