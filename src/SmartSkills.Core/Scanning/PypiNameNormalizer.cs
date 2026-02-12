using System.Text.RegularExpressions;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Normalizes Python package names per PEP 503:
/// lowercase, replace underscores/dots/consecutive-hyphens with single hyphens.
/// </summary>
public static partial class PypiNameNormalizer
{
    [GeneratedRegex(@"[-_.]+")]
    private static partial Regex SeparatorPattern();

    public static string Normalize(string packageName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);
        return SeparatorPattern().Replace(packageName.ToLowerInvariant(), "-");
    }
}
