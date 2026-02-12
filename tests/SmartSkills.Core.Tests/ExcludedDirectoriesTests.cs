using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class ExcludedDirectoriesTests
{
    [Theory]
    [InlineData(".git")]
    [InlineData(".hg")]
    [InlineData(".svn")]
    [InlineData(".vs")]
    [InlineData(".vscode")]
    [InlineData(".idea")]
    [InlineData("bin")]
    [InlineData("obj")]
    [InlineData("node_modules")]
    [InlineData(".next")]
    [InlineData(".nuxt")]
    [InlineData("bower_components")]
    [InlineData("venv")]
    [InlineData(".venv")]
    [InlineData("__pycache__")]
    [InlineData(".tox")]
    [InlineData(".mypy_cache")]
    [InlineData(".pytest_cache")]
    [InlineData("site-packages")]
    [InlineData("target")]
    [InlineData(".gradle")]
    [InlineData("build")]
    [InlineData("dist")]
    [InlineData("vendor")]
    [InlineData("coverage")]
    [InlineData(".cache")]
    public void All_ContainsExpectedDirectory(string dirName)
    {
        Assert.Contains(dirName, ExcludedDirectories.All);
    }

    [Theory]
    [InlineData("Node_Modules")]
    [InlineData("NODE_MODULES")]
    [InlineData(".GIT")]
    [InlineData("BIN")]
    [InlineData("OBJ")]
    [InlineData("Venv")]
    public void All_IsCaseInsensitive(string dirName)
    {
        Assert.Contains(dirName, ExcludedDirectories.All);
    }

    [Fact]
    public void Universal_ContainsExpectedEntries()
    {
        Assert.Contains(".git", ExcludedDirectories.Universal);
        Assert.Contains(".hg", ExcludedDirectories.Universal);
        Assert.Contains(".svn", ExcludedDirectories.Universal);
        Assert.Contains(".vs", ExcludedDirectories.Universal);
        Assert.Contains(".vscode", ExcludedDirectories.Universal);
        Assert.Contains(".idea", ExcludedDirectories.Universal);
    }

    [Fact]
    public void DotNet_ContainsExpectedEntries()
    {
        Assert.Contains("bin", ExcludedDirectories.DotNet);
        Assert.Contains("obj", ExcludedDirectories.DotNet);
    }

    [Fact]
    public void NodeJs_ContainsExpectedEntries()
    {
        Assert.Contains("node_modules", ExcludedDirectories.NodeJs);
        Assert.Contains(".next", ExcludedDirectories.NodeJs);
        Assert.Contains(".nuxt", ExcludedDirectories.NodeJs);
        Assert.Contains("bower_components", ExcludedDirectories.NodeJs);
    }

    [Fact]
    public void Python_ContainsExpectedEntries()
    {
        Assert.Contains("venv", ExcludedDirectories.Python);
        Assert.Contains(".venv", ExcludedDirectories.Python);
        Assert.Contains("__pycache__", ExcludedDirectories.Python);
        Assert.Contains(".tox", ExcludedDirectories.Python);
        Assert.Contains(".mypy_cache", ExcludedDirectories.Python);
        Assert.Contains(".pytest_cache", ExcludedDirectories.Python);
        Assert.Contains("site-packages", ExcludedDirectories.Python);
    }

    [Fact]
    public void Java_ContainsExpectedEntries()
    {
        Assert.Contains("target", ExcludedDirectories.Java);
        Assert.Contains(".gradle", ExcludedDirectories.Java);
        Assert.Contains("build", ExcludedDirectories.Java);
    }

    [Fact]
    public void BuildOutput_ContainsExpectedEntries()
    {
        Assert.Contains("dist", ExcludedDirectories.BuildOutput);
        Assert.Contains("vendor", ExcludedDirectories.BuildOutput);
        Assert.Contains("coverage", ExcludedDirectories.BuildOutput);
        Assert.Contains(".cache", ExcludedDirectories.BuildOutput);
    }

    [Fact]
    public void All_IsSupersetOfAllCategories()
    {
        Assert.Subset(ExcludedDirectories.Universal as ISet<string> ?? new HashSet<string>(ExcludedDirectories.Universal), (ISet<string>)ExcludedDirectories.All);
        Assert.Subset(ExcludedDirectories.DotNet as ISet<string> ?? new HashSet<string>(ExcludedDirectories.DotNet), (ISet<string>)ExcludedDirectories.All);
        Assert.Subset(ExcludedDirectories.NodeJs as ISet<string> ?? new HashSet<string>(ExcludedDirectories.NodeJs), (ISet<string>)ExcludedDirectories.All);
        Assert.Subset(ExcludedDirectories.Python as ISet<string> ?? new HashSet<string>(ExcludedDirectories.Python), (ISet<string>)ExcludedDirectories.All);
        Assert.Subset(ExcludedDirectories.Java as ISet<string> ?? new HashSet<string>(ExcludedDirectories.Java), (ISet<string>)ExcludedDirectories.All);
        Assert.Subset(ExcludedDirectories.BuildOutput as ISet<string> ?? new HashSet<string>(ExcludedDirectories.BuildOutput), (ISet<string>)ExcludedDirectories.All);
    }
}
