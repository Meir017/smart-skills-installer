# SmartSkills.Cli

[![NuGet](https://img.shields.io/nuget/v/SmartSkills.Cli.svg)](https://www.nuget.org/packages/SmartSkills.Cli)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SmartSkills.Cli.svg)](https://www.nuget.org/packages/SmartSkills.Cli)

A .NET global tool that automatically discovers and installs agent skills based on your project's dependencies.

## Installation

Install as a .NET global tool:

```shell
dotnet tool install -g SmartSkills.Cli
```

Or as a local tool in your repository:

```shell
dotnet new tool-manifest # if you don't have one yet
dotnet tool install SmartSkills.Cli
```

## Quick Start

```shell
# Scan your project and install matching skills
smart-skills install

# Preview what would be installed
smart-skills install --dry-run
```

## Commands

### `scan` — Scan for Dependencies

```shell
# Scan current directory (auto-detects .sln, .csproj, package.json, etc.)
smart-skills scan

# Scan a specific project
smart-skills scan --project ./src/MyProject/MyProject.csproj

# Recursive scan with JSON output
smart-skills scan --recursive --json
```

| Option | Description |
|--------|-------------|
| `-p, --project <path>` | Path to a project, solution, or directory |
| `--json` | Output results as JSON |
| `-r, --recursive` | Recursively scan subdirectories |
| `--depth <n>` | Maximum recursion depth (default: 5) |

### `install` — Install Skills

```shell
# Install skills based on detected packages
smart-skills install

# Install for a specific project
smart-skills install --project ./src/MyProject

# Force reinstall (overwrite locally modified skills)
smart-skills install --project ./src/MyProject --force
```

| Option | Description |
|--------|-------------|
| `-p, --project <path>` | Path to a project, solution, or directory |
| `-r, --recursive` | Recursively detect projects |
| `--depth <n>` | Maximum recursion depth (default: 5) |

### `restore` — Restore from Lock File

```shell
# Restore all skills to their exact locked versions
smart-skills restore

# Restore for a specific project
smart-skills restore --project ./src/MyProject
```

### `list` — List Installed Skills

```shell
smart-skills list
smart-skills list --json
```

### `status` — Check Skill Status

```shell
smart-skills status
smart-skills status --check-remote   # also check for upstream updates
smart-skills status --json
```

### `uninstall` — Remove a Skill

```shell
smart-skills uninstall my-skill-name
```

### Global Options

| Option | Description |
|--------|-------------|
| `-v, --verbose` | Enable verbose logging |
| `--dry-run` | Preview changes without executing |
| `--base-dir <path>` | Base directory for `.agents/skills` (defaults to current directory) |

## Lock File

SmartSkills generates a `smart-skills.lock.json` file that records the exact commit SHA and content hash of each installed skill. Commit this file to your repository so that `smart-skills restore` produces identical results for all team members and CI pipelines.

## Supported Ecosystems

| Ecosystem | Detected Files |
|-----------|---------------|
| .NET | `.csproj`, `.fsproj`, `.vbproj`, `.sln`, `.slnx` |
| Node.js | `package.json` (npm, yarn, pnpm, bun) |
| Python | `pyproject.toml`, `Pipfile.lock`, `requirements.txt`, `uv.lock` |
| Java | `pom.xml`, `build.gradle` |

## Further Reading

- [SmartSkills repository](https://github.com/meir017/smart-skills-installer) — full documentation, SDK, and MSBuild integration
- [Agent Skills specification](https://agentskills.io/specification.md)
