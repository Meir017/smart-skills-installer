using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class RequirementsTxtPackageResolverTests
{
    private static string TestDataPath => Path.Combine(AppContext.BaseDirectory, "TestData", "Python");

    [Fact]
    public void ParseRequirementsFile_ReturnsAllPackages()
    {
        var filePath = Path.Combine(TestDataPath, "requirements.txt");
        var packages = new List<ResolvedPackage>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        RequirementsTxtPackageResolver.ParseRequirementsFile(filePath, packages, seen);

        // azure-identity, fastapi, pydantic from requirements.txt + pytest from dev-requirements.txt
        Assert.Equal(4, packages.Count);
        Assert.All(packages, p => Assert.Equal(Ecosystems.Python, p.Ecosystem));
        Assert.All(packages, p => Assert.False(p.IsTransitive));
    }

    [Fact]
    public void ParseRequirementsFile_CommentsAndBlanksSkipped()
    {
        var filePath = Path.Combine(TestDataPath, "requirements.txt");
        var packages = new List<ResolvedPackage>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        RequirementsTxtPackageResolver.ParseRequirementsFile(filePath, packages, seen);

        // Should not contain any comment or option entries
        Assert.DoesNotContain(packages, p => p.Name.StartsWith('#'));
        Assert.DoesNotContain(packages, p => p.Name.StartsWith('-'));
    }

    [Fact]
    public void ParseRequirementsFile_IncludesFollowed()
    {
        var filePath = Path.Combine(TestDataPath, "requirements.txt");
        var packages = new List<ResolvedPackage>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        RequirementsTxtPackageResolver.ParseRequirementsFile(filePath, packages, seen);

        // pytest comes from -r dev-requirements.txt
        Assert.Contains(packages, p => p.Name == "pytest");
    }

    [Fact]
    public void ParseRequirementLine_StandardFormats()
    {
        var result1 = RequirementsTxtPackageResolver.ParseRequirementLine("azure-identity==1.16.0");
        Assert.NotNull(result1);
        Assert.Equal("azure-identity", result1.Value.Name);
        Assert.Equal("1.16.0", result1.Value.Version);

        var result2 = RequirementsTxtPackageResolver.ParseRequirementLine("fastapi>=0.100.0");
        Assert.NotNull(result2);
        Assert.Equal("fastapi", result2.Value.Name);
    }

    [Fact]
    public void ParseRequirementLine_ExtrasStripped()
    {
        var result = RequirementsTxtPackageResolver.ParseRequirementLine("pydantic[email]>=2.0");
        Assert.NotNull(result);
        Assert.Equal("pydantic", result.Value.Name);
    }

    [Fact]
    public void ParseRequirementLine_NamesNormalized()
    {
        var result = RequirementsTxtPackageResolver.ParseRequirementLine("Azure_Identity==1.0");
        Assert.NotNull(result);
        Assert.Equal("azure-identity", result.Value.Name);
    }
}
