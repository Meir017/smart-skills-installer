# SmartSkills

[![CI](https://github.com/Meir017/smart-skills-installer/actions/workflows/ci.yml/badge.svg)](https://github.com/Meir017/smart-skills-installer/actions/workflows/ci.yml)

Automatically discover and install agent skills based on your project's dependencies. Supports .NET (NuGet), Node.js (npm/yarn/pnpm/bun), Python, and Java ecosystems. Available as a CLI tool, MSBuild integration, and core SDK.

## Overview

SmartSkills scans your project for installed packages and matches them against a remote skill registry to find relevant agent skills. Skills follow the [Agent Skills specification](https://agentskills.io/specification) and are downloaded, validated, and installed locally.

**Key features:**
- Scan projects and solutions for NuGet, npm, Python, and Java dependencies
- Auto-detect project type (.NET, Node.js, Python, Java) in a directory
- Match packages against skill registries using exact and glob patterns
- Fetch skills from GitHub and Azure DevOps repositories
- Commit-SHA-based caching to skip unchanged skills
- Configurable multi-source registries with priority ordering

## Packages

| Package | Description | Version |
|---------|-------------|---------|
| [SmartSkills.Core](https://www.nuget.org/packages/SmartSkills.Core) | Core SDK — scanning, matching, fetching, and installation | [![NuGet](https://img.shields.io/nuget/v/SmartSkills.Core.svg)](https://www.nuget.org/packages/SmartSkills.Core) |
| [SmartSkills.Cli](https://www.nuget.org/packages/SmartSkills.Cli) | .NET global/local tool | [![NuGet](https://img.shields.io/nuget/v/SmartSkills.Cli.svg)](https://www.nuget.org/packages/SmartSkills.Cli) |
| [SmartSkills.MSBuild](https://www.nuget.org/packages/SmartSkills.MSBuild) | Automatic skill acquisition during build | [![NuGet](https://img.shields.io/nuget/v/SmartSkills.MSBuild.svg)](https://www.nuget.org/packages/SmartSkills.MSBuild) |

## Installation

### CLI Tool

Install as a .NET global tool:

```shell
dotnet tool install -g SmartSkills.Cli
```

Or as a local tool in your repository:

```shell
dotnet new tool-manifest # if you don't have one yet
dotnet tool install SmartSkills.Cli
```

### MSBuild Integration

```shell
dotnet add package SmartSkills.MSBuild
```

Since this is a build-only dependency, mark it with `PrivateAssets="all"`:

```xml
<PackageReference Include="SmartSkills.MSBuild" PrivateAssets="all" />
```

### Core SDK

```shell
dotnet add package SmartSkills.Core
```

## CLI Usage

### Scan for Dependencies

```bash
# Scan current directory (auto-detects .sln or .csproj)
smart-skills scan

# Scan a specific project
smart-skills scan --project ./src/MyProject/MyProject.csproj

# Output as JSON
smart-skills scan --project ./src/MyProject --json
```

### Install Skills

```bash
# Install skills based on detected packages
smart-skills install

# Install for a specific project
smart-skills install --project ./src/MyProject

# Preview without installing
smart-skills install --dry-run
```

### List Installed Skills

```bash
smart-skills list
smart-skills list --json
```

### Check Status

```bash
smart-skills status
smart-skills status --project ./src/MyProject
```

### Restore Skills from Lock File

```bash
# Restore all skills to their exact locked versions
smart-skills restore

# Restore for a specific project
smart-skills restore --project ./src/MyProject
```

### Uninstall a Skill

```bash
smart-skills uninstall my-skill-name
```

### Global Options

| Option | Description |
|--------|-------------|
| `-v, --verbose` | Enable verbose logging |

## Skills Lock File

SmartSkills uses a **lock file** (`smart-skills.lock.json`) to track the exact state of installed skills. It serves as the single source of truth for reproducible installations — analogous to `packages.lock.json` (NuGet) or `yarn.lock` (npm).

### How It Works

When you run `smart-skills install`, the lock file records:
- **Remote URL** and **skill path** — where the skill was fetched from
- **Commit SHA** — the exact commit used for this version of the skill
- **Content hash** — a SHA256 hash of all installed files for local edit detection

On subsequent installs, SmartSkills compares the remote commit SHA and local content hash to determine whether a skill needs updating, is up-to-date, or has been locally modified.

### Example Lock File

```json
{
  "version": 1,
  "skills": {
    "azure-identity-dotnet": {
      "remoteUrl": "https://github.com/microsoft/skills",
      "skillPath": "skills/azure-identity-dotnet",
      "language": "dotnet",
      "commitSha": "abc123def456...",
      "localContentHash": "sha256:e3b0c44298fc1c14..."
    }
  }
}
```

### Workflows

| Command | Behavior |
|---------|----------|
| `smart-skills install` | Fetches latest skills, updates lock file with new commit SHAs and content hashes |
| `smart-skills install --dry-run` | Preview what would be installed without making changes |
| `smart-skills restore` | Downloads skills at the exact commit SHAs recorded in the lock file |
| `smart-skills status` | Shows which skills are up-to-date, modified, or missing |
| `smart-skills status --check-remote` | Also checks if newer versions are available upstream |
| `smart-skills uninstall <name>` | Removes the skill directory and its lock file entry |

### Version Control

The lock file should be **committed to your repository**. This ensures that all team members and CI pipelines restore the same skill versions. The file uses deterministic JSON formatting (sorted keys, consistent indentation) to produce clean diffs.

## MSBuild Integration

### Basic Setup

Add the package reference:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="SmartSkills.MSBuild" Version="1.0.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Skills are automatically resolved and installed during build.

### Configuration Properties

| Property | Default | Description |
|----------|---------|-------------|
| `SmartSkillsEnabled` | `true` | Enable/disable skill acquisition |
| `SmartSkillsOutputDirectory` | `$(MSBuildProjectDirectory)\.agents\skills` | Local install directory |

### Disabling for Specific Builds

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <SmartSkillsEnabled>false</SmartSkillsEnabled>
</PropertyGroup>
```

## Skill Registry

SmartSkills ships with a **built-in registry** of curated skills that is embedded in the core library. When your project references a matching NuGet package, the corresponding skill is automatically discovered — no configuration needed.

See **[skills-registry.md](skills-registry.md)** for the full list of built-in skills across all ecosystems (.NET, JavaScript/TypeScript, Python, Java).

### Adding Custom Registries

You can extend the built-in registry by providing additional JSON files. These are merged with the embedded registry at runtime — your custom entries are appended alongside the built-in ones.

#### Programmatic Usage

```csharp
using SmartSkills.Core.Registry;

// Load only the built-in embedded registry
var entries = RegistryIndexParser.LoadEmbedded();

// Merge built-in + your custom registries
var entries = RegistryIndexParser.LoadMerged(new[]
{
    "path/to/my-team-skills.json",
    "path/to/project-skills.json"
});

// Parse a standalone registry file
var entries = RegistryIndexParser.Parse(jsonString);
```

### Registry JSON Format

A skill registry is a JSON file that maps packages to skill locations:

```json
{
  "repoUrl": "https://github.com/my-org/my-skills",
  "skills": [
    {
      "packagePatterns": ["Microsoft.EntityFrameworkCore", "Microsoft.EntityFrameworkCore.*"],
      "skillPath": "skills/ef-core",
      "language": "dotnet"
    },
    {
      "packagePatterns": ["@azure/cosmos"],
      "skillPath": "skills/azure-cosmos-ts",
      "language": "javascript"
    },
    {
      "packagePatterns": ["SomePackage"],
      "skillPath": "skills/some-skill",
      "repoUrl": "https://github.com/other-org/other-repo"
    }
  ]
}
```

| Field | Required | Description |
|-------|----------|-------------|
| `repoUrl` (top-level) | No | Default repository URL inherited by all skills in the file |
| `language` (top-level) | No | Default ecosystem filter inherited by all skills (`"dotnet"`, `"javascript"`, or omit for any) |
| `skills[].packagePatterns` | Yes | Array of package names or glob patterns to match |
| `skills[].skillPath` | Yes | Path to the skill directory within the repository |
| `skills[].repoUrl` | No | Per-skill repository URL override (takes precedence over the top-level value) |
| `skills[].language` | No | Per-skill ecosystem filter override (takes precedence over the top-level value) |

### Package Patterns

Patterns support exact match and glob syntax:
- `Microsoft.EntityFrameworkCore` — matches exactly `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.*` — matches any package starting with `Microsoft.EntityFrameworkCore.`
- `Azure.Storage.*` — matches `Azure.Storage.Blobs`, `Azure.Storage.Queues`, etc.

## Authentication

- **GitHub**: Public repositories only — no authentication required.
- **Azure DevOps**: Uses `DefaultAzureCredential` from [Azure.Identity](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential). This automatically picks up credentials from Azure CLI (`az login`), environment variables, managed identity, etc.

## Error Codes

| Code | Description | Remediation |
|------|-------------|-------------|
| SS001 | Network error | Check internet connection and proxy settings |
| SS002 | Authentication failed | Run `az login` for ADO access |
| SS003 | .NET SDK not found | Install from https://dot.net/download |
| SS004 | Skill validation failed | Check SKILL.md frontmatter format |
| SS005 | Registry not found | Verify registry URL |
| SS006 | Skill not found | Check skill path in registry index |
| SS007 | Installation failed | Check file permissions and disk space |

## Project Structure

```
src/
  SmartSkills.Core/        # Shared library (scanning, matching, fetching, installation)
  SmartSkills.Cli/         # CLI tool (System.CommandLine)
  SmartSkills.MSBuild/     # MSBuild tasks + props/targets
tests/
  SmartSkills.Core.Tests/  # Unit tests
  SmartSkills.Cli.Tests/   # E2E CLI tests
  SmartSkills.MSBuild.Tests/ # MSBuild task tests
```

## Development

```bash
# Build
dotnet build

# Test
dotnet test

# Pack CLI tool
dotnet pack src/SmartSkills.Cli -o ./artifacts

# Pack MSBuild package
dotnet pack src/SmartSkills.MSBuild -o ./artifacts
```

## License

MIT
