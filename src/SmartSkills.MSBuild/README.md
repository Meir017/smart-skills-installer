# SmartSkills.MSBuild

[![NuGet](https://img.shields.io/nuget/v/SmartSkills.MSBuild.svg)](https://www.nuget.org/packages/SmartSkills.MSBuild)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SmartSkills.MSBuild.svg)](https://www.nuget.org/packages/SmartSkills.MSBuild)

MSBuild integration for **SmartSkills** — automatically resolve and install agent skills during build based on your project's package references.

## Installation

```shell
dotnet add package SmartSkills.MSBuild
```

Since this is a build-only dependency, it should be marked with `PrivateAssets="all"` so it doesn't flow to consumers:

```xml
<PackageReference Include="SmartSkills.MSBuild" PrivateAssets="all" />
```

## How It Works

When you build your project, SmartSkills.MSBuild runs two targets automatically:

1. **ResolveSmartSkills** (before build) — scans your project's package references and matches them against the skill registry
2. **InstallSmartSkills** (after resolve) — downloads and installs matched skills to `.agents/skills`

Both targets are gated by the `SmartSkillsEnabled` property and are safe for multi-targeting projects (runs only once per build).

Skills are installed transitively — adding the package to any project in a dependency chain enables skill resolution.

## Configuration

| MSBuild Property | Default | Description |
|------------------|---------|-------------|
| `SmartSkillsEnabled` | `true` | Enable or disable skill acquisition |
| `SmartSkillsOutputDirectory` | `$(MSBuildProjectDirectory)\.agents\skills` | Directory where skills are installed |

### Disabling for Specific Configurations

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <SmartSkillsEnabled>false</SmartSkillsEnabled>
</PropertyGroup>
```

### Custom Output Directory

```xml
<PropertyGroup>
  <SmartSkillsOutputDirectory>$(SolutionDir).agents\skills</SmartSkillsOutputDirectory>
</PropertyGroup>
```

### Disabling via Command Line

```shell
dotnet build -p:SmartSkillsEnabled=false
```

## Example

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SmartSkills.MSBuild" PrivateAssets="all" />
    <PackageReference Include="Azure.Identity" />
    <PackageReference Include="StackExchange.Redis" />
  </ItemGroup>

</Project>
```

Building this project automatically installs the `azure-identity-dotnet` and `redis-development` skills into `.agents/skills/`.

## Further Reading

- [SmartSkills repository](https://github.com/meir017/smart-skills-installer) — full documentation, SDK, and CLI tool
- [Agent Skills specification](https://agentskills.io/specification.md)
