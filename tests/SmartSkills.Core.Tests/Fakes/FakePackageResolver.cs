using SmartSkills.Core.Scanning;

namespace SmartSkills.Core.Tests.Fakes;

internal sealed class FakePackageResolver : IPackageResolver
{
    private readonly Func<string, ProjectPackages> _resolve;

    public FakePackageResolver(ProjectPackages fixedResult)
        : this(_ => fixedResult) { }

    public FakePackageResolver(Func<string, ProjectPackages> resolve) =>
        _resolve = resolve;

    public Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(_resolve(projectPath));
}
