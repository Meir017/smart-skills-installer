using Microsoft.Extensions.Logging;
using SmartSkills.Core.Fetching;

namespace SmartSkills.Core.Tests;

public class AdoCredentialProviderTests
{
    [Fact]
    public void ResolveToken_ExplicitToken_ReturnsIt()
    {
        using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
        var provider = new AdoCredentialProvider(loggerFactory.CreateLogger<AdoCredentialProvider>());

        var token = provider.ResolveToken("my-explicit-token");

        Assert.Equal("my-explicit-token", token);
    }

    [Fact]
    public void ResolveToken_EnvVar_ReturnsIt()
    {
        var original = Environment.GetEnvironmentVariable("ADO_TOKEN");
        try
        {
            Environment.SetEnvironmentVariable("ADO_TOKEN", "env-token-value");
            using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
            var provider = new AdoCredentialProvider(loggerFactory.CreateLogger<AdoCredentialProvider>());

            var token = provider.ResolveToken();

            Assert.Equal("env-token-value", token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ADO_TOKEN", original);
        }
    }

    [Fact]
    public void ResolveToken_ExplicitTakesPriorityOverEnvVar()
    {
        var original = Environment.GetEnvironmentVariable("ADO_TOKEN");
        try
        {
            Environment.SetEnvironmentVariable("ADO_TOKEN", "env-token");
            using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
            var provider = new AdoCredentialProvider(loggerFactory.CreateLogger<AdoCredentialProvider>());

            var token = provider.ResolveToken("explicit-token");

            Assert.Equal("explicit-token", token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ADO_TOKEN", original);
        }
    }

    [Fact]
    public void ResolveToken_NoCredentials_ThrowsWithMessage()
    {
        var original = Environment.GetEnvironmentVariable("ADO_TOKEN");
        try
        {
            Environment.SetEnvironmentVariable("ADO_TOKEN", null);
            using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => { });
            var provider = new AdoCredentialProvider(loggerFactory.CreateLogger<AdoCredentialProvider>());

            var ex = Assert.Throws<InvalidOperationException>(() => provider.ResolveToken());
            Assert.Contains("No ADO credentials found", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ADO_TOKEN", original);
        }
    }
}
