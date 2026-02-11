namespace SmartSkills.Core.Configuration;

public record SmartSkillsConfig
{
    /// <summary>Configured skill source providers, in priority order.</summary>
    public IReadOnlyList<SourceProviderConfig> Sources { get; init; } = [];

    /// <summary>Local directory for installed skills.</summary>
    public string? SkillsOutputDirectory { get; init; }
}

public record SourceProviderConfig
{
    /// <summary>Provider type identifier (e.g. "github", "azuredevops").</summary>
    public required string ProviderType { get; init; }

    /// <summary>Repository URL.</summary>
    public required string Url { get; init; }

    /// <summary>Optional path to the registry index file within the repo.</summary>
    public string? RegistryIndexPath { get; init; }

    /// <summary>Optional credential key for authenticated sources.</summary>
    public string? CredentialKey { get; init; }

    /// <summary>Optional branch name (defaults to main).</summary>
    public string? Branch { get; init; }
}
