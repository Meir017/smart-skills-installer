using SmartSkills.Core.Fetching;

namespace SmartSkills.Core.Tests;

public class RegistryFetchServiceTests
{
    [Theory]
    [InlineData("github:owner/repo", "owner", "repo", "main")]
    [InlineData("github:owner/repo@develop", "owner", "repo", "develop")]
    [InlineData("owner/repo", "owner", "repo", "main")]
    [InlineData("owner/repo@v2", "owner", "repo", "v2")]
    public void ParseGitHubSource_ValidFormats(string source, string expectedOwner, string expectedRepo, string expectedBranch)
    {
        var (owner, repo, branch) = RegistryFetchService.ParseGitHubSource(source);

        Assert.Equal(expectedOwner, owner);
        Assert.Equal(expectedRepo, repo);
        Assert.Equal(expectedBranch, branch);
    }

    [Theory]
    [InlineData("github:invalid")]
    [InlineData("github:/repo")]
    [InlineData("github:owner/")]
    [InlineData("")]
    public void ParseGitHubSource_InvalidFormats_Throws(string source)
    {
        Assert.Throws<ArgumentException>(() => RegistryFetchService.ParseGitHubSource(source));
    }
}
