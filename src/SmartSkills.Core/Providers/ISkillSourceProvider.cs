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
    /// <param name="skillPath">Relative path to the skill directory.</param>
    /// <param name="commitSha">Optional commit SHA to fetch at. If null, uses the default branch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<string>> ListSkillFilesAsync(string skillPath, string? commitSha = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download the raw content of a single file.
    /// </summary>
    /// <param name="filePath">Path to the file within the repository.</param>
    /// <param name="commitSha">Optional commit SHA to fetch at. If null, uses the default branch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Stream> DownloadFileAsync(string filePath, string? commitSha = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Return the SHA of the most recent commit that touched the given skill directory.
    /// </summary>
    Task<string> GetLatestCommitShaAsync(string skillPath, CancellationToken cancellationToken = default);
}
