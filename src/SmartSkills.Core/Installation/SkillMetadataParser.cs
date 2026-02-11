using System.Text.RegularExpressions;
using SmartSkills.Core.Registry;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SmartSkills.Core.Installation;

/// <summary>
/// Parses and validates SKILL.md frontmatter per the Agent Skills specification.
/// </summary>
public interface ISkillMetadataParser
{
    SkillMetadata? Parse(string skillMdContent, out IReadOnlyList<string> validationErrors);
}

public sealed partial class SkillMetadataParser : ISkillMetadataParser
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public SkillMetadata? Parse(string skillMdContent, out IReadOnlyList<string> validationErrors)
    {
        var errors = new List<string>();
        validationErrors = errors;

        var frontmatter = ExtractFrontmatter(skillMdContent);
        if (frontmatter is null)
        {
            errors.Add("No YAML frontmatter found (expected --- delimiters).");
            return null;
        }

        FrontmatterDto dto;
        try
        {
            dto = YamlDeserializer.Deserialize<FrontmatterDto>(frontmatter) ?? new();
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse YAML: {ex.Message}");
            return null;
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors.Add("'name' is required.");
        else if (dto.Name.Length > 64 || !NamePattern().IsMatch(dto.Name))
            errors.Add("'name' must be 1-64 lowercase alphanumeric chars or hyphens.");

        if (string.IsNullOrWhiteSpace(dto.Description))
            errors.Add("'description' is required.");
        else if (dto.Description.Length > 1024)
            errors.Add("'description' must be 1-1024 characters.");

        if (dto.Compatibility is { Length: > 500 })
            errors.Add("'compatibility' must be ≤500 characters.");

        if (errors.Count > 0)
            return null;

        return new SkillMetadata
        {
            Name = dto.Name!,
            Description = dto.Description!,
            License = dto.License,
            Compatibility = dto.Compatibility,
            Metadata = dto.Metadata ?? new Dictionary<string, string>(),
            AllowedTools = dto.AllowedTools
        };
    }

    private static string? ExtractFrontmatter(string content)
    {
        if (!content.StartsWith("---"))
            return null;

        var end = content.IndexOf("---", 3, StringComparison.Ordinal);
        if (end < 0)
            return null;

        return content[3..end].Trim();
    }

    [GeneratedRegex(@"^[a-z0-9][a-z0-9\-]*$")]
    private static partial Regex NamePattern();

    private sealed class FrontmatterDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? License { get; set; }
        public string? Compatibility { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
        public string? AllowedTools { get; set; }
    }
}
