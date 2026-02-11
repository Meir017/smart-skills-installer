# Smart Skills Installer

Automatically discover and install agent skills based on your .NET project's dependencies. Available as both a CLI tool and MSBuild integration.

## Overview

Smart Skills Installer scans your .NET project for installed NuGet packages and matches them against a remote skill registry to find relevant agent skills. Skills are downloaded, verified, and installed locally.

**Key features:**
- Scan `.csproj` and `packages.lock.json` for dependencies
- Match libraries against skill registries using glob patterns
- Fetch skills from GitHub and Azure DevOps repositories
- SHA256 checksum verification for downloads
- Local caching with configurable TTL
- File-based configuration with user/project precedence

## Installation

### CLI Tool

```bash
dotnet tool install -g SmartSkills.Installer
```

### MSBuild Integration

Add the NuGet package to your project:

```xml
<PackageReference Include="SmartSkills.MSBuild" Version="0.1.0" PrivateAssets="all" />
```

## CLI Usage

### Scan for Dependencies

```bash
# Scan current directory
skills-installer scan

# Scan a specific path
skills-installer scan --path ./src/MyProject

# Output as JSON
skills-installer scan --path ./src/MyProject --output json
```

### Install Skills

```bash
# Install skills from a registry
skills-installer install --source github:org/skills-repo

# Force reinstall
skills-installer install --source github:org/skills-repo --force

# Skip confirmation
skills-installer install --source github:org/skills-repo --yes
```

### List Installed Skills

```bash
skills-installer list
skills-installer list --output json
```

### Update Skills

```bash
skills-installer update --source github:org/skills-repo
skills-installer update --source github:org/skills-repo --skill my-skill
```

### Uninstall Skills

```bash
skills-installer uninstall --skill my-skill
skills-installer uninstall --all
```

### Check Status

```bash
skills-installer status --path ./src/MyProject
```

### Global Options

| Option | Description |
|--------|-------------|
| `--verbose` | Enable verbose logging |
| `--config <path>` | Path to configuration file |
| `--dry-run` | Preview changes without executing |

## MSBuild Integration

### Basic Setup

Add the package reference and configure the registry source:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <SmartSkillsSources>github:your-org/skills-registry</SmartSkillsSources>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SmartSkills.MSBuild" Version="0.1.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Skills are automatically resolved and installed during build.

### Configuration Properties

| Property | Default | Description |
|----------|---------|-------------|
| `SmartSkillsEnabled` | `true` | Enable/disable skill acquisition |
| `SmartSkillsSources` | *(empty)* | Registry source (e.g., `github:org/repo`) |
| `SmartSkillsInstallDir` | `$(MSBuildProjectDirectory)\.smart-skills\` | Local install directory |
| `SmartSkillsCacheTtlMinutes` | `60` | Cache time-to-live in minutes |
| `SmartSkillsFailOnError` | `false` | Fail build on skill acquisition error |
| `SmartSkillsMaxParallelDownloads` | `4` | Max parallel downloads |
| `SmartSkillsVerbose` | `false` | Enable verbose MSBuild logging |
| `SmartSkillsAcquisitionPhase` | `build` | `build` or `restore` |

### Acquisition Phase

By default, skills are acquired during **build** (after `ResolvePackageAssets`). To acquire during **restore** instead:

```xml
<PropertyGroup>
  <SmartSkillsAcquisitionPhase>restore</SmartSkillsAcquisitionPhase>
</PropertyGroup>
```

### Multi-Targeting

For multi-targeted projects (`TargetFrameworks`), skill resolution runs only once using the first target framework, avoiding redundant work.

### Disabling for Specific Builds

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <SmartSkillsEnabled>false</SmartSkillsEnabled>
</PropertyGroup>
```

## Skill Registry Format

A skill registry is a JSON file hosted in a Git repository:

```json
{
  "registryVersion": "1.0",
  "lastUpdated": "2024-01-15",
  "sourceType": "github",
  "entries": [
    {
      "libraryPattern": "Microsoft.EntityFrameworkCore*",
      "skillManifestUrls": [
        "https://raw.githubusercontent.com/org/repo/main/skills/ef-core/manifest.json"
      ]
    },
    {
      "libraryPattern": "Serilog*",
      "skillManifestUrls": [
        "https://raw.githubusercontent.com/org/repo/main/skills/serilog/manifest.json"
      ]
    }
  ]
}
```

### Library Patterns

Patterns use glob syntax:
- `Microsoft.EntityFrameworkCore*` matches any EF Core package
- `Serilog` matches exactly `Serilog`
- `Azure.Storage.*` matches Azure Storage packages

## Configuration File

Create `.smart-skills.json` at the project or user level:

```json
{
  "sources": [
    {
      "type": "github",
      "location": "org/skills-registry",
      "branch": "main"
    },
    {
      "type": "ado",
      "location": "org/project/repo"
    }
  ],
  "cacheTtlMinutes": 60,
  "installDirectory": ".smart-skills"
}
```

**Precedence:** CLI flags > project config > user config (`~/.smart-skills/config.json`)

## Azure DevOps Support

For ADO-hosted registries, set authentication:

```bash
# Via environment variable
export SMART_SKILLS_ADO_PAT=your-pat-token

# Or Azure CLI (automatic)
az login
```

Registry source format: `ado:org/project/repo[@branch]`

## Error Codes

| Code | Description |
|------|-------------|
| SMSK001 | Registry fetch failed |
| SMSK002 | Skill download failed |
| SMSK003 | Checksum verification failed |
| SMSK004 | Manifest validation failed |
| SMSK005 | Configuration error |
| SMSK006 | Authentication failed |
| SMSK007 | Installation failed |
| SMSK008 | Network error |

## Project Structure

```
src/
  SmartSkills.Core/        # Shared library (scanning, matching, fetching)
  SmartSkills.Cli/         # CLI tool (System.CommandLine)
  SmartSkills.MSBuild/     # MSBuild tasks + props/targets
tests/
  SmartSkills.Core.Tests/  # Unit + integration tests
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
