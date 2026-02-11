using System.Text.Json.Serialization;

namespace SmartSkills.Core.Models;

public class SkillManifest
{
    [JsonPropertyName("skillId")]
    public string SkillId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("sourceUrl")]
    public string SourceUrl { get; set; } = string.Empty;

    [JsonPropertyName("triggers")]
    public List<SkillTrigger> Triggers { get; set; } = [];

    [JsonPropertyName("installSteps")]
    public List<InstallStep> InstallSteps { get; set; } = [];

    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = [];
}

public class SkillTrigger
{
    [JsonPropertyName("libraryPattern")]
    public string LibraryPattern { get; set; } = string.Empty;

    [JsonPropertyName("minVersion")]
    public string? MinVersion { get; set; }

    [JsonPropertyName("maxVersion")]
    public string? MaxVersion { get; set; }
}

public class InstallStep
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("destination")]
    public string Destination { get; set; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }
}
