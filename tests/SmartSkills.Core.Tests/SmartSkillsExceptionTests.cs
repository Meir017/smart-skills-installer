using SmartSkills.Core.Resilience;
using Xunit;

namespace SmartSkills.Core.Tests;

public class SmartSkillsExceptionTests
{
    [Fact]
    public void NetworkError_SetsErrorCodeAndRemediation()
    {
        var inner = new InvalidOperationException("connection refused");

        var ex = SmartSkillsException.NetworkError(inner);

        Assert.Equal("SS001", ex.ErrorCode);
        Assert.Contains("connection refused", ex.Message, StringComparison.Ordinal);
        Assert.NotNull(ex.Remediation);
        Assert.Contains("internet", ex.Remediation, StringComparison.OrdinalIgnoreCase);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void AuthFailed_Ado_SetsAdoRemediation()
    {
        var ex = SmartSkillsException.AuthFailed("ado");

        Assert.Equal("SS002", ex.ErrorCode);
        Assert.NotNull(ex.Remediation);
        Assert.Contains("az login", ex.Remediation, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthFailed_GitHub_SetsGitHubRemediation()
    {
        var ex = SmartSkillsException.AuthFailed("github");

        Assert.Equal("SS002", ex.ErrorCode);
        Assert.NotNull(ex.Remediation);
        Assert.Contains("public", ex.Remediation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SdkNotFound_SetsErrorCodeAndRemediation()
    {
        var ex = SmartSkillsException.SdkNotFound();

        Assert.Equal("SS003", ex.ErrorCode);
        Assert.NotNull(ex.Remediation);
        Assert.Contains("https://dot.net/download", ex.Remediation, StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultConstructor_SetsEmptyErrorCode()
    {
        var ex = new SmartSkillsException();

        Assert.Equal(string.Empty, ex.ErrorCode);
        Assert.Null(ex.Remediation);
    }

    [Fact]
    public void MessageConstructor_SetsEmptyErrorCode()
    {
        var ex = new SmartSkillsException("something went wrong");

        Assert.Equal(string.Empty, ex.ErrorCode);
        Assert.Equal("something went wrong", ex.Message);
        Assert.Null(ex.Remediation);
    }

    [Fact]
    public void FullConstructor_SetsAllProperties()
    {
        var inner = new InvalidOperationException("root cause");

        var ex = new SmartSkillsException("ERR01", "test message", "try again", inner);

        Assert.Equal("ERR01", ex.ErrorCode);
        Assert.Equal("test message", ex.Message);
        Assert.Equal("try again", ex.Remediation);
        Assert.Same(inner, ex.InnerException);
    }
}
