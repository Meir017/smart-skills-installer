using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class EcosystemModelTests
{
    [Fact]
    public void ResolvedPackage_DefaultEcosystem_IsDotnet()
    {
        var pkg = new ResolvedPackage { Name = "Foo", Version = "1.0", IsTransitive = false };
        Assert.Equal(Ecosystems.Dotnet, pkg.Ecosystem);
    }

    [Fact]
    public void ResolvedPackage_SetEcosystem_Npm()
    {
        var pkg = new ResolvedPackage { Name = "express", Version = "4.18.0", IsTransitive = false, Ecosystem = Ecosystems.JavaScript };
        Assert.Equal("javascript", pkg.Ecosystem);
    }

    [Fact]
    public void Ecosystems_Constants_AreCorrect()
    {
        Assert.Equal("dotnet", Ecosystems.Dotnet);
        Assert.Equal("javascript", Ecosystems.JavaScript);
    }
}
