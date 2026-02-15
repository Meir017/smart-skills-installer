using SmartSkills.Core.Scanning;

namespace SmartSkills.Core.Tests.Fakes;

internal sealed class FakePackageResolverFactory(IPackageResolver resolver) : IPackageResolverFactory
{
    public IPackageResolver GetResolver(DetectedProject project) => resolver;
}
