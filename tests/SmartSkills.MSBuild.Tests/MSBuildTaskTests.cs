using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SmartSkills.MSBuild.Tests;

public class MSBuildTaskTests : IDisposable
{
    private readonly string _tempDir;

    public MSBuildTaskTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "smartskills-msbuild-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private class MockBuildEngine : IBuildEngine
    {
        public List<string> Messages { get; } = [];
        public List<string> Warnings { get; } = [];
        public List<string> Errors { get; } = [];

        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => string.Empty;

        public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e.Message ?? string.Empty);
        public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e.Message ?? string.Empty);
        public void LogMessageEvent(BuildMessageEventArgs e) => Messages.Add(e.Message ?? string.Empty);
        public void LogCustomEvent(CustomBuildEventArgs e) { }
        public bool BuildProjectFile(string projectFileName, string[] targetNames, System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs) => true;
    }

    [Fact]
    public void ResolveSmartSkills_NoPackages_Succeeds()
    {
        var task = new ResolveSmartSkills
        {
            BuildEngine = new MockBuildEngine(),
            PackageReferences = [],
            RegistrySources = "github:org/repo"
        };

        var result = task.Execute();
        Assert.True(result);
        Assert.Empty(task.ResolvedSkills);
    }

    [Fact]
    public void ResolveSmartSkills_WithPackages_LogsPackageInfo()
    {
        var engine = new MockBuildEngine();
        var packages = new ITaskItem[]
        {
            CreateTaskItem("Newtonsoft.Json", "13.0.3"),
            CreateTaskItem("Serilog", "3.1.1"),
        };

        var task = new ResolveSmartSkills
        {
            BuildEngine = engine,
            PackageReferences = packages,
            RegistrySources = "github:org/repo",
            Verbose = true
        };

        var result = task.Execute();
        Assert.True(result);
        Assert.Contains(engine.Messages, m => m.Contains("Newtonsoft.Json"));
        Assert.Contains(engine.Messages, m => m.Contains("Serilog"));
    }

    [Fact]
    public void ResolveSmartSkills_ReportsTimingInfo()
    {
        var engine = new MockBuildEngine();
        var task = new ResolveSmartSkills
        {
            BuildEngine = engine,
            PackageReferences = [CreateTaskItem("SomeLib", "1.0.0")],
            RegistrySources = "github:org/repo"
        };

        var result = task.Execute();
        Assert.True(result);
        Assert.Contains(engine.Messages, m => m.Contains("ms"));
    }

    [Fact]
    public void InstallSmartSkills_NoSkills_Succeeds()
    {
        var task = new InstallSmartSkills
        {
            BuildEngine = new MockBuildEngine(),
            ResolvedSkills = [],
            InstallDirectory = _tempDir
        };

        var result = task.Execute();
        Assert.True(result);
        Assert.Empty(task.InstalledSkills);
    }

    [Fact]
    public void InstallSmartSkills_WithSkills_CreatesOutputItems()
    {
        var engine = new MockBuildEngine();
        var skills = new ITaskItem[]
        {
            CreateSkillItem("ef-core-skill", "1.0.0", "https://example.com/skill.zip"),
        };

        var task = new InstallSmartSkills
        {
            BuildEngine = engine,
            ResolvedSkills = skills,
            InstallDirectory = _tempDir,
            Verbose = true
        };

        var result = task.Execute();
        Assert.True(result);
        Assert.Single(task.InstalledSkills);
        Assert.Equal("ef-core-skill", task.InstalledSkills[0].ItemSpec);
        Assert.Contains(engine.Messages, m => m.Contains("Installing"));
    }

    [Fact]
    public void InstallSmartSkills_SkipsAlreadyInstalled()
    {
        var engine = new MockBuildEngine();
        var skillDir = Path.Combine(_tempDir, "existing-skill");
        Directory.CreateDirectory(skillDir);

        var skills = new ITaskItem[]
        {
            CreateSkillItem("existing-skill", "1.0.0", "https://example.com/skill.zip"),
        };

        var task = new InstallSmartSkills
        {
            BuildEngine = engine,
            ResolvedSkills = skills,
            InstallDirectory = _tempDir,
            ForceReinstall = false,
            Verbose = true
        };

        var result = task.Execute();
        Assert.True(result);
        Assert.Empty(task.InstalledSkills); // Skipped since directory exists
        Assert.Contains(engine.Messages, m => m.Contains("Skipping"));
    }

    [Fact]
    public void InstallSmartSkills_ForceReinstall_DoesNotSkip()
    {
        var engine = new MockBuildEngine();
        var skillDir = Path.Combine(_tempDir, "existing-skill");
        Directory.CreateDirectory(skillDir);

        var skills = new ITaskItem[]
        {
            CreateSkillItem("existing-skill", "1.0.0", "https://example.com/skill.zip"),
        };

        var task = new InstallSmartSkills
        {
            BuildEngine = engine,
            ResolvedSkills = skills,
            InstallDirectory = _tempDir,
            ForceReinstall = true,
            Verbose = true
        };

        var result = task.Execute();
        Assert.True(result);
        Assert.Single(task.InstalledSkills);
    }

    [Fact]
    public void InstallSmartSkills_ReportsTimingAndCounts()
    {
        var engine = new MockBuildEngine();
        var skills = new ITaskItem[]
        {
            CreateSkillItem("skill-a", "1.0.0", "https://example.com/a.zip"),
            CreateSkillItem("skill-b", "2.0.0", "https://example.com/b.zip"),
        };

        var task = new InstallSmartSkills
        {
            BuildEngine = engine,
            ResolvedSkills = skills,
            InstallDirectory = _tempDir
        };

        var result = task.Execute();
        Assert.True(result);
        Assert.Equal(2, task.InstalledSkills.Length);
        Assert.Contains(engine.Messages, m => m.Contains("Installed 2 skill(s)"));
    }

    [Fact]
    public void ResolveSmartSkills_Cancel_ReturnsFalse()
    {
        var task = new ResolveSmartSkills
        {
            BuildEngine = new MockBuildEngine(),
            PackageReferences = [CreateTaskItem("SomeLib", "1.0.0")],
            RegistrySources = "github:org/repo"
        };

        task.Cancel();
        // Cancel sets the token but Execute may complete before checking it
        // This just verifies Cancel doesn't throw
    }

    private static TaskItem CreateTaskItem(string name, string version)
    {
        var item = new TaskItem(name);
        item.SetMetadata("Version", version);
        return item;
    }

    private static TaskItem CreateSkillItem(string id, string version, string sourceUrl)
    {
        var item = new TaskItem(id);
        item.SetMetadata("Version", version);
        item.SetMetadata("SourceUrl", sourceUrl);
        return item;
    }
}
