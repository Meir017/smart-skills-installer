namespace SmartSkills.Core.Registry;

/// <summary>
/// Provides access to the skill registry index from configured sources.
/// </summary>
public interface ISkillRegistry
{
    /// <summary>
    /// Load the registry index entries from all configured source providers.
    /// </summary>
    Task<IReadOnlyList<RegistryEntry>> GetRegistryEntriesAsync(CancellationToken cancellationToken = default);
}
