# Match Strategy Abstraction & nuget-manager Skill

**Version:** 1.0.0

## Description

Refactor the skill matching system from a hardcoded package-pattern approach to an **extensible strategy pattern**. Each `RegistryEntry` declares a match strategy (e.g. `package`, `file-exists`) and strategy-specific criteria. An `IMatchStrategy` interface enables future strategies (e.g. `search-results-match` for environment variable detection) with zero changes to the core matcher. The first new strategy is `file-exists`, used to register the **nuget-manager** skill from `github/awesome-copilot` for .NET projects.

## Goals

1. Extract the current package-pattern matching into a `package` `IMatchStrategy`, preserving all existing behavior
2. Introduce an `IMatchStrategy` abstraction with a `MatchContext` carrying all available signals (packages, root file names, future data sources)
3. Implement a `file-exists` strategy matching when specified file patterns are found in the project root
4. Design for extensibility — future strategies (e.g. `search-results-match`) require only a new class + DI registration
5. Register the **nuget-manager** skill using the `file-exists` strategy, triggered by `.sln`, `.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`
6. Integrate into the installation pipeline (CLI, MSBuild, lock file) with no breaking changes

---

## Stories & Tasks

### S01: Match Strategy Abstraction

Define `IMatchStrategy`, `MatchContext`, and a strategy resolver. This is the core extensibility point — adding a new strategy requires only a new `IMatchStrategy` implementation and a DI registration.

- **S01-T01**: Define IMatchStrategy interface and MatchContext
  `IMatchStrategy` takes a `MatchContext` (packages, root files, etc.) and criteria, returns `MatchResult`. `MatchContext` is a class (not record) so new signal properties can be added without breaking existing strategies.

- **S01-T02**: Implement PackageMatchStrategy
  Extract existing `SkillMatcher.IsMatch` glob/exact logic into `PackageMatchStrategy`. Pure extraction — identical behavior.

- **S01-T03**: Implement FileExistsMatchStrategy
  Matches when criteria file patterns are found in `MatchContext.RootFileNames`. Supports exact names and globs.

- **S01-T04**: Create IMatchStrategyResolver
  Maps strategy names to `IMatchStrategy` instances via DI. Throws on unknown names.

### S02: Refactor RegistryEntry and Parser

- **S02-T01**: Refactor RegistryEntry to use MatchStrategy + MatchCriteria
  Replace `PackagePatterns` with `MatchStrategy` (string, defaults to `"package"`) and `MatchCriteria` (list of strings). Backward compatible — existing entries default to the `package` strategy.

- **S02-T02**: Update RegistryIndexParser for strategy-aware JSON
  Support two JSON formats: legacy `packagePatterns` (auto-inferred as `package` strategy) and new `matchStrategy` + `matchCriteria`.

### S03: Refactor SkillMatcher to Use Strategies

- **S03-T01**: Refactor SkillMatcher to delegate to IMatchStrategy
  Build `MatchContext`, resolve strategy per entry, delegate matching. Remove all inline matching logic.

- **S03-T02**: Update ISkillMatcher interface signature
  Accept `rootFileNames` parameter (optional, defaults to empty) so all signals reach the `MatchContext`.

### S04: Add nuget-manager to Built-in Registry

- **S04-T01**: Add nuget-manager entry to skills-registry.json
  ```json
  {
    "matchStrategy": "file-exists",
    "matchCriteria": ["*.sln", "*.slnx", "global.json", "Directory.Build.props", "Directory.Packages.props"],
    "skillPath": "skills/nuget-manager",
    "language": "dotnet"
  }
  ```
  Source: `https://github.com/github/awesome-copilot`

- **S04-T02**: Update skills-registry.md documentation

### S05: Installation Pipeline & Scanning Integration

- **S05-T01**: Collect root file names in scanning pipeline
- **S05-T02**: Update SkillInstaller.InstallAsync to build MatchContext

### S06: CLI and MSBuild Surface

- **S06-T01**: CLI scan command shows strategy-matched skills
- **S06-T02**: MSBuild task supports strategy-based matching

### S07: DI Registration and Wiring

- **S07-T01**: Register match strategies and resolver in DI

### S08: Testing

- **S08-T01**: Unit tests for PackageMatchStrategy
- **S08-T02**: Unit tests for FileExistsMatchStrategy
- **S08-T03**: Unit tests for RegistryIndexParser strategy support
- **S08-T04**: Integration test for nuget-manager end-to-end

---

## API Surface

See `api.cs` for the full proposed type changes. Key additions:

- **`IMatchStrategy`** — strategy interface with `Name` and `Evaluate(MatchContext, criteria)`
- **`MatchContext`** — extensible class carrying all signals (packages, root files, future fields)
- **`PackageMatchStrategy`** — extracted from current `SkillMatcher.IsMatch`
- **`FileExistsMatchStrategy`** — matches file name patterns against root directory files
- **`IMatchStrategyResolver`** — maps strategy names to implementations via DI
- **`RegistryEntry`** — `PackagePatterns` replaced by `MatchStrategy` + `MatchCriteria`

## Future Strategy Example

A `search-results-match` strategy could search file contents for patterns (e.g. env vars in `.env`):
```json
{
  "matchStrategy": "search-results-match",
  "matchCriteria": ["AZURE_STORAGE_CONNECTION_STRING", "AZURE_BLOB_*"],
  "skillPath": "skills/azure-storage-dotnet",
  "language": "dotnet"
}
```
This would only require a new `SearchResultsMatchStrategy : IMatchStrategy` class and a DI registration — zero changes to `SkillMatcher`, `RegistryEntry`, or `RegistryIndexParser`.
