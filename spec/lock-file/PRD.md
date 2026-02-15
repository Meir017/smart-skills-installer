# Lock File Specification

## Overview

The **skills lock file** (`smart-skills.lock.json`) is a version-controlled file that records the exact state of all tracked skills for a project. It serves as the single source of truth for reproducible skill installations across machines and CI environments — analogous to `packages.lock.json` (NuGet) or `yarn.lock` (npm).

## Goals

1. **Reproducibility** — Any machine can restore the exact same set of skills from the lock file
2. **Change detection** — Detect when remote skills have been updated (commit SHA changed)
3. **Local edit awareness** — Detect when a developer has manually modified installed skill files
4. **Minimal fetching** — Skip downloads when remote commit SHA hasn't changed and local files are unmodified
5. **Version control friendly** — JSON format, deterministic ordering, diffable

## File Name & Location

- **File name**: `smart-skills.lock.json`
- **Location**: Same directory as the project/solution file (the "project root")
- **Version control**: Should be committed to the repository

## Schema

```json
{
  "version": 1,
  "skills": {
    "<skill-name>": {
      "remoteUrl": "https://github.com/microsoft/skills",
      "skillPath": "skills/azure-identity-dotnet",
      "language": "dotnet",
      "commitSha": "abc123def456...",
      "localContentHash": "sha256:e3b0c44298fc1c14..."
    }
  }
}
```

### Top-Level Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `version` | `integer` | Yes | Schema version. Currently `1`. Allows future format evolution. |
| `skills` | `object` | Yes | Map of skill name → skill lock entry. Keys are sorted alphabetically for deterministic output. |

### Skill Lock Entry Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `remoteUrl` | `string` | Yes | The repository URL from which the skill is fetched (GitHub or Azure DevOps). |
| `skillPath` | `string` | Yes | Relative path to the skill directory within the remote repository. |
| `language` | `string` | No | Ecosystem filter (`"dotnet"`, `"npm"`, `"pip"`, etc.). Omit if not ecosystem-specific. |
| `commitSha` | `string` | Yes | Full SHA of the most recent commit that touched the skill directory in the remote repo. Used for change detection — if the remote SHA matches this value, the skill is up-to-date. |
| `localContentHash` | `string` | Yes | A content hash of all installed skill files (see [Content Hashing](#content-hashing)). Used to detect local manual edits. Format: `"sha256:<hex>"`. |

## Content Hashing

The `localContentHash` is computed over the installed skill directory to detect manual edits:

1. Enumerate all files in the skill directory recursively (excluding hidden files/directories starting with `.`)
2. Compute relative paths from the skill root, **normalize to forward slashes** (`/`), and **normalize to NFC Unicode form**
3. Sort paths using **ordinal (byte-order) comparison**
4. For each file in sorted order:
   a. Read file content as **raw bytes** (no line-ending conversion)
   b. Compute SHA256 of the raw bytes → lowercase hex string
5. Concatenate all entries as UTF-8: `"<relative-path>\n<file-sha256>\n"` (using `\n` literal, not platform line ending)
6. Compute SHA256 of the concatenated UTF-8 bytes
7. Store as `"sha256:<lowercase-hex>"`

### Cross-Platform Guarantees

| Concern | Mitigation |
|---------|------------|
| Path separators (`\` vs `/`) | Always normalize to `/` before sorting and concatenation |
| Unicode normalization (e.g. macOS NFD vs Windows NFC) | Normalize all paths to NFC (`String.Normalize(NormalizationForm.FormC)`) |
| Line endings in skill files (`\r\n` vs `\n`) | Hash raw bytes — no conversion. Files fetched from the provider are stored as-is, so identical downloads produce identical hashes |
| Sort order locale differences | Use ordinal (`StringComparison.Ordinal`) sort, not culture-aware |
| Hash output casing | Always lowercase hex |

> **Important**: Because the hash is over raw bytes, a `git checkout` that applies line-ending conversion (via `.gitattributes` `text=auto`) would change the hash. Skill directories should either be fetched via the SmartSkills provider (which does not apply conversions) or use `binary` / `-text` attributes.

This approach ensures:
- File renames, additions, and deletions are detected
- File content changes are detected
- The hash is identical on Windows, macOS, and Linux

## Workflows

### `install` (fresh or update)

```
1. Resolve packages via IPackageResolver
2. Match packages against registry → list of skills to install
3. For each matched skill:
   a. If skill exists in lock file:
      - Fetch remote commit SHA via ISkillSourceProvider.GetLatestCommitShaAsync
      - If commitSha matches lock file entry → compute local content hash
        - If localContentHash matches → SKIP (up-to-date, no local edits)
        - If localContentHash differs → WARN "locally modified", skip unless --force
      - If commitSha differs → download new version, update lock entry
   b. If skill not in lock file:
      - Download skill
      - Compute localContentHash of downloaded files
      - Add entry to lock file
4. Write updated lock file (sorted, indented)
```

### `restore` (from lock file)

```
1. Read lock file
2. For each skill in lock file:
   a. If skill directory exists and localContentHash matches → SKIP
   b. Otherwise → download skill at the recorded commitSha
      (use provider to fetch files at that specific commit, not latest)
3. Recompute and verify localContentHash after download
```

### `status`

```
1. Read lock file
2. For each skill:
   a. Check if local files exist and compute current content hash
   b. Compare against lock file's localContentHash → report if modified
   c. Optionally fetch remote commit SHA → report if update available
```

### `uninstall <skill-name>`

```
1. Remove skill directory from disk
2. Remove entry from lock file
3. Write updated lock file
```

## Relationship to Existing State File

The lock file **replaces** the current `.agents-skills-state.json` file:

| Aspect | `.agents-skills-state.json` (current) | `smart-skills.lock.json` (new) |
|--------|----------------------------------------|--------------------------------|
| Purpose | Internal runtime state | Version-controlled contract |
| Version control | Gitignored | Committed |
| Contains | Skill metadata, install path, timestamp, commit SHA | Fetch coordinates, commit SHA, content hash |
| Local edit detection | No | Yes (via `localContentHash`) |
| Reproducible restore | No (requires re-resolving) | Yes (lock file has all info needed) |
| Deterministic output | No | Yes (sorted keys, stable format) |

Migration: When the lock file is adopted, the existing `LocalSkillStore` / `ISkillStore` will be refactored to read/write `smart-skills.lock.json` instead.

## Example

```json
{
  "version": 1,
  "skills": {
    "azure-identity-dotnet": {
      "remoteUrl": "https://github.com/microsoft/skills",
      "skillPath": "skills/azure-identity-dotnet",
      "language": "dotnet",
      "commitSha": "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2",
      "localContentHash": "sha256:9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08"
    },
    "redis-development": {
      "remoteUrl": "https://github.com/redis/agent-skills",
      "skillPath": "skills/redis-development",
      "language": "dotnet",
      "commitSha": "f6e5d4c3b2a1f6e5d4c3b2a1f6e5d4c3b2a1f6e5",
      "localContentHash": "sha256:535de3a03f7e82e6768b3b4c8f0496d6d03f3023aa5a0b36e2069c912c08e4d0"
    }
  }
}
```

## Design Decisions

1. **JSON over YAML** — Consistent with the existing registry format and .NET ecosystem conventions (e.g. `packages.lock.json`)
2. **Flat skill map (not array)** — Enables O(1) lookup by skill name and produces cleaner diffs
3. **Full commit SHA** — Avoids ambiguity from short SHAs; 40-char hex string
4. **Separate `commitSha` and `localContentHash`** — `commitSha` tracks remote state; `localContentHash` tracks local state. Together they answer: "is the remote updated?" and "did someone edit files locally?"
5. **No skill metadata in lock file** — The lock file is about *fetch coordinates and integrity*, not skill content. Metadata lives in the installed `SKILL.md`.
6. **`restore` fetches at specific commit** — Unlike `install` (which gets latest), `restore` should fetch files at exactly the recorded `commitSha` for reproducibility. This requires provider support for fetching at a specific ref.
