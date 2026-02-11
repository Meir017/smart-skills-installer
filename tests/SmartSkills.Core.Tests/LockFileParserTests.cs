using SmartSkills.Core.Scanning;

namespace SmartSkills.Core.Tests;

public class LockFileParserTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LockFileParser _parser = new();

    public LockFileParserTests()
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
    public void Parse_DirectAndTransitiveDeps_ReturnsBoth()
    {
        var lockFile = WriteFile("packages.lock.json", """
            {
              "version": 2,
              "dependencies": {
                "net8.0": {
                  "Newtonsoft.Json": {
                    "type": "Direct",
                    "requested": "[13.0.1, )",
                    "resolved": "13.0.1",
                    "contentHash": "abc123"
                  },
                  "Newtonsoft.Json.Bson": {
                    "type": "Transitive",
                    "resolved": "1.0.2",
                    "contentHash": "def456"
                  }
                }
              }
            }
            """);

        var result = _parser.Parse(lockFile);

        Assert.Equal(2, result.Count);
        var direct = result.Single(p => p.Name == "Newtonsoft.Json");
        Assert.Equal("13.0.1", direct.Version);
        Assert.False(direct.IsTransitive);

        var transitive = result.Single(p => p.Name == "Newtonsoft.Json.Bson");
        Assert.Equal("1.0.2", transitive.Version);
        Assert.True(transitive.IsTransitive);
    }

    [Fact]
    public void Parse_MultipleTargetFrameworks_DeduplicatesPreferringDirect()
    {
        var lockFile = WriteFile("packages.lock.json", """
            {
              "version": 2,
              "dependencies": {
                "net8.0": {
                  "PkgA": {
                    "type": "Transitive",
                    "resolved": "1.0.0"
                  },
                  "PkgB": {
                    "type": "Direct",
                    "resolved": "2.0.0"
                  }
                },
                "net9.0": {
                  "PkgA": {
                    "type": "Direct",
                    "resolved": "1.0.0"
                  },
                  "PkgB": {
                    "type": "Direct",
                    "resolved": "2.0.0"
                  }
                }
              }
            }
            """);

        var result = _parser.Parse(lockFile);

        Assert.Equal(2, result.Count);
        // PkgA should be marked as Direct (from net9.0 TFM which overrides transitive)
        var pkgA = result.Single(p => p.Name == "PkgA");
        Assert.False(pkgA.IsTransitive);
    }

    [Fact]
    public void Parse_EmptyDependencies_ReturnsEmpty()
    {
        var lockFile = WriteFile("packages.lock.json", """
            {
              "version": 2,
              "dependencies": {
                "net8.0": {}
              }
            }
            """);

        var result = _parser.Parse(lockFile);
        Assert.Empty(result);
    }
}
