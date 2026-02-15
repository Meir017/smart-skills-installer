using SmartSkills.Core.Scanning;

namespace SmartSkills.Core.Tests.Fakes;

internal sealed class FakeProjectDetector(IReadOnlyList<DetectedProject> results) : IProjectDetector
{
    public ProjectDetectionOptions? LastOptionsUsed { get; private set; }

    public IReadOnlyList<DetectedProject> Detect(string directoryPath) =>
        Detect(directoryPath, new ProjectDetectionOptions());

    public IReadOnlyList<DetectedProject> Detect(string directoryPath, ProjectDetectionOptions options)
    {
        LastOptionsUsed = options;
        return results;
    }
}
