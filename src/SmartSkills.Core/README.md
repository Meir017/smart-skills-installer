# SmartSkills.Core

[![NuGet](https://img.shields.io/nuget/v/SmartSkills.Core.svg)](https://www.nuget.org/packages/SmartSkills.Core)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SmartSkills.Core.svg)](https://www.nuget.org/packages/SmartSkills.Core)

Core SDK library for **SmartSkills** — scan project dependencies, match them against skill registries, and install agent skills automatically.

## Installation

```shell
dotnet add package SmartSkills.Core
```

## Getting Started

Register all SmartSkills services with a single extension method:

```csharp
using Microsoft.Extensions.DependencyInjection;
using SmartSkills.Core;

var services = new ServiceCollection();
services.AddSmartSkills();
```

This registers scanning, matching, installation, caching, and provider services as singletons.

## Key Services

| Interface | Description |
|-----------|-------------|
| `ILibraryScanner` | Scans projects, solutions, or directories for package references |
| `ISkillMatcher` | Matches resolved packages against skill registry entries using exact and glob patterns |
| `ISkillInstaller` | Orchestrates the full install/restore/uninstall workflow |
| `ISkillRegistry` | Loads and merges built-in and custom skill registries |
| `IProjectDetector` | Auto-detects project ecosystems (.NET, Node.js, Python, Java) in a directory |
| `ISkillLockFileStore` | Reads and writes the `smart-skills.lock.json` lock file |
| `ISkillSourceProviderFactory` | Creates GitHub or Azure DevOps providers from a repository URL |

## Scanning

```csharp
var scanner = serviceProvider.GetRequiredService<ILibraryScanner>();

// Scan a single project
var packages = await scanner.ScanProjectAsync("path/to/MyProject.csproj");

// Scan a solution
var packages = await scanner.ScanSolutionAsync("path/to/MySolution.sln");

// Auto-detect and scan all projects in a directory
var packages = await scanner.ScanDirectoryAsync("path/to/repo");

// Recursive scan with depth control
var packages = await scanner.ScanDirectoryAsync("path/to/repo", new ProjectDetectionOptions
{
    Recursive = true,
    MaxDepth = 3
});
```

### Supported Ecosystems

| Ecosystem | Project Files |
|-----------|--------------|
| .NET | `.csproj`, `.fsproj`, `.vbproj`, `.sln`, `.slnx` |
| Node.js | `package.json` (npm, yarn, pnpm, bun) |
| Python | `pyproject.toml`, `Pipfile.lock`, `requirements.txt`, `uv.lock` |
| Java | `pom.xml`, `build.gradle` |

## Registry & Matching

```csharp
using SmartSkills.Core.Registry;

// Load only the built-in embedded registry
var entries = RegistryIndexParser.LoadEmbedded();

// Merge built-in + custom registries
var entries = RegistryIndexParser.LoadMerged(new[]
{
    "path/to/my-team-skills.json",
    "path/to/project-skills.json"
});

// Match packages against registry
var matcher = serviceProvider.GetRequiredService<ISkillMatcher>();
var matched = matcher.Match(resolvedPackages, registryEntries);
```

Package patterns support exact match and glob syntax:
- `Azure.Identity` — matches exactly
- `Azure.Identity.*` — matches any package starting with `Azure.Identity.`

## Installation Workflow

```csharp
var installer = serviceProvider.GetRequiredService<ISkillInstaller>();

// Install skills for a project
var result = await installer.InstallAsync(new InstallOptions
{
    ProjectPath = "path/to/project",
    DryRun = false,
    Force = false
});

// result.Installed, result.Updated, result.SkippedUpToDate, result.Failed

// Restore skills from lock file
await installer.RestoreAsync("path/to/project");

// Remove a skill
await installer.UninstallAsync("skill-name", "path/to/project");
```

## Skill Providers

Skills are fetched from remote repositories. The provider is auto-detected from the URL:

| Provider | URL Pattern | Authentication |
|----------|-------------|----------------|
| GitHub | `https://github.com/{owner}/{repo}` | Public repos — none required |
| Azure DevOps | `https://dev.azure.com/{org}/{project}/_git/{repo}` | `DefaultAzureCredential` (Azure CLI, env vars, managed identity) |

## Configuration

| Class | Property | Default |
|-------|----------|---------|
| `ProjectDetectionOptions` | `Recursive` | `false` |
| `ProjectDetectionOptions` | `MaxDepth` | `5` |
| `InstallOptions` | `DryRun` | `false` |
| `InstallOptions` | `Force` | `false` |

## Further Reading

- [SmartSkills repository](https://github.com/meir017/smart-skills-installer) — full documentation, CLI tool, and MSBuild integration
- [Agent Skills specification](https://agentskills.io/specification.md)
