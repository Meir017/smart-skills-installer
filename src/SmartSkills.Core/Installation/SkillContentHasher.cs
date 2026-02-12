using System.Security.Cryptography;
using System.Text;

namespace SmartSkills.Core.Installation;

/// <summary>
/// Computes a deterministic, cross-platform SHA256 content hash over a skill directory.
/// </summary>
public static class SkillContentHasher
{
    /// <summary>
    /// Compute a content hash over all files in the given directory.
    /// Returns a string in the format "sha256:&lt;lowercase-hex&gt;".
    /// </summary>
    public static string ComputeHash(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        // 1. Enumerate all files recursively, excluding hidden files/directories
        var files = EnumerateNonHiddenFiles(directoryPath);

        // 2. Compute relative paths, normalize to forward slashes and NFC Unicode
        var entries = files
            .Select(f => NormalizeRelativePath(directoryPath, f))
            .OrderBy(p => p, StringComparer.Ordinal) // 3. Sort ordinally
            .ToList();

        // 4-5. For each file: hash raw bytes, build concatenated manifest
        var sb = new StringBuilder();
        foreach (var relativePath in entries)
        {
            var fullPath = Path.Combine(directoryPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var fileBytes = File.ReadAllBytes(fullPath);
            var fileHash = ComputeSha256Hex(fileBytes);
            sb.Append(relativePath).Append('\n').Append(fileHash).Append('\n');
        }

        // 6-7. Hash the concatenated UTF-8 bytes
        var manifestBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var finalHash = ComputeSha256Hex(manifestBytes);
        return $"sha256:{finalHash}";
    }

    private static IEnumerable<string> EnumerateNonHiddenFiles(string rootPath)
    {
        return Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
            .Where(f => !ContainsHiddenSegment(rootPath, f));
    }

    private static bool ContainsHiddenSegment(string rootPath, string fullPath)
    {
        var relativePath = Path.GetRelativePath(rootPath, fullPath);
        return relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment.StartsWith('.'));
    }

    private static string NormalizeRelativePath(string rootPath, string fullPath)
    {
        var relativePath = Path.GetRelativePath(rootPath, fullPath);
        // Normalize to forward slashes
        var normalized = relativePath.Replace('\\', '/');
        // Normalize to NFC Unicode form
        return normalized.Normalize(NormalizationForm.FormC);
    }

    private static string ComputeSha256Hex(byte[] data)
    {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexStringLower(hashBytes);
    }
}
