using System.Text.Json.Serialization;

namespace SmartSkills.Core.Models;

/// <summary>
/// Represents a skill that has been resolved from the matching engine,
/// ready for download and installation.
/// </summary>
public class ResolvedSkill
{
    [JsonPropertyName("skillId")]
    public string SkillId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("manifestUrl")]
    public string ManifestUrl { get; set; } = string.Empty;

    [JsonPropertyName("manifest")]
    public SkillManifest? Manifest { get; set; }

    /// <summary>
    /// The library names that triggered this skill resolution.
    /// </summary>
    [JsonPropertyName("matchedLibraries")]
    public List<string> MatchedLibraries { get; set; } = [];
}
