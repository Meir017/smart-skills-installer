# Multi-Language Support: Node.js (npm / yarn / pnpm)

Extend SmartSkills to detect and install agent skills for Node.js projects in addition to .NET. This involves refactoring the core interfaces to be language-agnostic, implementing Node.js package resolution (npm, yarn, pnpm), auto-detecting project types, and extending the registry format to include language metadata.

## Goals

1. Refactor core abstractions (`ResolvedPackage`, `RegistryEntry`, `ISkillMatcher`) to be language-agnostic, enabling future language additions with minimal changes
2. Auto-detect Node.js projects by the presence of `package.json` and lock files (`package-lock.json`, `yarn.lock`, `pnpm-lock.yaml`)
3. Resolve npm/yarn/pnpm dependencies (direct and transitive) from lock files
4. Extend the registry index format with an optional `language` field so skills can target specific ecosystems
5. Maintain full backward compatibility — existing .NET workflows must continue to work without changes
6. Support the same CLI commands (`scan`, `install`, `list`, `status`) for Node.js projects with no special flags

## Non-Goals

- Python (pip), Java (Maven/Gradle), or other languages — those can follow this pattern later
- Node.js-specific MSBuild integration — the MSBuild tasks remain .NET-only
- Monorepo/workspace support for npm/yarn/pnpm workspaces (can be a follow-up)
- Publishing a standalone npm package — SmartSkills remains a .NET tool that understands Node.js projects

## Current Architecture

The following components are **already language-agnostic** and need only minor updates:

| Component | File | Status |
|---|---|---|
| `ISkillSourceProvider` | `Providers/` | ✅ Fully language-agnostic |
| `ISkillStore` / `LocalSkillStore` | `Installation/` | ✅ Fully language-agnostic |
| `SkillMetadataParser` | `Installation/` | ✅ Fully language-agnostic |
| `RegistryIndexParser` | `Registry/RegistryIndexParser.cs` | ⚠️ Needs `language` field support |
| `SkillMatcher` | `Registry/SkillMatcher.cs` | ⚠️ Needs language-aware filtering |

The following components are **.NET-specific** and need refactoring or new implementations:

| Component | File | Change Needed |
|---|---|---|
| `ResolvedPackage` | `Scanning/Models.cs` | Add `Ecosystem` field |
| `IPackageResolver` | `Scanning/IPackageResolver.cs` | No change (already abstract) |
| `DotnetCliPackageResolver` | `Scanning/DotnetCliPackageResolver.cs` | No change (stays as-is) |
| `LibraryScanner` | `Scanning/LibraryScanner.cs` | Refactor to support multiple project types |
| `SkillInstaller` | `Installation/SkillInstaller.cs` | Replace hardcoded `.sln`/`.csproj` detection |
| `RegistryEntry` | `Registry/Models.cs` | Add optional `Language` field |

## Stories

### S01: Language Abstraction — Core Model Changes

Refactor the core models and interfaces to be language-agnostic without breaking existing .NET functionality. All changes must be additive — existing code that doesn't set the new fields should continue to work with `dotnet` as the implicit default.

- **S01-T01**: Add `Ecosystem` Field to `ResolvedPackage`
  Add an `Ecosystem` property (e.g. `"dotnet"`, `"npm"`) to the `ResolvedPackage` record in `Scanning/Models.cs`. This identifies which package manager resolved the package. Default to `"dotnet"` for backward compatibility with existing `DotnetCliPackageResolver` output.
  - **Requirements:**
    - Add `public string Ecosystem { get; init; } = "dotnet";` to `ResolvedPackage`
    - Define a static class `Ecosystems` with constants: `Dotnet = "dotnet"`, `Npm = "npm"`
    - Existing callers that don't set `Ecosystem` must continue to work
  - **Definition of Done:** `ResolvedPackage` has an `Ecosystem` field that defaults to `"dotnet"` and existing code compiles without changes.
  - **Verifications:**
    - All existing tests pass without modification
    - `dotnet build` succeeds

- **S01-T02**: Add `Language` Field to `RegistryEntry`
  Add an optional `Language` property to the `RegistryEntry` record in `Registry/Models.cs`. When set, a registry entry only matches packages from that ecosystem. When `null`, the entry matches any ecosystem (backward compatible).
  - **Requirements:**
    - Add `public string? Language { get; init; }` to `RegistryEntry`
    - Update `RegistryIndexParser.ParseDocument` to read the optional `"language"` JSON field from each skill entry and from the top-level object (as a default, similar to `repoUrl`)
    - Entries without `"language"` remain ecosystem-agnostic
  - **Definition of Done:** Registry JSON with `"language": "npm"` is parsed correctly; entries without language still parse and match as before.
  - **Verifications:**
    - Parsing `{"packagePatterns":["express"],"skillPath":"skills/express","language":"npm"}` produces a `RegistryEntry` with `Language == "npm"`
    - Existing `.NET` registry entries (no `language` field) still parse correctly
    - All existing tests pass

- **S01-T03**: Update `ISkillMatcher` for Language-Aware Matching
  Extend the `SkillMatcher` to filter registry entries by ecosystem when the entry has a `Language` set. If a `RegistryEntry.Language` is non-null, it should only match packages whose `ResolvedPackage.Ecosystem` matches. If `Language` is null, the entry matches packages from any ecosystem (current behavior).
  - **Requirements:**
    - Update `SkillMatcher.Match` to compare `entry.Language` against `package.Ecosystem` when both are set
    - Entries with `Language == null` match any package ecosystem (backward compatible)
    - Case-insensitive comparison for language matching
  - **Definition of Done:** A registry entry with `"language": "npm"` does not match NuGet packages, and vice versa. Entries without a language still match everything.
  - **Verifications:**
    - Unit test: npm registry entry does not match .NET packages
    - Unit test: .NET registry entry does not match npm packages
    - Unit test: language-less entry matches both .NET and npm packages
    - All existing matcher tests pass

### S02: Project Type Detection

Implement automatic project type detection so the CLI and installer can determine whether a directory contains a .NET project, a Node.js project, or both — without requiring the user to specify.

- **S02-T01**: Implement `IProjectDetector` Interface
  Define an `IProjectDetector` abstraction in `SmartSkills.Core.Scanning` that inspects a directory path and returns one or more detected project descriptors, each identifying the ecosystem and the relevant project file path.
  - **Requirements:**
    - Define `record DetectedProject(string Ecosystem, string ProjectFilePath)`
    - Define `interface IProjectDetector { IReadOnlyList<DetectedProject> Detect(string directoryPath); }`
    - If a directory contains both `MyApp.sln` and `package.json`, both should be returned
  - **Definition of Done:** Interface and model are defined in `SmartSkills.Core.Scanning`.
  - **Verifications:**
    - Code compiles
    - Interface is registered in the DI container

- **S02-T02**: Implement `ProjectDetector`
  Implement `IProjectDetector` with detection logic for .NET and Node.js ecosystems. Detection should work on a directory by looking for known project markers.
  - **Requirements:**
    - Detect .NET projects: look for `*.sln`, `*.slnx`, `*.csproj`, `*.fsproj`, `*.vbproj` (prefer `.sln`/`.slnx` when present)
    - Detect Node.js projects: look for `package.json` (only if it's a real project, not inside `node_modules/`)
    - Return `DetectedProject` for each found ecosystem with the path to the primary project file
    - Register in DI
  - **Definition of Done:** `ProjectDetector` correctly identifies .NET and Node.js projects in a directory.
  - **Verifications:**
    - Unit test: directory with only `.csproj` → `[DetectedProject("dotnet", "path/to.csproj")]`
    - Unit test: directory with only `package.json` → `[DetectedProject("npm", "path/package.json")]`
    - Unit test: directory with both `.sln` and `package.json` → two results
    - Unit test: empty directory → empty list

- **S02-T03**: Integrate Project Detection into `SkillInstaller`
  Replace the hardcoded `.sln`/`.csproj` detection in `SkillInstaller.InstallAsync` (lines 47–56) with the `IProjectDetector`. The installer should iterate over all detected projects and aggregate packages across ecosystems.
  - **Requirements:**
    - Inject `IProjectDetector` into `SkillInstaller`
    - When `options.ProjectPath` points to a directory, use `IProjectDetector.Detect()` to find projects
    - When it points to a specific file (`.sln`, `.csproj`, `package.json`), infer the ecosystem from the file extension
    - Route each detected project to the appropriate `IPackageResolver` via a resolver factory (see S03-T04)
    - Aggregate all `ResolvedPackage` results before matching against the registry
  - **Definition of Done:** `SkillInstaller` uses project detection instead of hardcoded extension checks, and can handle Node.js projects alongside .NET.
  - **Verifications:**
    - Existing .NET install flow works identically
    - A directory with `package.json` triggers Node.js package resolution
    - A directory with both `.sln` and `package.json` resolves packages from both ecosystems

### S03: Node.js Package Resolution

Implement `IPackageResolver` for the Node.js ecosystem by parsing lock files to extract the full dependency tree.

- **S03-T01**: Implement `NpmPackageResolver`
  Implement `IPackageResolver` that resolves npm packages by parsing `package.json` for direct dependencies and `package-lock.json` for the full resolved dependency graph. Sets `Ecosystem = "npm"` on all returned `ResolvedPackage` entries.
  - **Requirements:**
    - Parse `package.json` to identify direct dependencies (`dependencies`, `devDependencies`) — these are `IsTransitive = false`
    - Parse `package-lock.json` (lockfile version 2 or 3) to extract the full resolved dependency graph — non-direct deps are `IsTransitive = true`
    - If `package-lock.json` is missing, fall back to only listing direct deps from `package.json`
    - Set `ResolvedPackage.Version` to the resolved version from the lock file
    - Set `ResolvedPackage.Ecosystem` to `Ecosystems.Npm`
    - Handle scoped packages (e.g. `@azure/identity`) correctly
  - **Definition of Done:** `NpmPackageResolver` returns resolved packages from npm lock files with correct direct/transitive classification.
  - **Verifications:**
    - Unit test with sample `package.json` + `package-lock.json`: correct packages and versions returned
    - Unit test: scoped packages (`@scope/name`) are handled correctly
    - Unit test: missing lock file → only direct deps returned
    - Unit test: devDependencies are included and marked correctly

- **S03-T02**: Implement `YarnPackageResolver`
  Implement `IPackageResolver` that resolves Yarn packages by parsing `yarn.lock` (Yarn Classic v1 and Yarn Berry v2+ formats).
  - **Requirements:**
    - Parse `yarn.lock` to extract resolved package names and versions
    - Determine direct vs transitive by cross-referencing with `package.json`
    - Support both Yarn Classic format (custom text format) and Yarn Berry format (YAML-based)
    - Set `ResolvedPackage.Ecosystem` to `Ecosystems.Npm` (packages are from the npm registry regardless of client)
    - If `yarn.lock` is missing, fall back to direct deps from `package.json`
  - **Definition of Done:** `YarnPackageResolver` correctly parses both Yarn lock file formats.
  - **Verifications:**
    - Unit test: Yarn Classic `yarn.lock` → correct packages
    - Unit test: Yarn Berry `yarn.lock` → correct packages
    - Unit test: direct vs transitive classification matches `package.json`

- **S03-T03**: Implement `PnpmPackageResolver`
  Implement `IPackageResolver` that resolves pnpm packages by parsing `pnpm-lock.yaml`.
  - **Requirements:**
    - Parse `pnpm-lock.yaml` to extract resolved package names and versions
    - Determine direct vs transitive by cross-referencing with `package.json`
    - Set `ResolvedPackage.Ecosystem` to `Ecosystems.Npm`
    - If `pnpm-lock.yaml` is missing, fall back to direct deps from `package.json`
  - **Definition of Done:** `PnpmPackageResolver` correctly parses pnpm lock files.
  - **Verifications:**
    - Unit test with sample `pnpm-lock.yaml`: correct packages and versions
    - Unit test: direct vs transitive classification is correct

- **S03-T04**: Implement `IPackageResolverFactory`
  Create a factory that selects the appropriate `IPackageResolver` based on the detected ecosystem and available lock files.
  - **Requirements:**
    - Define `interface IPackageResolverFactory { IPackageResolver GetResolver(DetectedProject project); }`
    - For `Ecosystem == "dotnet"`: return the existing `DotnetCliPackageResolver`
    - For `Ecosystem == "npm"`: inspect the project directory for lock files and return the matching resolver:
      - `pnpm-lock.yaml` present → `PnpmPackageResolver`
      - `yarn.lock` present → `YarnPackageResolver`
      - Otherwise → `NpmPackageResolver` (handles both with and without `package-lock.json`)
    - Register in DI
  - **Definition of Done:** Factory correctly routes to the right resolver based on ecosystem and lock file presence.
  - **Verifications:**
    - Unit test: dotnet project → `DotnetCliPackageResolver`
    - Unit test: npm project with `yarn.lock` → `YarnPackageResolver`
    - Unit test: npm project with `pnpm-lock.yaml` → `PnpmPackageResolver`
    - Unit test: npm project with `package-lock.json` → `NpmPackageResolver`
    - Unit test: npm project with no lock file → `NpmPackageResolver`

### S04: Extend LibraryScanner for Multi-Language

Refactor `LibraryScanner` to work with multiple project types rather than being hardcoded to .NET solutions and projects.

- **S04-T01**: Refactor `LibraryScanner` for Ecosystem Routing
  Update `LibraryScanner` to use `IPackageResolverFactory` instead of a single `IPackageResolver`, and support scanning Node.js projects alongside .NET.
  - **Requirements:**
    - Replace constructor dependency on `IPackageResolver` with `IPackageResolverFactory`
    - `ScanProjectAsync` should accept any project file path (`.csproj`, `package.json`, etc.) and route to the correct resolver via the factory
    - `ScanSolutionAsync` remains .NET-specific (solutions are a .NET concept) but now explicitly lives alongside a new generic scan path
    - Add a new method: `Task<IReadOnlyList<ProjectPackages>> ScanDirectoryAsync(string directoryPath, CancellationToken)` that uses `IProjectDetector` to discover all projects in a directory and scan each
  - **Definition of Done:** `LibraryScanner` can scan both .NET and Node.js projects.
  - **Verifications:**
    - Existing .NET scanning works identically
    - `ScanProjectAsync("path/package.json")` returns npm packages
    - `ScanDirectoryAsync` on a mixed directory returns packages from both ecosystems

- **S04-T02**: Update `ILibraryScanner` Interface
  Add the `ScanDirectoryAsync` method to the `ILibraryScanner` interface.
  - **Requirements:**
    - Add `Task<IReadOnlyList<ProjectPackages>> ScanDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);`
    - Keep existing `ScanProjectAsync` and `ScanSolutionAsync` methods
  - **Definition of Done:** Interface updated, implementation provided in `LibraryScanner`.
  - **Verifications:**
    - `dotnet build` succeeds
    - All callers updated

### S05: Node.js Skill Registry Entries

Populate the embedded registry with Node.js/npm skill entries so that Node.js projects get skills automatically, just like .NET projects do today.

- **S05-T01**: Create Node.js Embedded Registry
  Add Node.js skill entries to the embedded `skills-registry.json` (or a separate `skills-registry-npm.json` that is merged at load time).
  - **Requirements:**
    - Add entries for npm packages that have corresponding skills in [microsoft/skills](https://github.com/microsoft/skills) and [redis/agent-skills](https://github.com/redis/agent-skills), such as:
      - `@azure/identity` → `azure-identity-js`
      - `@azure/service-bus` → `azure-servicebus-js`
      - `@azure/openai` → `azure-ai-openai-js`
      - `@azure/search-documents` → `azure-search-documents-js`
      - `redis`, `ioredis` → `redis-development`
    - Each entry must include `"language": "npm"`
    - Verify that the skill paths exist in the referenced repositories
  - **Definition of Done:** Embedded registry includes Node.js skill entries that are loaded at startup.
  - **Verifications:**
    - `RegistryIndexParser.LoadEmbedded()` returns entries with `Language == "npm"`
    - The npm entries don't break existing .NET entry loading

- **S05-T02**: Update Registry JSON Format Documentation
  Update the `README.md` registry JSON format section to document the new optional `language` field.
  - **Requirements:**
    - Add `language` to the registry JSON format table
    - Show an example with `"language": "npm"`
    - Explain that `language` is optional and defaults to matching any ecosystem
  - **Definition of Done:** README documents the `language` field.
  - **Verifications:**
    - README includes `language` field documentation

### S06: CLI Updates for Multi-Language

Update the CLI commands to leverage multi-language scanning seamlessly.

- **S06-T01**: Update `scan` Command for Auto-Detection
  The `scan` command should auto-detect all project types in the target directory and display results grouped by ecosystem.
  - **Requirements:**
    - When no `--project` is specified, scan the current directory for all supported project types
    - When `--project` points to a `package.json`, scan it as a Node.js project
    - Display results with an `Ecosystem` column in human-readable output
    - JSON output should include the `ecosystem` field on each package
  - **Definition of Done:** `smart-skills scan` in a Node.js project directory shows npm packages.
  - **Verifications:**
    - `smart-skills scan` in a dir with `package.json` → lists npm packages
    - `smart-skills scan` in a dir with both `.sln` and `package.json` → lists both
    - `smart-skills scan --json` includes `ecosystem` field

- **S06-T02**: Update `install` Command
  The `install` command should work with Node.js projects using the same interface — no new flags needed.
  - **Requirements:**
    - Auto-detect project type and resolve packages accordingly
    - Match against language-filtered registry entries
    - Install skills to the same `.agents/skills/` directory structure
    - `--dry-run` works for Node.js projects
  - **Definition of Done:** `smart-skills install` in a Node.js project installs matching skills.
  - **Verifications:**
    - `smart-skills install` in a Node.js dir installs npm-matched skills
    - `smart-skills install --dry-run` previews correctly
    - Skills installed from npm matches coexist with .NET skills

### S07: Testing

Comprehensive tests for all new multi-language functionality.

- **S07-T01**: Unit Tests for Model Changes
  Test the `Ecosystem` field on `ResolvedPackage` and `Language` field on `RegistryEntry`.
  - **Requirements:**
    - Test default ecosystem is `"dotnet"`
    - Test setting ecosystem to `"npm"`
    - Test `RegistryEntry` with and without `Language`
  - **Definition of Done:** Core model changes have full unit test coverage.
  - **Verifications:**
    - `dotnet test --filter "FullyQualifiedName~ModelTests"` passes

- **S07-T02**: Unit Tests for Language-Aware Matching
  Test `SkillMatcher` with mixed-ecosystem packages and registry entries.
  - **Requirements:**
    - Test: npm entry matches npm package but not dotnet package
    - Test: dotnet entry matches dotnet package but not npm package
    - Test: language-null entry matches both
    - Test: mixed packages + mixed entries → correct cross-product
  - **Definition of Done:** Matcher tests cover all language-filtering scenarios.
  - **Verifications:**
    - `dotnet test --filter "FullyQualifiedName~SkillMatcherTests"` passes

- **S07-T03**: Unit Tests for Node.js Package Resolvers
  Test each resolver with sample lock files checked into `TestData/`.
  - **Requirements:**
    - Test `NpmPackageResolver` with sample `package.json` + `package-lock.json`
    - Test `YarnPackageResolver` with sample `yarn.lock` (both v1 and v2 formats)
    - Test `PnpmPackageResolver` with sample `pnpm-lock.yaml`
    - Test fallback behavior when lock files are missing
    - Test scoped packages, optional dependencies, devDependencies
    - Place sample files in `tests/SmartSkills.Core.Tests/TestData/NodeJs/`
  - **Definition of Done:** All three resolvers have tests with real sample lock files.
  - **Verifications:**
    - `dotnet test --filter "FullyQualifiedName~PackageResolver"` passes

- **S07-T04**: Unit Tests for Project Detection
  Test `ProjectDetector` with various directory layouts.
  - **Requirements:**
    - Test: .NET-only directory
    - Test: Node.js-only directory
    - Test: mixed directory
    - Test: empty directory
    - Test: nested `node_modules/package.json` is NOT detected as a project
  - **Definition of Done:** Project detection has full test coverage.
  - **Verifications:**
    - `dotnet test --filter "FullyQualifiedName~ProjectDetector"` passes

- **S07-T05**: Integration / E2E Tests
  End-to-end CLI tests with Node.js projects.
  - **Requirements:**
    - Create sample Node.js project fixtures in test data
    - Test `scan` command against a Node.js project
    - Test `install` command against a Node.js project with matching registry entries
  - **Definition of Done:** E2E tests verify the full Node.js workflow via the CLI.
  - **Verifications:**
    - `dotnet test --filter "FullyQualifiedName~Cli.Tests"` passes

## API Surface

See companion file `api.cs` for the updated public API sketch showing new and changed types.

## Registry JSON Format (Updated)

```json
{
  "repoUrl": "https://github.com/microsoft/skills",
  "language": "npm",
  "skills": [
    {
      "packagePatterns": ["@azure/identity", "@azure/identity-*"],
      "skillPath": ".github/skills/azure-identity-js"
    },
    {
      "packagePatterns": ["express"],
      "skillPath": "skills/express",
      "language": "npm"
    },
    {
      "packagePatterns": ["SomePackage"],
      "skillPath": "skills/generic-skill",
      "language": null
    }
  ]
}
```

| Field | Required | Description |
|-------|----------|-------------|
| `repoUrl` (top-level) | No | Default repository URL inherited by all skills |
| `language` (top-level) | No | Default ecosystem filter inherited by all skills (`"dotnet"`, `"npm"`, or `null` for any) |
| `skills[].packagePatterns` | Yes | Array of package names or glob patterns to match |
| `skills[].skillPath` | Yes | Path to the skill directory within the repository |
| `skills[].repoUrl` | No | Per-skill repository URL override |
| `skills[].language` | No | Per-skill ecosystem filter override (takes precedence over top-level) |

## Dependency Changes

| Package | Purpose | Project |
|---------|---------|---------|
| `YamlDotNet` | Parse `pnpm-lock.yaml` and Yarn Berry lock files | `SmartSkills.Core` |

No additional NuGet packages are needed for npm/package-lock.json parsing (JSON via `System.Text.Json`). Yarn Classic lock file parsing requires a custom parser (simple line-oriented format).
