using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class PypiNameNormalizerTests
{
    [Theory]
    [InlineData("Azure_Identity", "azure-identity")]
    [InlineData("my.package", "my-package")]
    [InlineData("already-normal", "already-normal")]
    [InlineData("UPPER__CASE", "upper-case")]
    [InlineData("Mixed.Under_Score", "mixed-under-score")]
    [InlineData("a___b...c---d", "a-b-c-d")]
    [InlineData("simple", "simple")]
    [InlineData("Azure-Identity", "azure-identity")]
    public void Normalize_HandlesAllCases(string input, string expected)
    {
        Assert.Equal(expected, PypiNameNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => PypiNameNormalizer.Normalize(null!));
    }

    [Fact]
    public void Normalize_ThrowsOnEmpty()
    {
        Assert.Throws<ArgumentException>(() => PypiNameNormalizer.Normalize(""));
    }
}
