using System.Text.Json;
using SmartSkills.Core.Models;

namespace SmartSkills.Core.Registry;

public static class SkillManifestValidator
{
    public static (bool IsValid, List<string> Errors) Validate(SkillManifest manifest)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(manifest.SkillId))
            errors.Add("'skillId' is required.");

        if (string.IsNullOrWhiteSpace(manifest.Name))
            errors.Add("'name' is required.");

        if (string.IsNullOrWhiteSpace(manifest.Version))
            errors.Add("'version' is required.");

        if (string.IsNullOrWhiteSpace(manifest.Author))
            errors.Add("'author' is required.");

        if (manifest.Triggers.Count == 0)
            errors.Add("'triggers' must contain at least one entry.");

        foreach (var trigger in manifest.Triggers)
        {
            if (string.IsNullOrWhiteSpace(trigger.LibraryPattern))
                errors.Add("Each trigger must have a non-empty 'libraryPattern'.");
        }

        if (manifest.InstallSteps.Count == 0)
            errors.Add("'installSteps' must contain at least one entry.");

        foreach (var step in manifest.InstallSteps)
        {
            if (string.IsNullOrWhiteSpace(step.Action))
                errors.Add("Each install step must have a non-empty 'action'.");
        }

        return (errors.Count == 0, errors);
    }

    public static (SkillManifest? Manifest, List<string> Errors) ParseAndValidate(string json)
    {
        SkillManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<SkillManifest>(json);
        }
        catch (JsonException ex)
        {
            return (null, [$"Invalid JSON: {ex.Message}"]);
        }

        if (manifest is null)
            return (null, ["Deserialization returned null."]);

        var (isValid, errors) = Validate(manifest);
        return isValid ? (manifest, []) : (null, errors);
    }
}
