# Multi-Language Support: Python (pip / poetry / uv / pipenv)

Extend SmartSkills to detect and install agent skills for Python projects. This builds on the language-agnostic core introduced in the Node.js PR (`feature/multi-language-nodejs`) — adding a `"python"` ecosystem constant, Python-specific package resolvers, project detection for Python markers, and embedded registry entries for the 40 Python skills available in [microsoft/skills](https://github.com/microsoft/skills).

## Goals

1. Auto-detect Python projects by the presence of `pyproject.toml`, `requirements.txt`, or `setup.py`
2. Resolve Python dependencies (direct and transitive) from lock files: `uv.lock`, `poetry.lock`, `Pipfile.lock`, and `requirements.txt`
3. Add the `"python"` ecosystem constant and register Python skill entries in the embedded registry
4. Maintain full backward compatibility — existing .NET and Node.js workflows are unchanged
5. Support the same CLI commands (`scan`, `install`, `list`, `status`) for Python projects with no special flags

## Non-Goals

- Ruby, Go, Rust, or other languages — those can follow this pattern later
- Virtual environment management or Python version detection
- Running `pip install` or any Python tooling — SmartSkills only reads lock files
- Supporting `conda` environments (can be a follow-up)

## Prerequisites

This spec assumes the Node.js multi-language PR has been merged, providing:
- `Ecosystems` constants class with `Dotnet` and `Npm`
- `RegistryEntry.Language` field and language-aware `SkillMatcher`
- `IProjectDetector` / `ProjectDetector` abstraction
- `IPackageResolverFactory` / `PackageResolverFactory`
- `LibraryScanner.ScanDirectoryAsync`

## Python Ecosystem Landscape

| Lock File | Tool | Format | Notes |
|-----------|------|--------|-------|
| `uv.lock` | [uv](https://docs.astral.sh/uv/) | TOML | Fast, modern. Gaining rapid adoption. |
| `poetry.lock` | [Poetry](https://python-poetry.org/) | TOML | Mature, widely used. Separate `[metadata]` section. |
| `Pipfile.lock` | [Pipenv](https://pipenv.pypa.io/) | JSON | Older but still common. `default` + `develop` sections. |
| `requirements.txt` | pip | Plain text | No lock file per se; pinned versions (`pkg==1.2.3`). Universally supported. |

All four formats identify packages by [PyPI](https://pypi.org/) package names (lowercase, hyphens normalized to hyphens or underscores). Direct vs transitive classification requires cross-referencing with `pyproject.toml` or `requirements.txt`.

### Package Name Normalization

PyPI normalizes package names: `azure-identity`, `azure_identity`, and `Azure-Identity` all refer to the same package. The resolver must normalize names to lowercase with hyphens (PEP 503) before matching against the registry.

## Stories

### S01: Python Ecosystem Constant & Detection

Add the Python ecosystem to the existing multi-language infrastructure.

- **S01-T01**: Add `Python` Ecosystem Constant
  Add `public const string Python = "python";` to the `Ecosystems` class in `Scanning/Models.cs`.
  - **Requirements:**
    - Add `Python = "python"` constant
    - No other changes needed — existing code handles unknown ecosystems gracefully
  - **Definition of Done:** Constant exists, `dotnet build` succeeds.
  - **Verifications:**
    - `dotnet build` passes

- **S01-T02**: Extend `ProjectDetector` for Python
  Add Python project detection to the existing `ProjectDetector.Detect` method.
  - **Requirements:**
    - Detect Python projects by looking for (in priority order):
      1. `pyproject.toml` — modern standard (PEP 621)
      2. `setup.py` — legacy but still common
      3. `requirements.txt` — minimal but universal
    - Return the first found as `DetectedProject(Ecosystems.Python, filePath)`
    - Only detect at the top level of the directory (not inside `venv/`, `.venv/`, `__pycache__/`)
    - If `pyproject.toml` is present, prefer it over `setup.py` and `requirements.txt`
  - **Definition of Done:** `ProjectDetector` returns a Python project alongside .NET/Node.js projects in a polyglot directory.
  - **Verifications:**
    - Unit test: directory with only `pyproject.toml` → `DetectedProject("python", ...)`
    - Unit test: directory with only `requirements.txt` → `DetectedProject("python", ...)`
    - Unit test: directory with `pyproject.toml` + `setup.py` → returns `pyproject.toml`
    - Unit test: directory with `.sln` + `package.json` + `pyproject.toml` → three results
    - Existing detection tests still pass

### S02: Python Package Resolvers

Implement `IPackageResolver` for Python lock files.

- **S02-T01**: Implement PyPI Package Name Normalization Utility
  Create a shared utility for normalizing Python package names per PEP 503.
  - **Requirements:**
    - Normalize to lowercase
    - Replace underscores, dots, and consecutive hyphens with a single hyphen
    - `Azure_Identity` → `azure-identity`, `my.package` → `my-package`
    - Expose as a static method: `PypiNameNormalizer.Normalize(string name)`
  - **Definition of Done:** Normalizer handles all PEP 503 edge cases.
  - **Verifications:**
    - Unit test: `Azure_Identity` → `azure-identity`
    - Unit test: `my.package` → `my-package`
    - Unit test: `already-normal` → `already-normal`
    - Unit test: `UPPER__CASE` → `upper-case`

- **S02-T02**: Implement Direct Dependency Extraction from `pyproject.toml`
  Parse `pyproject.toml` to identify direct dependencies, used by all Python resolvers for transitive classification.
  - **Requirements:**
    - Parse `[project.dependencies]` array for direct production dependencies
    - Parse `[project.optional-dependencies]` for extras/dev dependencies
    - Parse `[tool.poetry.dependencies]` for Poetry-managed projects
    - Handle PEP 508 dependency specifiers (e.g. `azure-identity>=1.0.0,<2.0`)
    - Return a set of normalized package names
  - **Definition of Done:** Correctly extracts direct dependency names from `pyproject.toml`.
  - **Verifications:**
    - Unit test: PEP 621 `[project.dependencies]` parsed correctly
    - Unit test: Poetry `[tool.poetry.dependencies]` parsed correctly
    - Unit test: version specifiers stripped, names normalized

- **S02-T03**: Implement `UvLockPackageResolver`
  Implement `IPackageResolver` for `uv.lock` (TOML format).
  - **Requirements:**
    - Parse `uv.lock` TOML: each `[[package]]` table has `name` and `version` fields
    - Cross-reference with `pyproject.toml` direct dependencies for `IsTransitive` classification
    - Normalize all package names via `PypiNameNormalizer`
    - Set `Ecosystem = Ecosystems.Python`
    - Fall back to `pyproject.toml`/`requirements.txt` direct deps when `uv.lock` is missing
  - **Definition of Done:** `UvLockPackageResolver` returns resolved packages from `uv.lock`.
  - **Verifications:**
    - Unit test with sample `uv.lock`: correct packages and versions
    - Unit test: direct vs transitive classification matches `pyproject.toml`
    - Unit test: package names are normalized

- **S02-T04**: Implement `PoetryLockPackageResolver`
  Implement `IPackageResolver` for `poetry.lock` (TOML format).
  - **Requirements:**
    - Parse `poetry.lock` TOML: each `[[package]]` table has `name`, `version`, and `category` fields
    - Use `category = "main"` vs `"dev"` where available
    - Cross-reference with `pyproject.toml` for direct dependency classification
    - Normalize all package names
    - Set `Ecosystem = Ecosystems.Python`
    - Fall back to direct deps when `poetry.lock` is missing
  - **Definition of Done:** `PoetryLockPackageResolver` parses both Poetry lock formats.
  - **Verifications:**
    - Unit test with sample `poetry.lock`: correct packages and versions
    - Unit test: direct vs transitive classification
    - Unit test: dev dependencies included

- **S02-T05**: Implement `PipfileLockPackageResolver`
  Implement `IPackageResolver` for `Pipfile.lock` (JSON format).
  - **Requirements:**
    - Parse `Pipfile.lock` JSON: `default` section for production deps, `develop` for dev deps
    - Each entry has `"version": "==1.2.3"` format
    - All entries in `default` + `develop` are direct (Pipfile.lock is flat)
    - Normalize all package names
    - Set `Ecosystem = Ecosystems.Python`
  - **Definition of Done:** `PipfileLockPackageResolver` correctly parses Pipfile.lock.
  - **Verifications:**
    - Unit test with sample `Pipfile.lock`: correct packages and versions
    - Unit test: `default` and `develop` sections both parsed
    - Unit test: version prefix (`==`) stripped

- **S02-T06**: Implement `RequirementsTxtPackageResolver`
  Implement `IPackageResolver` for `requirements.txt` (plain text format).
  - **Requirements:**
    - Parse each line as a PEP 508 requirement: `package==1.2.3`, `package>=1.0`, `package`
    - Skip comments (`#`), blank lines, and option lines (`-r`, `--index-url`, etc.)
    - Handle `-r other-file.txt` includes (recursively read referenced files)
    - All entries are direct (`IsTransitive = false`) since `requirements.txt` has no dependency graph
    - Normalize all package names
    - Set `Ecosystem = Ecosystems.Python`
  - **Definition of Done:** `RequirementsTxtPackageResolver` handles real-world `requirements.txt` files.
  - **Verifications:**
    - Unit test: standard `pkg==1.2.3` lines parsed
    - Unit test: comments and blank lines skipped
    - Unit test: `-r` includes followed
    - Unit test: extras (`pkg[extra]>=1.0`) handled — extras stripped from name

- **S02-T07**: Extend `PackageResolverFactory` for Python
  Add Python ecosystem routing to the existing factory.
  - **Requirements:**
    - For `Ecosystem == "python"`: inspect the project directory for lock files:
      - `uv.lock` present → `UvLockPackageResolver`
      - `poetry.lock` present → `PoetryLockPackageResolver`
      - `Pipfile.lock` present → `PipfileLockPackageResolver`
      - Otherwise → `RequirementsTxtPackageResolver`
    - Register all Python resolvers as singletons in DI
  - **Definition of Done:** Factory routes to the correct Python resolver.
  - **Verifications:**
    - Unit test: python project with `uv.lock` → `UvLockPackageResolver`
    - Unit test: python project with `poetry.lock` → `PoetryLockPackageResolver`
    - Unit test: python project with `Pipfile.lock` → `PipfileLockPackageResolver`
    - Unit test: python project with only `requirements.txt` → `RequirementsTxtPackageResolver`

### S03: Python Skill Registry Entries

Populate the embedded registry with Python/PyPI skill entries.

- **S03-T01**: Add Python Skill Entries to Embedded Registry
  Add entries for the 40 Python skills in [microsoft/skills](https://github.com/microsoft/skills).
  - **Requirements:**
    - Map PyPI package names to skill paths, e.g.:
      - `azure-identity` → `.github/skills/azure-identity-py`
      - `azure-servicebus` → `.github/skills/azure-servicebus-py`
      - `azure-search-documents` → `.github/skills/azure-search-documents-py`
      - `azure-cosmos` → `.github/skills/azure-cosmos-py`
      - `azure-storage-blob` → `.github/skills/azure-storage-blob-py`
      - `azure-ai-projects` → `.github/skills/azure-ai-projects-py`
      - `azure-keyvault-secrets`, `azure-keyvault-keys`, `azure-keyvault-certificates` → `.github/skills/azure-keyvault-py`
      - `azure-monitor-opentelemetry` → `.github/skills/azure-monitor-opentelemetry-py`
      - `fastapi` → `.github/skills/fastapi-router-py`
      - `pydantic` → `.github/skills/pydantic-models-py`
      - (and all remaining Python skill directories)
    - Each entry must include `"language": "python"`
    - Verify skill paths exist in the repository
  - **Definition of Done:** Embedded registry includes Python entries loaded at startup.
  - **Verifications:**
    - `RegistryIndexParser.LoadEmbedded()` returns entries with `Language == "python"`
    - Python entries don't break existing .NET and npm entry loading

### S04: Testing

Comprehensive tests for Python functionality.

- **S04-T01**: Unit Tests for Package Name Normalization
  Test `PypiNameNormalizer.Normalize` with edge cases.
  - **Requirements:**
    - Test underscore → hyphen conversion
    - Test dot → hyphen conversion
    - Test uppercase → lowercase
    - Test consecutive separator collapse
    - Test already-normalized names unchanged
  - **Definition of Done:** Full test coverage for normalization.
  - **Verifications:**
    - `dotnet test --filter "FullyQualifiedName~PypiNameNormalizer"` passes

- **S04-T02**: Unit Tests for Python Resolvers
  Test each resolver with sample lock files in `TestData/Python/`.
  - **Requirements:**
    - Test `UvLockPackageResolver` with sample `uv.lock`
    - Test `PoetryLockPackageResolver` with sample `poetry.lock`
    - Test `PipfileLockPackageResolver` with sample `Pipfile.lock`
    - Test `RequirementsTxtPackageResolver` with sample `requirements.txt`
    - Test fallback behavior when lock files are missing
    - Place sample files in `tests/SmartSkills.Core.Tests/TestData/Python/`
  - **Definition of Done:** All four resolvers have tests with real sample lock files.
  - **Verifications:**
    - `dotnet test --filter "FullyQualifiedName~Python"` passes

- **S04-T03**: Unit Tests for Python Project Detection
  Extend `ProjectDetectorTests` for Python scenarios.
  - **Requirements:**
    - Test: `pyproject.toml` only → Python detected
    - Test: `requirements.txt` only → Python detected
    - Test: `pyproject.toml` preferred over `setup.py`
    - Test: polyglot dir (.sln + package.json + pyproject.toml) → three results
  - **Definition of Done:** Detection tests cover all Python project markers.
  - **Verifications:**
    - `dotnet test --filter "FullyQualifiedName~ProjectDetector"` passes

- **S04-T04**: Unit Tests for Factory Routing
  Extend `PackageResolverFactory` tests for Python.
  - **Requirements:**
    - Test: python project with each lock file type routes to the correct resolver
  - **Definition of Done:** Factory tests cover Python routing.
  - **Verifications:**
    - `dotnet test --filter "FullyQualifiedName~ResolverFactory"` passes

## API Surface

See companion file `api.cs` for the new and changed types.

## Dependency Changes

| Package | Purpose | Project |
|---------|---------|---------|
| `Tomlyn` | Parse TOML files (`pyproject.toml`, `uv.lock`, `poetry.lock`) | `SmartSkills.Core` |

`Pipfile.lock` is JSON (parsed via `System.Text.Json`). `requirements.txt` is plain text (custom parser).

## Sample Lock File Formats

### uv.lock (TOML)
```toml
version = 1

[[package]]
name = "azure-identity"
version = "1.16.0"

[[package]]
name = "azure-core"
version = "1.30.1"
```

### poetry.lock (TOML)
```toml
[[package]]
name = "azure-identity"
version = "1.16.0"
description = "Microsoft Azure Identity Library for Python"
category = "main"

[[package]]
name = "azure-core"
version = "1.30.1"
description = "Microsoft Azure Core Library for Python"
category = "main"
```

### Pipfile.lock (JSON)
```json
{
  "default": {
    "azure-identity": { "version": "==1.16.0" },
    "azure-core": { "version": "==1.30.1" }
  },
  "develop": {
    "pytest": { "version": "==8.0.0" }
  }
}
```

### requirements.txt (plain text)
```
azure-identity==1.16.0
azure-core>=1.30.0,<2.0
requests
-r dev-requirements.txt
# this is a comment
```
