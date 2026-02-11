using SmartSkills.Core.Fetching;

namespace SmartSkills.Core.Tests;

public class AdoContentFetcherTests
{
    [Theory]
    [InlineData("ado:myorg/myproj/myrepo", "myorg", "myproj", "myrepo", "main")]
    [InlineData("ado:myorg/myproj/myrepo@develop", "myorg", "myproj", "myrepo", "develop")]
    [InlineData("myorg/myproj/myrepo", "myorg", "myproj", "myrepo", "main")]
    [InlineData("myorg/myproj/myrepo@v2", "myorg", "myproj", "myrepo", "v2")]
    public void ParseAdoSource_ValidFormats(string source, string org, string project, string repo, string branch)
    {
        var result = AdoContentFetcher.ParseAdoSource(source);

        Assert.Equal(org, result.Org);
        Assert.Equal(project, result.Project);
        Assert.Equal(repo, result.Repo);
        Assert.Equal(branch, result.Branch);
    }

    [Theory]
    [InlineData("ado:invalid")]
    [InlineData("ado:org/project")]
    [InlineData("ado:org//repo")]
    [InlineData("")]
    public void ParseAdoSource_InvalidFormats_Throws(string source)
    {
        Assert.Throws<ArgumentException>(() => AdoContentFetcher.ParseAdoSource(source));
    }

    [Fact]
    public void BuildItemUrl_WithoutBranch_ReturnsCorrectUrl()
    {
        var url = AdoContentFetcher.BuildItemUrl("myorg", "myproj", "myrepo", "/skills/manifest.json");
        Assert.Contains("dev.azure.com/myorg/myproj/_apis/git/repositories/myrepo/items", url);
        Assert.Contains("api-version=7.0", url);
    }

    [Fact]
    public void BuildItemUrl_WithBranch_IncludesVersionDescriptor()
    {
        var url = AdoContentFetcher.BuildItemUrl("myorg", "myproj", "myrepo", "/skills/manifest.json", "develop");
        Assert.Contains("versionDescriptor.version=develop", url);
        Assert.Contains("versionDescriptor.versionType=branch", url);
    }

    [Fact]
    public void BuildListUrl_ReturnsCorrectUrl()
    {
        var url = AdoContentFetcher.BuildListUrl("myorg", "myproj", "myrepo", "/skills");
        Assert.Contains("recursionLevel=Full", url);
        Assert.Contains("scopePath=", url);
    }
}
