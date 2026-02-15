using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class DotnetCliPackageResolverTests
{
    [Fact]
    public void ParseJsonOutput_WithTopLevelAndTransitivePackages_ReturnsCorrectPackages()
    {
        var json = """
        {
          "version": 1,
          "parameters": "",
          "projects": [
            {
              "path": "test.csproj",
              "frameworks": [
                {
                  "framework": "net10.0",
                  "topLevelPackages": [
                    { "id": "Newtonsoft.Json", "requestedVersion": "13.0.3", "resolvedVersion": "13.0.3" }
                  ],
                  "transitivePackages": [
                    { "id": "System.Runtime", "resolvedVersion": "4.3.0" }
                  ]
                }
              ]
            }
          ]
        }
        """;

        var result = DotnetCliPackageResolver.ParseJsonOutput("test.csproj", json);

        Assert.Equal("test.csproj", result.ProjectPath);
        Assert.Equal(2, result.Packages.Count);
        Assert.Contains(result.Packages, p => p.Name == "Newtonsoft.Json" && !p.IsTransitive && p.Version == "13.0.3");
        Assert.Contains(result.Packages, p => p.Name == "System.Runtime" && p.IsTransitive);
    }

    [Fact]
    public void ParseJsonOutput_WithMultipleFrameworks_ReturnsAllPackages()
    {
        var json = """
        {
          "version": 1,
          "projects": [
            {
              "path": "test.csproj",
              "frameworks": [
                {
                  "framework": "net8.0",
                  "topLevelPackages": [
                    { "id": "PkgA", "resolvedVersion": "1.0.0" }
                  ]
                },
                {
                  "framework": "net10.0",
                  "topLevelPackages": [
                    { "id": "PkgB", "resolvedVersion": "2.0.0" }
                  ]
                }
              ]
            }
          ]
        }
        """;

        var result = DotnetCliPackageResolver.ParseJsonOutput("test.csproj", json);

        Assert.Equal(2, result.Packages.Count);
        Assert.Contains(result.Packages, p => p.Name == "PkgA" && p.TargetFramework == "net8.0");
        Assert.Contains(result.Packages, p => p.Name == "PkgB" && p.TargetFramework == "net10.0");
    }

    [Fact]
    public void ParseJsonOutput_EmptyProjects_ReturnsEmptyList()
    {
        var json = """{ "version": 1, "projects": [] }""";
        var result = DotnetCliPackageResolver.ParseJsonOutput("test.csproj", json);
        Assert.Empty(result.Packages);
    }
}
