using Microsoft.Extensions.Logging.Abstractions;
using SmartSkills.Core.Providers;
using SmartSkills.Core.Providers.AzureDevOps;
using SmartSkills.Core.Providers.GitHub;
using Xunit;

namespace SmartSkills.Core.Tests;

public class SkillSourceProviderFactoryTests
{
    private static readonly IHttpClientFactory HttpClientFactory = new SimpleHttpClientFactory();

    private readonly SkillSourceProviderFactory _factory = new(NullLoggerFactory.Instance, HttpClientFactory);

    private sealed class SimpleHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    [Fact]
    public void CreateFromRepoUrl_GitHubUrl_ReturnsGitHubProvider()
    {
        var provider = _factory.CreateFromRepoUrl("https://github.com/owner/repo");

        Assert.NotNull(provider);
        Assert.IsType<GitHubSkillSourceProvider>(provider);
        Assert.Equal("github", provider.ProviderType);
    }

    [Fact]
    public void CreateFromRepoUrl_AdoUrl_ReturnsAdoProvider()
    {
        var provider = _factory.CreateFromRepoUrl("https://dev.azure.com/org/project/_git/repo");

        Assert.NotNull(provider);
        Assert.IsType<AdoSkillSourceProvider>(provider);
        Assert.Equal("azuredevops", provider.ProviderType);
    }

    [Fact]
    public void CreateFromRepoUrl_InvalidGitHubUrl_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _factory.CreateFromRepoUrl("https://github.com/owneronly"));
    }

    [Fact]
    public void CreateFromRepoUrl_InvalidAdoUrl_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _factory.CreateFromRepoUrl("https://dev.azure.com/org/project/badpath"));
    }

    [Fact]
    public void CreateFromRepoUrl_UnsupportedHost_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _factory.CreateFromRepoUrl("https://gitlab.com/owner/repo"));
    }

    [Fact]
    public void CreateFromRepoUrl_SameUrl_ReturnsCachedInstance()
    {
        var provider1 = _factory.CreateFromRepoUrl("https://github.com/owner/repo-cached");
        var provider2 = _factory.CreateFromRepoUrl("https://github.com/owner/repo-cached");

        Assert.Same(provider1, provider2);
    }

    [Fact]
    public void CreateFromRepoUrl_DifferentUrls_ReturnsDifferentInstances()
    {
        var provider1 = _factory.CreateFromRepoUrl("https://github.com/owner/repo-a");
        var provider2 = _factory.CreateFromRepoUrl("https://github.com/owner/repo-b");

        Assert.NotSame(provider1, provider2);
    }
}
