namespace SmartSkills.Core.Providers;

/// <summary>
/// Creates <see cref="ISkillSourceProvider"/> instances from repository URLs.
/// </summary>
public interface ISkillSourceProviderFactory
{
    /// <summary>
    /// Creates a provider that can fetch skills from the specified repository URL.
    /// Supports GitHub (https://github.com/owner/repo) and Azure DevOps URLs.
    /// </summary>
#pragma warning disable CA1054 // URI parameters should not be strings
    ISkillSourceProvider CreateFromRepoUrl(string repoUrl);
#pragma warning restore CA1054 // URI parameters should not be strings
}
