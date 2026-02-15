using Xunit;

namespace SmartSkills.Cli.Tests;

[Collection("CLI")]
public class CliEndToEndTests
{
    [Fact]
    public async Task Help_DisplaysUsageInfo()
    {
        var (exitCode, output) = await CliFixture.RunCliAsync("--help");

        Assert.Equal(0, exitCode);
        Assert.Contains("SmartSkills", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scan", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("install", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Version_DisplaysVersion()
    {
        var (exitCode, output) = await CliFixture.RunCliAsync("--version");

        Assert.Equal(0, exitCode);
        Assert.NotEmpty(output.Trim());
    }

    [Fact]
    public async Task List_WhenNoSkillsInstalled_ShowsMessage()
    {
        var (exitCode, output) = await CliFixture.RunCliAsync("list");

        Assert.Equal(0, exitCode);
        Assert.Contains("No skills installed", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownCommand_ReturnsError()
    {
        var (exitCode, _) = await CliFixture.RunCliAsync("nonexistent");

        Assert.NotEqual(0, exitCode);
    }
}
