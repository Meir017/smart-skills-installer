# Recursive Project Scanning

Extend the `IProjectDetector` and directory scanning pipeline to recursively discover projects in nested directories, intelligently skipping well-known non-project directories (e.g., `node_modules`, `bin`, `obj`, `.git`). Today, `ProjectDetector.Detect()` only inspects the top-level directory it's given. This means a monorepo or workspace layout — where multiple projects live in subdirectories — requires the user to point the CLI at each project individually. Recursive scanning removes that friction.

## Goals

1. Recursively discover all project types (`.sln`, `.csproj`, `package.json`, `pyproject.toml`, `pom.xml`, `build.gradle`, etc.) in nested directories from a given root
2. Skip well-known non-project directories to avoid wasted I/O and false positives
3. Provide a configurable maximum depth to prevent unbounded traversal in deeply nested file systems
4. Maintain backward compatibility — existing single-directory detection continues to work unchanged
5. Integrate recursive scanning into the CLI `scan` and `install` commands via a `--recursive` flag

## Non-Goals

- Workspace/monorepo-aware resolution (e.g., npm workspaces, .NET `Directory.Build.props` inheritance) — that is a separate feature
- Parallelizing the directory walk — sequential traversal is sufficient for typical repository sizes
- Watching for file system changes — this is a one-shot scan
- Custom user-defined exclude patterns (can be a follow-up)

## Current Architecture

| Component | File | Current Behavior |
|---|---|---|
| `IProjectDetector` | `Scanning/IProjectDetector.cs` | `Detect(directoryPath)` — single-directory, no recursion |
| `ProjectDetector` | `Scanning/ProjectDetector.cs` | Checks top-level files only. Excludes `node_modules` for Node.js, `venv`/`.venv`/`__pycache__`/`.tox` for Python. |
| `LibraryScanner` | `Scanning/LibraryScanner.cs` | `ScanDirectoryAsync` calls `_projectDetector.Detect()` on the given directory only |
| `SkillInstaller` | `Installation/SkillInstaller.cs` | Routes directory paths to `ScanDirectoryAsync` — no recursive option |
| CLI `scan` command | `Cli/Program.cs` | Accepts `--project` path; defaults to current directory; no `--recursive` flag |

## Directories to Exclude

The following directories should be skipped during recursive traversal. They are grouped by ecosystem but applied globally (a `.git` directory is never a project root regardless of ecosystem).

### Universal Excludes

| Directory | Reason |
|-----------|--------|
| `.git` | Version control internals |
| `.hg` | Mercurial version control |
| `.svn` | Subversion version control |
| `.vs` | Visual Studio local settings |
| `.vscode` | VS Code local settings |
| `.idea` | JetBrains IDE settings |

### .NET Excludes

| Directory | Reason |
|-----------|--------|
| `bin` | Build output |
| `obj` | Intermediate build output |

### Node.js Excludes

| Directory | Reason |
|-----------|--------|
| `node_modules` | Installed npm packages (can be deeply nested) |
| `.next` | Next.js build output |
| `.nuxt` | Nuxt.js build output |
| `bower_components` | Legacy Bower packages |

### Python Excludes

| Directory | Reason |
|-----------|--------|
| `venv` | Virtual environment |
| `.venv` | Virtual environment (dotfile variant) |
| `__pycache__` | Compiled bytecode cache |
| `.tox` | Tox testing environments |
| `.mypy_cache` | mypy type-checking cache |
| `.pytest_cache` | pytest cache |
| `site-packages` | Installed Python packages |

### Java Excludes

| Directory | Reason |
|-----------|--------|
| `target` | Maven build output |
| `.gradle` | Gradle cache/metadata |
| `build` | Gradle build output |

### General Build Output

| Directory | Reason |
|-----------|--------|
| `dist` | Common distribution/build output directory |
| `vendor` | Vendored dependencies (Go, PHP, Ruby) |
| `coverage` | Test coverage reports |
| `.cache` | Generic cache directory |

## Traversal Strategy

The recursive scanner uses **breadth-first traversal with early pruning**:

1. **Detect first, then recurse** — For each directory, run the existing `ProjectDetector.Detect()` logic. If a project is found, record it. Then decide whether to recurse into subdirectories.
2. **Prune on detection** — When a project marker is found in a directory, **do not recurse deeper** into that directory's subtree for the _same ecosystem_. For example, if `package.json` is found at `apps/web/`, don't look for another `package.json` inside `apps/web/src/`. However, a sibling directory `apps/api/` should still be scanned. Different ecosystems in the same directory are still recursed independently.
3. **Exclude directories globally** — Skip all directories in the exclude list regardless of depth. This is checked before any file I/O in the directory.
4. **Depth limiting** — Accept a configurable `maxDepth` parameter (default: 5). Depth 0 means "current directory only" (existing behavior). Depth 1 means "current directory + immediate children", etc.
5. **Solution aggregation** — When a `.sln`/`.slnx` file is found, do not separately detect the `.csproj` files it references. The solution already enumerates its projects.

## Stories

### S01: Recursive Detection in ProjectDetector

Extend `IProjectDetector` and `ProjectDetector` to support recursive directory traversal with smart exclusions.

- **S01-T01**: Define `ProjectDetectionOptions` Record
  Create a configuration record that controls recursive scanning behavior.
  - **Requirements:**
    - Define `record ProjectDetectionOptions` with properties:
      - `bool Recursive` (default: `false`)
      - `int MaxDepth` (default: `5`)
    - Place in `SmartSkills.Core.Scanning` namespace
  - **Definition of Done:** Record is defined and compiles.
  - **Verifications:**
    - `dotnet build` succeeds

- **S01-T02**: Add `Detect` Overload with Options
  Add an overload of `IProjectDetector.Detect` that accepts `ProjectDetectionOptions`.
  - **Requirements:**
    - Add `IReadOnlyList<DetectedProject> Detect(string directoryPath, ProjectDetectionOptions options)` to `IProjectDetector`
    - The existing `Detect(string directoryPath)` remains as-is and delegates to the new overload with default options (`Recursive = false`)
    - Backward compatible — no breaking changes to existing callers
  - **Definition of Done:** Interface has both overloads; existing callers compile unchanged.
  - **Verifications:**
    - `dotnet build` succeeds
    - All existing tests pass

- **S01-T03**: Implement Recursive Traversal in ProjectDetector
  Implement the recursive directory walking logic with exclusion and pruning.
  - **Requirements:**
    - Implement the new `Detect(directoryPath, options)` overload
    - When `options.Recursive` is `false`, behave exactly as today (call existing per-ecosystem detection methods)
    - When `options.Recursive` is `true`:
      - Enumerate subdirectories, skipping any in the global exclude set (case-insensitive name match)
      - For each non-excluded subdirectory, recursively call `Detect` up to `options.MaxDepth` levels deep
      - Apply the "prune on detection" strategy: when a project marker is found for an ecosystem, do not recurse deeper into that subtree for the same ecosystem
    - Track visited directories to avoid cycles (e.g., symlinks)
    - Log skipped directories at `Debug` level
  - **Definition of Done:** `ProjectDetector.Detect(path, new ProjectDetectionOptions { Recursive = true })` discovers projects in nested directories while skipping excluded dirs.
  - **Verifications:**
    - Unit test: monorepo layout with projects at different depths — all discovered
    - Unit test: `node_modules` subtrees are skipped
    - Unit test: `.git` directory is skipped
    - Unit test: depth limit is respected (projects beyond `MaxDepth` are not found)
    - Unit test: symlink cycles do not cause infinite recursion
    - All existing non-recursive tests pass unchanged

- **S01-T04**: Consolidate Exclusion List
  Extract the exclusion list into a shared, well-documented constant so it can be reused and tested independently.
  - **Requirements:**
    - Create a static class `ExcludedDirectories` in `SmartSkills.Core.Scanning` with:
      - `IReadOnlySet<string> All` — the combined set of all directory names to exclude (case-insensitive)
      - Individual category sets for discoverability: `Universal`, `DotNet`, `NodeJs`, `Python`, `Java`, `BuildOutput`
    - Update `ProjectDetector` to use `ExcludedDirectories.All` instead of inline arrays
    - Remove the existing `PythonExcludedDirs` array from `ProjectDetector`
  - **Definition of Done:** All exclusion logic uses the centralized `ExcludedDirectories` class.
  - **Verifications:**
    - `ExcludedDirectories.All` contains all expected entries
    - Existing `node_modules` and Python venv exclusions still work
    - `dotnet build` succeeds

### S02: Integrate Recursive Scanning into LibraryScanner

Wire the recursive detection through `LibraryScanner.ScanDirectoryAsync`.

- **S02-T01**: Add `ScanDirectoryAsync` Overload with Options
  Add an overload that accepts `ProjectDetectionOptions` and passes it through to the project detector.
  - **Requirements:**
    - Add `Task<IReadOnlyList<ProjectPackages>> ScanDirectoryAsync(string directoryPath, ProjectDetectionOptions options, CancellationToken cancellationToken = default)` to `ILibraryScanner`
    - The existing `ScanDirectoryAsync(string, CancellationToken)` delegates to the new overload with default options
    - The new overload passes `options` to `_projectDetector.Detect(directoryPath, options)`
    - Deduplicate results — if the same project file is discovered via multiple paths (e.g., a `.csproj` found both directly and via a `.sln`), include it only once
  - **Definition of Done:** `ScanDirectoryAsync` can scan recursively when options say so.
  - **Verifications:**
    - Existing calls to `ScanDirectoryAsync(path, ct)` work unchanged
    - Recursive call discovers nested projects
    - No duplicate `ProjectPackages` results for the same project file

### S03: CLI `--recursive` Flag

Expose recursive scanning to the user via CLI flags.

- **S03-T01**: Add `--recursive` Option to `scan` Command
  Add a `--recursive` (alias `-r`) boolean flag to the `scan` CLI command.
  - **Requirements:**
    - Add `Option<bool>("--recursive", ...)` with alias `-r`, default `false`
    - Add `Option<int>("--depth", ...)` with default `5`, only meaningful when `--recursive` is set
    - Pass these values as `ProjectDetectionOptions` to `ScanDirectoryAsync`
    - Display discovered project paths grouped by ecosystem in the output
  - **Definition of Done:** `smart-skills scan --recursive` discovers and scans projects in nested directories.
  - **Verifications:**
    - `smart-skills scan --recursive` in a monorepo shows projects from subdirectories
    - `smart-skills scan` (without `--recursive`) behaves as before
    - `smart-skills scan --recursive --depth 1` limits to one level of nesting
    - `smart-skills scan --recursive --json` includes all discovered projects in JSON output

- **S03-T02**: Add `--recursive` Option to `install` Command
  Add the same `--recursive` flag to the `install` command so skills are resolved across all discovered projects.
  - **Requirements:**
    - Add `--recursive` and `--depth` options (same as `scan`)
    - Pass through to `SkillInstaller` → `LibraryScanner.ScanDirectoryAsync` with options
    - Aggregate packages from all discovered projects before matching against the registry
    - `--dry-run` with `--recursive` previews all matched skills across the monorepo
  - **Definition of Done:** `smart-skills install --recursive` installs skills for all projects in a directory tree.
  - **Verifications:**
    - `smart-skills install --recursive --dry-run` shows matched skills from nested projects
    - Skills from multiple ecosystems in a monorepo are all resolved

### S04: Testing

Comprehensive tests for recursive scanning, exclusion logic, and CLI integration.

- **S04-T01**: Unit Tests for ExcludedDirectories
  Verify the exclusion list is complete and correct.
  - **Requirements:**
    - Test that `All` contains every expected directory name
    - Test case-insensitive lookup (e.g., `Node_Modules` is excluded)
    - Test that the individual category sets are disjoint from each other where expected
  - **Definition of Done:** Exclusion list has full test coverage.
  - **Verifications:**
    - `dotnet test --filter "FullyQualifiedName~ExcludedDirectories"` passes

- **S04-T02**: Unit Tests for Recursive ProjectDetector
  Test recursive traversal with various directory layouts.
  - **Requirements:**
    - Create temp directory structures in test setup:
      - Flat layout (single project at root) — `Recursive = true` finds it
      - Nested monorepo (projects at depth 2 and 3) — all found within `MaxDepth`
      - Excluded directories present (`node_modules/package.json`, `.git/`) — skipped
      - Mixed ecosystems (`.csproj` at root, `package.json` in subdirectory) — both found
      - Depth boundary — project at depth 6 with `MaxDepth = 5` — not found
    - Test `Recursive = false` (default) still works as before
  - **Definition of Done:** Recursive detection logic has full coverage of edge cases.
  - **Verifications:**
    - `dotnet test --filter "FullyQualifiedName~ProjectDetector"` passes

- **S04-T03**: Unit Tests for Recursive LibraryScanner
  Test that `ScanDirectoryAsync` with recursive options discovers and resolves packages from nested projects.
  - **Requirements:**
    - Mock `IProjectDetector` to return projects at various depths
    - Verify `ScanDirectoryAsync` resolves packages for all discovered projects
    - Verify deduplication when a project appears via both direct detection and solution membership
  - **Definition of Done:** Scanner integration with recursive detection is tested.
  - **Verifications:**
    - `dotnet test --filter "FullyQualifiedName~LibraryScanner"` passes

- **S04-T04**: CLI Integration Tests
  Test the `--recursive` flag end-to-end.
  - **Requirements:**
    - Test `scan --recursive` in a directory with nested projects
    - Test `scan --recursive --depth 0` behaves like non-recursive scan
    - Test `install --recursive --dry-run` aggregates skills across nested projects
  - **Definition of Done:** CLI recursive flags work end-to-end.
  - **Verifications:**
    - `dotnet test --filter "FullyQualifiedName~Cli.Tests"` passes

## API Surface

See companion file `api.cs` for the updated public API sketch showing new and changed types.
