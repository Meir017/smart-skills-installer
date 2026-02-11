namespace SmartSkills.Core.Configuration;

public interface IConfigProvider
{
    /// <summary>
    /// Load merged configuration (user-level + project-level overrides).
    /// </summary>
    Task<SmartSkillsConfig> LoadAsync(string? projectPath = null, CancellationToken cancellationToken = default);
}
