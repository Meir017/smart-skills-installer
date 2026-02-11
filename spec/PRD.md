# Smart Skills Installer CLI

A C# toolset that inspects installed libraries in a .NET project and automatically installs relevant agent skills by fetching them from remote sources (public GitHub repositories or authenticated Azure DevOps URLs). The primary acquisition mechanism is MSBuild integration—skills are resolved and installed as part of the build pipeline via custom MSBuild targets/tasks. A companion CLI tool is provided for manual operations, diagnostics, and configuration.

## Goals

1. Automate the discovery and installation of agent skills based on project dependencies
2. Provide MSBuild-native integration so skills are acquired automatically during build
3. Support multiple remote skill sources (GitHub, Azure DevOps)
4. Provide a seamless developer experience with minimal configuration—just add a NuGet package
5. Ensure secure credential handling for authenticated sources
6. Support incremental builds so skill acquisition only runs when dependencies change

## Stories

### S01: Project Scaffolding & CLI Framework

Set up the .NET console application with a CLI framework for parsing commands, options, and arguments.

- **S01-T01**: Initialize .NET Console Project
  Create a .NET solution with a shared core class library and a console application project. The core library contains all scanning, matching, fetching, and installation logic so it can be consumed by both the CLI and the MSBuild task projects.
- **S01-T02**: Integrate CLI Parsing Library
  Add System.CommandLine NuGet package and configure root command with global options (--verbose, --config, --dry-run).
- **S01-T03**: Integrate Hosting Extensions
  Set up the application using the .NET Generic Host (Microsoft.Extensions.Hosting) for dependency injection, configuration, and logging infrastructure.

### S02: Library Detection & Scanning

Detect installed libraries/packages in the target project using an extensible package resolver abstraction. Rather than manually parsing `.csproj` files and reimplementing MSBuild's complex import/resolution logic (which involves `Directory.Build.props`, `Directory.Packages.props`, conditional `PackageReference` items, Central Package Management, and transitive dependencies), we delegate to purpose-built tooling that already handles all of this correctly.

- **S02-T01**: Implement Library Scanner SDK in SmartSkills.Core
  Build the core scanning API in SmartSkills.Core that accepts a project or solution (.sln/.slnx) path and returns a list of detected packages. This is the reusable SDK consumed by both the CLI and MSBuild tasks. Use `Microsoft.VisualStudio.SolutionPersistence` NuGet package for parsing solution files — it supports both .sln and .slnx formats via a unified `SolutionModel` API and is the same library used internally by the .NET SDK and Visual Studio.
- **S02-T02**: Define IPackageResolver Abstraction
  Define an `IPackageResolver` interface in SmartSkills.Core that abstracts package resolution. The interface accepts a project or solution path and returns a unified list of resolved packages (name, version, whether direct or transitive, target framework). This abstraction allows different resolution strategies to be plugged in without changing the scanning or matching logic. Register implementations via dependency injection so consumers can select or override the resolver.
- **S02-T03**: Implement DotnetCliPackageResolver
  Implement `IPackageResolver` by shelling out to `dotnet list package --include-transitive --format json`. This leverages the .NET SDK's own NuGet restore and resolution engine, which correctly handles all MSBuild evaluation complexity (`.props`/`.targets` imports, Central Package Management, conditional references, multi-targeting, etc.). Parse the JSON output (available since .NET SDK 7.0.200) to extract the full resolved dependency graph including both top-level and transitive packages with their resolved versions. Handle error cases such as missing SDK, restore failures, and malformed output gracefully.
- **S02-T04**: Implement 'scan' CLI Command
  Add a 'scan' subcommand to SmartSkills.Cli that wraps the Core scanning SDK, accepts an optional project/solution path (defaults to current directory), and displays results in a human-readable table with JSON output support.

### S03: Skill Registry & Mapping Configuration

Define and manage the mapping between library names and their corresponding agent skills. Skills follow the open [Agent Skills specification](https://agentskills.io/specification.md) — each skill is a directory containing a `SKILL.md` file with YAML frontmatter (`name`, `description`, and optional `license`, `compatibility`, `metadata`, `allowed-tools` fields) followed by Markdown instructions. Skills may also include optional `scripts/`, `references/`, and `assets/` subdirectories.

- **S03-T01**: Define Skill Manifest Schema
  Adopt the Agent Skills specification as the skill format. A skill is a directory whose name matches the `name` frontmatter field, containing at minimum a `SKILL.md` file. The YAML frontmatter provides the metadata (name, description, license, compatibility, metadata map, allowed-tools). No custom manifest schema is needed — the `SKILL.md` frontmatter *is* the manifest.
- **S03-T02**: Implement Skill Registry Index
  Create a registry index file format that maps NuGet package names/patterns to available skills. The index is hosted alongside skill directories in the remote source. Each entry maps one or more library names (or glob patterns) to a skill directory path in the repository. Example: `"Microsoft.EntityFrameworkCore*" → "ef-core-skill/"`.
- **S03-T03**: Implement Library-to-Skill Matching Engine
  Build the matching engine that takes a list of detected libraries (from `IPackageResolver`) and resolves them to applicable skills from the registry index. Support exact name matches, prefix/glob patterns, and priority ordering when multiple skills match.

### S04: Skill Source Provider Abstraction

Define an extensible `ISkillSourceProvider` abstraction in SmartSkills.Core that encapsulates all interaction with a remote skill repository. This abstraction allows the installation engine to work uniformly across different hosting backends (GitHub, Azure DevOps, local filesystem, etc.) without knowing the transport details.

The provider is responsible for:
- **Listing files** in a skill directory (given a skill path within the repository)
- **Downloading raw file content** for individual files (SKILL.md, scripts/*, references/*, assets/*)
- **Fetching the registry index** file from the repository root
- **Resolving the latest commit SHA** that touched a specific skill directory, enabling change detection without downloading content

- **S04-T01**: Define ISkillSourceProvider Interface
  Define the interface in SmartSkills.Core with methods: `GetRegistryIndexAsync(...)` to fetch and return the parsed registry index; `ListSkillFilesAsync(skillPath)` to enumerate all files in a skill directory tree; `DownloadFileAsync(filePath)` to download a single raw file; and `GetLatestCommitShaAsync(skillPath)` to return the SHA of the most recent commit affecting the given skill directory. All methods accept a `CancellationToken`. Register implementations via dependency injection.
- **S04-T02**: Implement Commit-SHA-Based Cache in Installation Engine
  Before downloading a skill, call `GetLatestCommitShaAsync` and compare the returned SHA against the SHA stored in the local state file from the previous install. If the SHA matches, skip the download entirely and report the skill as up-to-date. Store the SHA in the local state file alongside each installed skill entry. This avoids re-downloading unchanged skills and minimizes API calls.

### S05: GitHub Skill Source Provider

Implement `ISkillSourceProvider` for public (and optionally authenticated) GitHub repositories.

- **S05-T01**: Implement GitHub HTTP Client
  Create an HTTP client wrapper for GitHub REST API and raw content access (`raw.githubusercontent.com`), with rate limiting awareness, retry logic, and optional PAT authentication.
- **S05-T02**: Implement GitHubSkillSourceProvider
  Implement `ISkillSourceProvider` using the GitHub API: use the Git Trees API (`GET /repos/{owner}/{repo}/git/trees/{sha}?recursive=1`) to list files in a skill directory; use raw content URLs to download files; use the Commits API (`GET /repos/{owner}/{repo}/commits?path={skillPath}&per_page=1`) to resolve the latest commit SHA for a skill path. Also implement `GetRegistryIndexAsync` to fetch the registry index file from a configurable path in the repo.

### S06: Azure DevOps Skill Source Provider

Implement `ISkillSourceProvider` for authenticated Azure DevOps (ADO) Git repositories.

- **S06-T01**: Implement ADO Authentication
  Support multiple ADO authentication methods: Personal Access Token (PAT), Azure CLI credential, and Managed Identity.
- **S06-T02**: Implement AdoSkillSourceProvider
  Implement `ISkillSourceProvider` using the ADO Git REST API: use the Items API (`GET /items?scopePath={skillPath}&recursionLevel=Full`) to list files in a skill directory; use the Items API with `download=true` to fetch raw file content; use the Commits API (`GET /commits?searchCriteria.itemPath={skillPath}&$top=1`) to resolve the latest commit SHA for a skill path. Also implement `GetRegistryIndexAsync` to fetch the registry index from the repo.

### S07: Skill Installation Engine

Implement the core logic for installing, updating, and managing agent skills on the local system. A "skill" follows the [Agent Skills specification](https://agentskills.io/specification.md): a directory containing a `SKILL.md` file (with YAML frontmatter and Markdown instructions) and optional `scripts/`, `references/`, and `assets/` subdirectories. Installation means placing this directory structure on disk so that an agent can discover it.

The installation pipeline is:
1. **Resolve packages** — use `IPackageResolver` to get the project's dependency list
2. **Match skills** — query the registry index (fetched via `ISkillSourceProvider`) to find skills mapped to those packages
3. **Check cache** — for each matched skill, call `ISkillSourceProvider.GetLatestCommitShaAsync` and compare against the locally stored SHA; skip download if unchanged
4. **Download** — call `ISkillSourceProvider.ListSkillFilesAsync` then `DownloadFileAsync` for each file in the skill directory tree
5. **Parse & validate** — parse the `SKILL.md` frontmatter and validate against the Agent Skills spec constraints
6. **Write to disk** — place the skill directory under the configured skills output path, preserving the spec-defined structure
7. **Track state** — record installed skills, their source provider, commit SHA, and install timestamp

- **S07-T01**: Implement 'install' Command
  Add the 'install' subcommand that orchestrates the full pipeline: resolve packages → match skills → check cache → fetch via provider → validate → install. Report which skills were installed, skipped (already up-to-date), or failed.
- **S07-T02**: Implement SKILL.md Frontmatter Parser & Validator
  Parse YAML frontmatter from `SKILL.md` files and validate against the Agent Skills spec constraints: required `name` and `description` fields, name format rules, optional `license` (string), `compatibility` (≤500 chars), `metadata` (string→string map), and `allowed-tools` (space-delimited string). Reject skills that fail validation with clear error messages.
- **S07-T03**: Implement Local Skill Storage
  Define the local file system layout for installed skills. Each skill is stored as a directory matching its `name` field, containing the full directory tree from the remote source (`SKILL.md`, and optional `scripts/`, `references/`, `assets/`). Maintain a local state file tracking installed skills, their source provider, commit SHA, and install timestamp for cache validation and clean uninstallation.
- **S07-T04**: Implement Skill Update Detection
  Compare the locally cached commit SHA against the latest SHA from the provider. When the SHA differs (or `metadata.version` has changed), flag the skill as having an update available.
- **S07-T05**: Implement Skill Uninstallation
  Add the ability to remove installed skills cleanly by deleting the skill directory and removing its entry from the local state file.

### S08: Configuration Management

Implement user and project-level configuration for registry sources, credentials, and tool behavior.

- **S08-T01**: Implement Configuration File Support
  Support a JSON/YAML configuration file for persistent settings, with user-level and project-level overrides.
- **S08-T02**: Implement Multi-Source Registry Configuration
  Allow users to configure multiple skill registry sources (mix of GitHub and ADO) with priority ordering.
- **S08-T03**: Implement Credential Storage
  Provide secure storage for ADO tokens and GitHub PATs, avoiding plaintext storage in config files.

### S09: Error Handling, Resilience & Caching

Implement robust error handling, retry logic, and caching to ensure reliable operation.

- **S09-T01**: Implement Retry Policy with Exponential Backoff
  Add configurable retry logic for all HTTP operations with exponential backoff and jitter.
- **S09-T02**: Implement Local Cache
  Cache registry indexes and skill manifests locally to reduce network calls and enable offline browsing.
- **S09-T03**: Implement Structured Error Reporting
  Provide clear, actionable error messages with error codes and suggested fixes.

### S10: List & Status Commands

Provide commands to view installed skills, available skills, and overall status.

- **S10-T01**: Implement 'list' Command
  Add a command to list installed skills with their versions, sources, and install dates.
- **S10-T02**: Implement 'status' Command
  Add a command showing a summary of the current project context: detected libraries, matched skills, installed/missing skills.

### S11: Testing & Quality Assurance

Comprehensive testing strategy covering unit tests, integration tests, and end-to-end scenarios.

- **S11-T01**: Unit Tests for Core Logic
  Write unit tests for library detection, skill matching, manifest parsing, and configuration loading.
- **S11-T02**: Integration Tests for Remote Fetching
  Write integration tests that verify fetching from GitHub and ADO sources using recorded HTTP responses.
- **S11-T03**: End-to-End CLI Tests
  Write E2E tests that invoke the CLI as a subprocess and verify complete workflows.
- **S11-T04**: MSBuild Integration Tests
  Write tests that invoke MSBuild with sample projects referencing the SmartSkills.MSBuild package and verify skill acquisition during build.

### S12: Packaging & Distribution

Package the CLI tool for distribution as a .NET global tool and provide installation documentation.

- **S12-T01**: Package as .NET Global Tool
  Configure the CLI project for distribution as a .NET global tool via NuGet.
- **S12-T02**: Package MSBuild Targets as NuGet Package
  Package the SmartSkills.MSBuild project as a NuGet package that delivers .props and .targets files, automatically integrating skill acquisition into any consuming project's build.
- **S12-T03**: Create README and Usage Documentation
  Write comprehensive README.md with installation instructions, usage examples, and configuration reference for both the MSBuild package and CLI tool.

### S13: MSBuild Integration - Custom Tasks & Targets

Implement custom MSBuild tasks and targets that automatically acquire and install agent skills as part of the build pipeline. This is the primary skill acquisition mechanism—consumers simply add a PackageReference and skills are resolved at build time.

- **S13-T01**: Implement ResolveSmartSkills MSBuild Task
  Create a custom MSBuild task (extending Microsoft.Build.Utilities.Task) that reads the project's PackageReference items, queries the skill registry, and outputs a list of skills to install.
- **S13-T02**: Implement InstallSmartSkills MSBuild Task
  Create a custom MSBuild task that takes the resolved skills list and downloads/installs each skill to the configured output directory.
- **S13-T03**: Author .props and .targets Files
  Create the MSBuild .props and .targets files that wire the custom tasks into the build pipeline with correct ordering, default property values, and incremental build support.
- **S13-T04**: Implement MSBuild Diagnostic Logging & Build Output
  Provide clear build output showing skill acquisition progress, using MSBuild message importance levels appropriately.
- **S13-T05**: Handle MSBuild Task Assembly Loading & Dependencies
  Ensure the MSBuild task DLL and its dependencies (SmartSkills.Core, HTTP libraries, etc.) load correctly in the MSBuild process without conflicting with the host project's assemblies.
- **S13-T06**: Support NuGet Restore-Time vs Build-Time Skill Acquisition
  Determine and implement the correct MSBuild phase for skill acquisition, balancing between restore-time (earlier, but limited) and build-time (full access to resolved references).
- **S13-T07**: Support Multi-Targeting and Solution-Level Builds
  Ensure skill acquisition works correctly for multi-targeting projects and solution-level builds without duplicate acquisition.
