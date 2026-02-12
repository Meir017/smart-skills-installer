using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartSkills.Core.Installation;

/// <summary>
/// Represents the smart-skills.lock.json file content.
/// </summary>
public sealed class SkillsLockFile
{
    /// <summary>Schema version. Currently 1.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Map of skill name → lock entry, serialized with sorted keys.</summary>
    public SortedDictionary<string, SkillLockEntry> Skills { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>
/// A single skill entry in the lock file, containing fetch coordinates and integrity hash.
/// </summary>
public sealed class SkillLockEntry
{
    /// <summary>Repository URL from which the skill is fetched.</summary>
#pragma warning disable CA1056 // URI properties should not be strings
    public required string RemoteUrl { get; set; }
#pragma warning restore CA1056

    /// <summary>Relative path to the skill directory within the remote repository.</summary>
    public required string SkillPath { get; set; }

    /// <summary>Ecosystem filter (e.g. "dotnet", "javascript"). Null if not ecosystem-specific.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Language { get; set; }

    /// <summary>Full SHA of the most recent commit that touched the skill directory.</summary>
    public required string CommitSha { get; set; }

    /// <summary>Content hash of all installed skill files. Format: "sha256:&lt;hex&gt;".</summary>
    public required string LocalContentHash { get; set; }
}

/// <summary>
/// Provides deterministic JSON serialization for the lock file.
/// </summary>
public static class SkillsLockFileSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serialize a lock file to deterministic JSON (sorted keys, indented, camelCase).
    /// </summary>
    public static string Serialize(SkillsLockFile lockFile)
    {
        ArgumentNullException.ThrowIfNull(lockFile);
        return JsonSerializer.Serialize(lockFile, SerializerOptions);
    }

    /// <summary>
    /// Deserialize a lock file from JSON, validating the schema version.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the schema version is unsupported.</exception>
    public static SkillsLockFile Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var lockFile = JsonSerializer.Deserialize<SkillsLockFile>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Failed to deserialize lock file: result was null.");

        if (lockFile.Version != 1)
        {
            throw new InvalidOperationException(
                $"Unsupported lock file version: {lockFile.Version}. Only version 1 is supported.");
        }

        return lockFile;
    }
}
