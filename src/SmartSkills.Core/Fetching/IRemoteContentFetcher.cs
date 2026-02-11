namespace SmartSkills.Core.Fetching;

/// <summary>
/// Abstraction for fetching content from remote sources (GitHub, ADO, etc.).
/// </summary>
public interface IRemoteContentFetcher
{
    /// <summary>
    /// Fetches the content of a file at the given URL as a string.
    /// </summary>
    Task<string> FetchStringAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file to the specified local path.
    /// </summary>
    Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken = default);
}
