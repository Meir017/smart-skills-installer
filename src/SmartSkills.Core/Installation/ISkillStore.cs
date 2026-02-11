namespace SmartSkills.Core.Installation;

/// <summary>
/// Manages the local skill storage and installed state tracking.
/// </summary>
public interface ISkillStore
{
    Task<IReadOnlyList<InstalledSkill>> GetInstalledSkillsAsync(CancellationToken cancellationToken = default);
    Task<InstalledSkill?> GetByNameAsync(string skillName, CancellationToken cancellationToken = default);
    Task SaveAsync(InstalledSkill skill, CancellationToken cancellationToken = default);
    Task RemoveAsync(string skillName, CancellationToken cancellationToken = default);
}
