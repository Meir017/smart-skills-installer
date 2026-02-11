using SmartSkills.Core.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SmartSkills.Core.Tests;

public class ConfigProviderTests
{
    [Fact]
    public async Task LoadAsync_NoConfigFiles_ReturnsDefaults()
    {
        var provider = new ConfigProvider(null, NullLogger<ConfigProvider>.Instance);
        var config = await provider.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(config.Sources);
        Assert.Null(config.SkillsOutputDirectory);
    }

    [Fact]
    public async Task LoadAsync_WithOverridePath_LoadsFromFile()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, """
            {
              "sources": [
                { "providerType": "github", "url": "https://github.com/test/repo" }
              ],
              "skillsOutputDirectory": "/custom/path"
            }
            """, TestContext.Current.CancellationToken);

            var provider = new ConfigProvider(tempFile, NullLogger<ConfigProvider>.Instance);
            var config = await provider.LoadAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Single(config.Sources);
            Assert.Equal("github", config.Sources[0].ProviderType);
            Assert.Equal("/custom/path", config.SkillsOutputDirectory);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
