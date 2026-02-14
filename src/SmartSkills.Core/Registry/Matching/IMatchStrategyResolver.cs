namespace SmartSkills.Core.Registry.Matching;

/// <summary>
/// Resolves <see cref="IMatchStrategy"/> implementations by name.
/// </summary>
public interface IMatchStrategyResolver
{
    IMatchStrategy Resolve(string strategyName);
}
