using SmartSkills.Core.Providers;

namespace SmartSkills.Core.Registry;

/// <summary>
/// Loads registry entries from the embedded base registry and all configured source providers.
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
        // Start with the embedded base registry (no provider attached — uses default)
        var allEntries = new List<RegistryEntry>(RegistryIndexParser.LoadEmbedded());

        // Merge entries from remote providers, stamping each with its source provider
        foreach (var provider in _providers)
        {
            var entries = await provider.GetRegistryIndexAsync(cancellationToken);
            foreach (var entry in entries)
            {
                allEntries.Add(entry with { SourceProvider = provider });
            }
        }

        return allEntries;
    }
}
