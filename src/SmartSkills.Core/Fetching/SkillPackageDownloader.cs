using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SmartSkills.Core.Models;

namespace SmartSkills.Core.Fetching;

public class SkillPackageDownloader
{
    private readonly IRemoteContentFetcher _fetcher;
    private readonly ILogger<SkillPackageDownloader> _logger;

    public SkillPackageDownloader(IRemoteContentFetcher fetcher, ILogger<SkillPackageDownloader> logger)
    {
        _fetcher = fetcher;
        _logger = logger;
    }

    /// <summary>
    /// Downloads all files specified in the skill manifest's install steps to the destination directory.
    /// </summary>
    public async Task DownloadSkillAsync(SkillManifest manifest, string destinationDir, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var step in manifest.InstallSteps)
        {
            var destPath = Path.Combine(destinationDir, step.Destination);
            _logger.LogInformation("Downloading: {Source} -> {Destination}", step.Source, step.Destination);

            await _fetcher.DownloadFileAsync(step.Source, destPath, cancellationToken);

            if (!string.IsNullOrWhiteSpace(step.Sha256))
            {
                VerifyChecksum(destPath, step.Sha256);
            }
        }

        _logger.LogInformation("Skill '{SkillId}' downloaded successfully ({Count} file(s))",
            manifest.SkillId, manifest.InstallSteps.Count);
    }

    internal static void VerifyChecksum(string filePath, string expectedSha256)
    {
        byte[] hash;
        using (var stream = File.OpenRead(filePath))
        {
            hash = SHA256.HashData(stream);
        }

        var actual = Convert.ToHexString(hash);

        if (!actual.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(filePath);
            throw new InvalidOperationException(
                $"Checksum mismatch for '{filePath}'. Expected: {expectedSha256}, Actual: {actual}. File has been removed.");
        }
    }
}
