using System.Text.Json;
using SmartSkills.Core.Models;
using SmartSkills.Core.Registry;

namespace SmartSkills.Core.Tests;

public class SkillManifestValidatorTests
{
    private static SkillManifest CreateValidManifest() => new()
    {
        SkillId = "ef-core-skill",
        Name = "Entity Framework Core Skill",
        Description = "Agent skill for EF Core operations",
        Version = "1.0.0",
        Author = "test-author",
        SourceUrl = "https://github.com/example/skills",
        Triggers =
        [
            new SkillTrigger { LibraryPattern = "Microsoft.EntityFrameworkCore", MinVersion = "7.0.0" },
            new SkillTrigger { LibraryPattern = @"Microsoft\.EntityFrameworkCore\..*" }
        ],
        InstallSteps =
        [
            new InstallStep { Action = "copy", Source = "skills/ef-core/prompt.md", Destination = "prompt.md" }
        ],
        Dependencies = ["dotnet-base-skill"]
    };

    [Fact]
    public void Validate_ValidManifest_ReturnsValid()
    {
        var manifest = CreateValidManifest();
        var (isValid, errors) = SkillManifestValidator.Validate(manifest);

        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_SupportsRegexPatternInTriggers()
    {
        var manifest = CreateValidManifest();
        var json = JsonSerializer.Serialize(manifest);
        var (parsed, errors) = SkillManifestValidator.ParseAndValidate(json);

        Assert.NotNull(parsed);
        Assert.Empty(errors);
        Assert.Contains(parsed.Triggers, t => t.LibraryPattern.Contains(".*"));
    }

    [Fact]
    public void Validate_MissingSkillId_ReturnsError()
    {
        var manifest = CreateValidManifest();
        manifest.SkillId = "";
        var (isValid, errors) = SkillManifestValidator.Validate(manifest);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("skillId"));
    }

    [Fact]
    public void Validate_EmptyTriggers_ReturnsError()
    {
        var manifest = CreateValidManifest();
        manifest.Triggers.Clear();
        var (isValid, errors) = SkillManifestValidator.Validate(manifest);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("triggers"));
    }

    [Fact]
    public void Validate_EmptyInstallSteps_ReturnsError()
    {
        var manifest = CreateValidManifest();
        manifest.InstallSteps.Clear();
        var (isValid, errors) = SkillManifestValidator.Validate(manifest);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("installSteps"));
    }

    [Fact]
    public void ParseAndValidate_InvalidJson_ReturnsError()
    {
        var (manifest, errors) = SkillManifestValidator.ParseAndValidate("not valid json {{{");

        Assert.Null(manifest);
        Assert.Single(errors);
        Assert.Contains("Invalid JSON", errors[0]);
    }

    [Fact]
    public void ParseAndValidate_ValidJsonRoundTrip_Succeeds()
    {
        var original = CreateValidManifest();
        var json = JsonSerializer.Serialize(original, new JsonSerializerOptions { WriteIndented = true });
        var (parsed, errors) = SkillManifestValidator.ParseAndValidate(json);

        Assert.NotNull(parsed);
        Assert.Empty(errors);
        Assert.Equal(original.SkillId, parsed.SkillId);
        Assert.Equal(original.Triggers.Count, parsed.Triggers.Count);
        Assert.Equal(original.Dependencies.Count, parsed.Dependencies.Count);
    }
}
