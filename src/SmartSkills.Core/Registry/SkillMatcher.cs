using SmartSkills.Core.Registry.Matching;
using SmartSkills.Core.Scanning;

namespace SmartSkills.Core.Registry;

/// <summary>
/// Matches registry entries to projects by delegating to <see cref="IMatchStrategy"/> implementations.
/// </summary>
public sealed class SkillMatcher : ISkillMatcher
{
    private readonly IMatchStrategyResolver _strategyResolver;

    public SkillMatcher(IMatchStrategyResolver strategyResolver)
    {
        ArgumentNullException.ThrowIfNull(strategyResolver);
        _strategyResolver = strategyResolver;
    }

    /// <summary>
    /// Convenience constructor that registers the built-in strategies.
    /// </summary>
    public SkillMatcher()
        : this(new MatchStrategyResolver([new PackageMatchStrategy(), new FileExistsMatchStrategy()]))
    {
    }

    public IReadOnlyList<MatchedSkill> Match(
        IEnumerable<ResolvedPackage> packages,
        IEnumerable<RegistryEntry> registryEntries,
        IReadOnlyList<string>? rootFileNames = null)
    {
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(registryEntries);

        var packageList = packages.ToList();
        var results = new Dictionary<string, MatchedSkill>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in registryEntries)
        {
            if (results.ContainsKey(entry.SkillPath))
                continue;

            var context = new MatchContext
            {
                ResolvedPackages = packageList,
                RootFileNames = rootFileNames ?? [],
                Language = entry.Language
            };

            var strategy = _strategyResolver.Resolve(entry.Type);
            var result = strategy.Evaluate(context, entry.MatchCriteria);

            if (result.IsMatch)
            {
                results[entry.SkillPath] = new MatchedSkill
                {
                    RegistryEntry = entry,
                    MatchedPatterns = result.MatchedPatterns
                };
            }
        }

        return results.Values.ToList();
    }
}
