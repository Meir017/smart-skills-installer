using SmartSkills.Core.Scanning;

namespace SmartSkills.Core.Tests;

public class CsprojParserTests : IDisposable
{
    private readonly string _tempDir;
    private readonly CsprojParser _parser = new();

    public CsprojParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SmartSkillsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string WriteFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    [Fact]
    public void Parse_MultiplePackageReferences_ReturnsAll()
    {
        var csproj = WriteFile("test.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
                <PackageReference Include="Serilog" Version="3.1.0" />
              </ItemGroup>
            </Project>
            """);

        var result = _parser.Parse(csproj);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Name == "Newtonsoft.Json" && p.Version == "13.0.1");
        Assert.Contains(result, p => p.Name == "Serilog" && p.Version == "3.1.0");
    }

    [Fact]
    public void Parse_VersionAsChildElement_ReturnsVersion()
    {
        var csproj = WriteFile("test.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json">
                  <Version>13.0.1</Version>
                </PackageReference>
              </ItemGroup>
            </Project>
            """);

        var result = _parser.Parse(csproj);

        Assert.Single(result);
        Assert.Equal("13.0.1", result[0].Version);
    }

    [Fact]
    public void Parse_VersionRangesAndWildcards_ReturnsAsIs()
    {
        var csproj = WriteFile("test.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="PkgA" Version="[1.0,2.0)" />
                <PackageReference Include="PkgB" Version="3.*" />
              </ItemGroup>
            </Project>
            """);

        var result = _parser.Parse(csproj);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Name == "PkgA" && p.Version == "[1.0,2.0)");
        Assert.Contains(result, p => p.Name == "PkgB" && p.Version == "3.*");
    }

    [Fact]
    public void Parse_CentralPackageManagement_ResolvesVersions()
    {
        WriteFile("Directory.Packages.props", """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
                <PackageVersion Include="Serilog" Version="3.1.0" />
              </ItemGroup>
            </Project>
            """);

        var csproj = WriteFile("src/test.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" />
                <PackageReference Include="Serilog" />
              </ItemGroup>
            </Project>
            """);

        var result = _parser.Parse(csproj);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Name == "Newtonsoft.Json" && p.Version == "13.0.3");
        Assert.Contains(result, p => p.Name == "Serilog" && p.Version == "3.1.0");
    }

    [Fact]
    public void Parse_NoPackageReferences_ReturnsEmpty()
    {
        var csproj = WriteFile("test.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var result = _parser.Parse(csproj);

        Assert.Empty(result);
    }

    [Fact]
    public void ParseSolution_MultipleProjects_ReturnsDeduplicated()
    {
        WriteFile("src/ProjA/ProjA.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
                <PackageReference Include="SharedPkg" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """);

        WriteFile("src/ProjB/ProjB.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Serilog" Version="3.1.0" />
                <PackageReference Include="SharedPkg" Version="2.0.0" />
              </ItemGroup>
            </Project>
            """);

        var slnContent = """
            Microsoft Visual Studio Solution File, Format Version 12.00
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ProjA", "src\ProjA\ProjA.csproj", "{00000000-0000-0000-0000-000000000001}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ProjB", "src\ProjB\ProjB.csproj", "{00000000-0000-0000-0000-000000000002}"
            EndProject
            """;

        var sln = WriteFile("Test.sln", slnContent);

        var result = _parser.ParseSolution(sln);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, p => p.Name == "Newtonsoft.Json");
        Assert.Contains(result, p => p.Name == "Serilog");
        Assert.Contains(result, p => p.Name == "SharedPkg");
    }
}