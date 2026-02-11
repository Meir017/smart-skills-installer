using SmartSkills.Core.Installation;
using Xunit;

namespace SmartSkills.Core.Tests;

public class SkillMetadataParserTests
{
    private readonly SkillMetadataParser _parser = new();

    [Fact]
    public void Parse_ValidFrontmatter_ReturnsMetadata()
    {
        var content = """
            ---
            name: my-skill
            description: A test skill for unit testing.
            license: MIT
            ---
            # My Skill
            Instructions here.
            """;

        var result = _parser.Parse(content, out var errors);

        Assert.NotNull(result);
        Assert.Empty(errors);
        Assert.Equal("my-skill", result.Name);
        Assert.Equal("A test skill for unit testing.", result.Description);
        Assert.Equal("MIT", result.License);
    }

    [Fact]
    public void Parse_MissingFrontmatter_ReturnsNull()
    {
        var content = "# No Frontmatter\nJust markdown.";

        var result = _parser.Parse(content, out var errors);

        Assert.Null(result);
        Assert.Contains(errors, e => e.Contains("frontmatter"));
    }

    [Fact]
    public void Parse_MissingName_ReturnsNull()
    {
        var content = """
            ---
            description: A skill without a name.
            ---
            """;

        var result = _parser.Parse(content, out var errors);

        Assert.Null(result);
        Assert.Contains(errors, e => e.Contains("name"));
    }

    [Fact]
    public void Parse_InvalidNameFormat_ReturnsNull()
    {
        var content = """
            ---
            name: Invalid_Name!
            description: A test skill.
            ---
            """;

        var result = _parser.Parse(content, out var errors);

        Assert.Null(result);
        Assert.Contains(errors, e => e.Contains("name"));
    }

    [Fact]
    public void Parse_DescriptionTooLong_ReturnsNull()
    {
        var longDesc = new string('a', 1025);
        var content = $"---\nname: test-skill\ndescription: {longDesc}\n---";

        var result = _parser.Parse(content, out var errors);

        Assert.Null(result);
        Assert.Contains(errors, e => e.Contains("description"));
    }
}
