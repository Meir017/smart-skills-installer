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

## Skill Registry

SmartSkills ships with a **built-in registry** of curated skills that is embedded in the core library. When your project references a matching NuGet package, the corresponding skill is automatically discovered — no configuration needed.

### Built-in Skills

The embedded registry includes skills from [microsoft/skills](https://github.com/microsoft/skills) and [redis/agent-skills](https://github.com/redis/agent-skills):

| NuGet Package(s) | Skill | Source |
|---|---|---|
| `Azure.Messaging.ServiceBus` | azure-servicebus-dotnet | microsoft/skills |
| `Azure.AI.OpenAI` | azure-ai-openai-dotnet | microsoft/skills |
| `Azure.Identity`, `Azure.Identity.*` | azure-identity-dotnet | microsoft/skills |
| `Azure.Messaging.EventGrid`, `Azure.Messaging.EventGrid.*` | azure-eventgrid-dotnet | microsoft/skills |
| `Azure.Messaging.EventHubs`, `Azure.Messaging.EventHubs.*` | azure-eventhub-dotnet | microsoft/skills |
| `Azure.Search.Documents` | azure-search-documents-dotnet | microsoft/skills |
| `Azure.Security.KeyVault.Keys` | azure-security-keyvault-keys-dotnet | microsoft/skills |
| `Azure.AI.DocumentIntelligence` | azure-ai-document-intelligence-dotnet | microsoft/skills |
| `Azure.AI.Projects`, `Azure.AI.Projects.*` | azure-ai-projects-dotnet | microsoft/skills |
| `Azure.AI.VoiceLive` | azure-ai-voicelive-dotnet | microsoft/skills |
| `Azure.Maps.Search`, `Azure.Maps.Routing`, `Azure.Maps.Rendering`, `Azure.Maps.Geolocation`, `Azure.Maps.TimeZones` | azure-maps-search-dotnet | microsoft/skills |
| `Azure.AI.Agents.Persistent` | azure-ai-agents-persistent-dotnet | microsoft/skills |
| `Microsoft.Agents.Builder`, `Microsoft.Agents.Hosting.AspNetCore`, `Microsoft.Agents.*` | m365-agents-dotnet | microsoft/skills |
| `Microsoft.Azure.WebJobs.Extensions.AuthenticationEvents` | microsoft-azure-webjobs-extensions-authentication-events-dotnet | microsoft/skills |
| `Azure.ResourceManager.AppContainers` | azure-mgmt-appcontainers-dotnet | microsoft/skills |
| `Azure.ResourceManager.AppService` | azure-mgmt-appservice-dotnet | microsoft/skills |
| `Azure.ResourceManager.Compute` | azure-mgmt-compute-dotnet | microsoft/skills |
| `Azure.ResourceManager.ContainerRegistry` | azure-mgmt-containerregistry-dotnet | microsoft/skills |
| `Azure.ResourceManager.ContainerService` | azure-mgmt-containerservice-dotnet | microsoft/skills |
| `Azure.ResourceManager.CosmosDB` | azure-mgmt-cosmosdb-dotnet | microsoft/skills |
| `Azure.ResourceManager.KeyVault` | azure-mgmt-keyvault-dotnet | microsoft/skills |
| `Azure.ResourceManager.Network` | azure-mgmt-network-dotnet | microsoft/skills |
| `Azure.ResourceManager.Playwright` | azure-mgmt-playwright-dotnet | microsoft/skills |
| `Azure.ResourceManager.Sql` | azure-resource-manager-sql-dotnet | microsoft/skills |
| `GitHub.Copilot.SDK` | copilot-sdk | microsoft/skills |
| `StackExchange.Redis`, `StackExchange.Redis.*`, `NRedisStack`, `Microsoft.Extensions.Caching.StackExchangeRedis`, `Redis.OM` | redis-development | redis/agent-skills |

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

A skill registry is a JSON file that maps NuGet packages to skill locations:

```json
{
  "repoUrl": "https://github.com/my-org/my-skills",
  "skills": [
    {
      "packagePatterns": ["Microsoft.EntityFrameworkCore", "Microsoft.EntityFrameworkCore.*"],
      "skillPath": "skills/ef-core"
    },
    {
      "packagePatterns": ["Serilog", "Serilog.*"],
      "skillPath": "skills/serilog"
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
| `skills[].packagePatterns` | Yes | Array of NuGet package names or glob patterns to match |
| `skills[].skillPath` | Yes | Path to the skill directory within the repository |
| `skills[].repoUrl` | No | Per-skill repository URL override (takes precedence over the top-level value) |

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
