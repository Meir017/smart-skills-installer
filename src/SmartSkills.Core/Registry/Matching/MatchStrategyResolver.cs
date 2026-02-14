namespace SmartSkills.Core.Registry.Matching;

/// <summary>
/// Default resolver backed by DI-registered <see cref="IMatchStrategy"/> instances.
/// </summary>
public sealed class MatchStrategyResolver : IMatchStrategyResolver
{
    private readonly Dictionary<string, IMatchStrategy> _strategies;

    public MatchStrategyResolver(IEnumerable<IMatchStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        _strategies = strategies.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IMatchStrategy Resolve(string strategyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(strategyName);

        if (_strategies.TryGetValue(strategyName, out var strategy))
            return strategy;

        throw new InvalidOperationException(
            $"Unknown match strategy '{strategyName}'. Registered strategies: {string.Join(", ", _strategies.Keys)}");
    }
}
