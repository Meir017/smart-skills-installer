using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SmartSkills.Core.Providers.AzureDevOps;
using SmartSkills.Core.Providers.GitHub;

namespace SmartSkills.Core.Providers;

/// <summary>
/// Creates <see cref="ISkillSourceProvider"/> instances from repository URLs.
/// Caches providers per unique owner/repo combination.
/// </summary>
public sealed class SkillSourceProviderFactory : ISkillSourceProviderFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, ISkillSourceProvider> _cache = new(StringComparer.OrdinalIgnoreCase);

    public SkillSourceProviderFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public ISkillSourceProvider CreateFromRepoUrl(string repoUrl)
    {
        return _cache.GetOrAdd(repoUrl, CreateProvider);
    }

    private ISkillSourceProvider CreateProvider(string repoUrl)
    {
        var uri = new Uri(repoUrl);

        if (uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            // Parse: https://github.com/{owner}/{repo}
            var segments = uri.AbsolutePath.Trim('/').Split('/', 3);
            if (segments.Length < 2)
                throw new ArgumentException($"Invalid GitHub URL: {repoUrl}. Expected format: https://github.com/owner/repo");

            return new GitHubSkillSourceProvider(
                segments[0],
                segments[1],
                branch: "main",
                registryIndexPath: null,
                _loggerFactory.CreateLogger<GitHubSkillSourceProvider>(),
                _loggerFactory.CreateLogger<GitHubHttpClient>());
        }
        else if (uri.Host.EndsWith("dev.azure.com", StringComparison.OrdinalIgnoreCase) ||
                 uri.Host.EndsWith("visualstudio.com", StringComparison.OrdinalIgnoreCase))
        {
            // Parse: https://dev.azure.com/{org}/{project}/_git/{repo}
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            string org, project, repo;
            if (segments.Length >= 4 && segments[2] == "_git")
            {
                org = segments[0];
                project = segments[1];
                repo = segments[3];
            }
            else
            {
                throw new ArgumentException($"Invalid Azure DevOps URL: {repoUrl}. Expected format: https://dev.azure.com/org/project/_git/repo");
            }

            return new AdoSkillSourceProvider(
                org, project, repo,
                branch: "main",
                registryIndexPath: null,
                _loggerFactory.CreateLogger<AdoSkillSourceProvider>(),
                _loggerFactory.CreateLogger<AdoHttpClient>());
        }
        else
        {
            throw new ArgumentException($"Unsupported repository host: {uri.Host}. Supported: github.com, dev.azure.com");
        }
    }
}
