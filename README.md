# SmartSkills

Automatically discover and install agent skills based on your .NET project's dependencies. Available as both a CLI tool and MSBuild integration.

## Overview

SmartSkills scans your .NET project for installed NuGet packages and matches them against a remote skill registry to find relevant agent skills. Skills follow the [Agent Skills specification](https://agentskills.io/specification.md) and are downloaded, validated, and installed locally.

**Key features:**
- Scan projects and solutions for NuGet dependencies
- Match packages against skill registries using exact and glob patterns
- Fetch skills from GitHub and Azure DevOps repositories
- Commit-SHA-based caching to skip unchanged skills
- Configurable multi-source registries with priority ordering

## Installation

### CLI Tool

```bash
dotnet tool install -g SmartSkills.Cli
```

### MSBuild Integration

Add the NuGet package to your project:

```xml
<PackageReference Include="SmartSkills.MSBuild" Version="1.0.0" PrivateAssets="all" />
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

### Uninstall a Skill

```bash
smart-skills uninstall my-skill-name
```

### Global Options

| Option | Description |
|--------|-------------|
| `-v, --verbose` | Enable verbose logging |
| `--dry-run` | Preview changes without executing |

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
| `SmartSkillsOutputDirectory` | `$(MSBuildProjectDirectory)\.smartskills` | Local install directory |

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
  "skills": [
    {
      "packagePatterns": ["Microsoft.EntityFrameworkCore*"],
      "skillPath": "skills/ef-core"
    },
    {
      "packagePatterns": ["Serilog", "Serilog.*"],
      "skillPath": "skills/serilog"
    }
  ]
}
```

### Package Patterns

Patterns support exact match and glob syntax:
- `Microsoft.EntityFrameworkCore*` matches any EF Core package
- `Serilog` matches exactly `Serilog`
- `Azure.Storage.*` matches Azure Storage packages

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
