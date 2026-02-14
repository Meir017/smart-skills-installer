using SmartSkills.Core.Scanning;

namespace SmartSkills.Core.Registry;

/// <summary>
/// Matches detected packages and project files to applicable skills from the registry.
/// </summary>
public interface ISkillMatcher
{
    IReadOnlyList<MatchedSkill> Match(
        IEnumerable<ResolvedPackage> packages,
        IEnumerable<RegistryEntry> registryEntries,
        IReadOnlyList<string>? rootFileNames = null);
}
