using System.Text.Json.Serialization;

namespace SmartSkills.Core.Models;

public class SkillRegistry
{
    [JsonPropertyName("registryVersion")]
    public string RegistryVersion { get; set; } = "1.0";

    [JsonPropertyName("lastUpdated")]
    public string LastUpdated { get; set; } = string.Empty;

    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; } = string.Empty;

    [JsonPropertyName("entries")]
    public List<SkillRegistryEntry> Entries { get; set; } = [];
}

public class SkillRegistryEntry
{
    [JsonPropertyName("libraryPattern")]
    public string LibraryPattern { get; set; } = string.Empty;

    [JsonPropertyName("skillManifestUrls")]
    public List<string> SkillManifestUrls { get; set; } = [];
}
