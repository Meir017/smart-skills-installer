using SmartSkills.Core.Providers;

namespace SmartSkills.Core.Registry;

/// <summary>
/// Loads registry entries from all configured source providers.
/// </summary>
public sealed class SkillRegistry : ISkillRegistry
{
    private readonly IEnumerable<ISkillSourceProvider> _providers;

    public SkillRegistry(IEnumerable<ISkillSourceProvider> providers)
    {
        _providers = providers;
    }

    public async Task<IReadOnlyList<RegistryEntry>> GetRegistryEntriesAsync(CancellationToken cancellationToken = default)
    {
        var allEntries = new List<RegistryEntry>();

        foreach (var provider in _providers)
        {
            var entries = await provider.GetRegistryIndexAsync(cancellationToken);
            allEntries.AddRange(entries);
        }

        return allEntries;
    }
}
