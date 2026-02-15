using System.Collections;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xunit;

namespace SmartSkills.MSBuild.Tests;

public class MSBuildTaskTests : IDisposable
{
    private readonly string _tempDir;

    public MSBuildTaskTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ResolveSmartSkills_Execute_WithValidDirectory_ReturnsTrue()
    {
        var task = new ResolveSmartSkills
        {
            BuildEngine = new FakeBuildEngine(),
            ProjectPath = _tempDir,
            OutputDirectory = _tempDir
        };

        var result = task.Execute();

        Assert.True(result);
    }

    [Fact]
    public void ResolveSmartSkills_Execute_SetsResolvedSkillsToEmpty()
    {
        var task = new ResolveSmartSkills
        {
            BuildEngine = new FakeBuildEngine(),
            ProjectPath = _tempDir,
            OutputDirectory = _tempDir
        };

        task.Execute();

        Assert.Empty(task.ResolvedSkills);
    }

    [Fact]
    public void InstallSmartSkills_Execute_WithNoSkills_ReturnsTrue()
    {
        var task = new InstallSmartSkills
        {
            BuildEngine = new FakeBuildEngine(),
            Skills = [],
            OutputDirectory = _tempDir
        };

        var result = task.Execute();

        Assert.True(result);
    }

    [Fact]
    public void InstallSmartSkills_Execute_WithSkills_ReturnsTrue()
    {
        var task = new InstallSmartSkills
        {
            BuildEngine = new FakeBuildEngine(),
            Skills = [new TaskItem("MySkill")],
            OutputDirectory = _tempDir
        };

        var result = task.Execute();

        Assert.True(result);
    }

    private sealed class FakeBuildEngine : IBuildEngine
    {
        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => "test.proj";
        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => true;
        public void LogCustomEvent(CustomBuildEventArgs e) { }
        public void LogErrorEvent(BuildErrorEventArgs e) { }
        public void LogMessageEvent(BuildMessageEventArgs e) { }
        public void LogWarningEvent(BuildWarningEventArgs e) { }
    }
}
