using SmartSkills.Core.Registry.Matching;
using Xunit;

namespace SmartSkills.Core.Tests;

public class MatchStrategyResolverTests
{
    [Fact]
    public void Resolve_RegisteredStrategy_ReturnsCorrectStrategy()
    {
        var strategy = new FakeMatchStrategy("package");
        var resolver = new MatchStrategyResolver([strategy]);

        var result = resolver.Resolve("package");

        Assert.Same(strategy, result);
    }

    [Fact]
    public void Resolve_CaseInsensitive_ReturnsMatch()
    {
        var strategy = new FakeMatchStrategy("package");
        var resolver = new MatchStrategyResolver([strategy]);

        var result = resolver.Resolve("PACKAGE");

        Assert.Same(strategy, result);
    }

    [Fact]
    public void Resolve_UnknownStrategy_ThrowsInvalidOperationException()
    {
        var resolver = new MatchStrategyResolver([new FakeMatchStrategy("package")]);

        var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve("unknown"));

        Assert.Contains("package", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Resolve_NullOrEmpty_ThrowsArgumentException(string? strategyName)
    {
        var resolver = new MatchStrategyResolver([new FakeMatchStrategy("package")]);

        Assert.ThrowsAny<ArgumentException>(() => resolver.Resolve(strategyName!));
    }

    [Fact]
    public void Constructor_NullStrategies_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MatchStrategyResolver(null!));
    }

    [Fact]
    public void Resolve_MultipleStrategies_ReturnsCorrectOne()
    {
        var package = new FakeMatchStrategy("package");
        var fileExists = new FakeMatchStrategy("file-exists");
        var resolver = new MatchStrategyResolver([package, fileExists]);

        Assert.Same(package, resolver.Resolve("package"));
        Assert.Same(fileExists, resolver.Resolve("file-exists"));
    }

    private sealed class FakeMatchStrategy(string name) : IMatchStrategy
    {
        public string Name => name;

        public MatchResult Evaluate(MatchContext context, IReadOnlyList<string> criteria)
            => MatchResult.NoMatch;
    }
}
