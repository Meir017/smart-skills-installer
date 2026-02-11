using SmartSkills.Core.Fetching;

namespace SmartSkills.Core.Tests;

public class GitHubContentFetcherTests
{
    [Fact]
    public void BuildRawUrl_ReturnsCorrectUrl()
    {
        var url = GitHubContentFetcher.BuildRawUrl("owner", "repo", "main", "skills/manifest.json");
        Assert.Equal("https://raw.githubusercontent.com/owner/repo/main/skills/manifest.json", url);
    }

    [Fact]
    public void BuildApiContentsUrl_WithoutBranch_ReturnsCorrectUrl()
    {
        var url = GitHubContentFetcher.BuildApiContentsUrl("owner", "repo", "skills");
        Assert.Equal("https://api.github.com/repos/owner/repo/contents/skills", url);
    }

    [Fact]
    public void BuildApiContentsUrl_WithBranch_IncludesRef()
    {
        var url = GitHubContentFetcher.BuildApiContentsUrl("owner", "repo", "skills", "develop");
        Assert.Equal("https://api.github.com/repos/owner/repo/contents/skills?ref=develop", url);
    }
}
