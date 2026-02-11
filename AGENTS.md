# AGENTS.md — .NET Development Guidelines

## Project Overview

This is a .NET 10 solution (`SmartSkills.sln`) consisting of:

- **SmartSkills.Core** — shared class library (scanning, matching, fetching, installation logic)
- **SmartSkills.Cli** — console application (CLI tool using `System.CommandLine`)
- **SmartSkills.MSBuild** — custom MSBuild tasks and targets
- **tests/** — xUnit test projects for each component

## Target Framework

This project targets **.NET 10** (`net10.0`). All projects must use `net10.0` as their `TargetFramework`. The `global.json` pins the SDK version accordingly.

## Documentation & API Reference

**Always use the Microsoft Learn MCP tools** (`microsoft_docs_search`, `microsoft_code_sample_search`, `microsoft_docs_fetch`) to look up:

- .NET APIs, classes, and method signatures
- NuGet package usage and configuration
- MSBuild task authoring patterns
- `System.CommandLine` API reference
- `Microsoft.Extensions.*` library usage
- xUnit testing patterns and best practices

Do NOT guess at API signatures or parameter names — search Microsoft Learn first to ensure accuracy with .NET 10 APIs.

## Dependency Management

**Always use the `dotnet` CLI to manage dependencies.** Never manually edit `.csproj` files to add, remove, or update package references.

### Adding packages

```shell
dotnet add <project> package <PackageName>
dotnet add src/SmartSkills.Core/SmartSkills.Core.csproj package System.Text.Json
```

### Removing packages

```shell
dotnet remove <project> package <PackageName>
```

### Adding project references

```shell
dotnet add <project> reference <other-project>
dotnet add src/SmartSkills.Cli/SmartSkills.Cli.csproj reference src/SmartSkills.Core/SmartSkills.Core.csproj
```

### Updating packages

```shell
dotnet outdated              # check for outdated packages (requires dotnet-outdated tool)
dotnet add <project> package <PackageName>  # re-adding picks up the latest version
```

## Build & Test Commands

```shell
dotnet build                           # build the entire solution
dotnet test                            # run all tests
dotnet test --filter "FullyQualifiedName~SmartSkills.Core.Tests"  # run specific test project
dotnet run --project src/SmartSkills.Cli  # run the CLI tool
dotnet pack                            # create NuGet packages
```

## Solution Structure Conventions

```
SmartSkills.sln
Directory.Build.props          # shared build properties (LangVersion, Nullable, TreatWarningsAsErrors)
global.json                    # SDK version pin
src/
  SmartSkills.Core/            # shared logic — no UI or CLI dependencies
  SmartSkills.Cli/             # CLI entry point — depends on Core
  SmartSkills.MSBuild/         # MSBuild tasks — depends on Core
tests/
  SmartSkills.Core.Tests/
  SmartSkills.Cli.Tests/
  SmartSkills.MSBuild.Tests/
```

### Key rules

- **Core must have zero UI dependencies.** All scanning, matching, fetching, and installation logic lives here.
- **Cli depends on Core only.** CLI-specific concerns (argument parsing, console output) stay in the Cli project.
- **MSBuild depends on Core only.** MSBuild task wrappers stay thin — delegate to Core for logic.
- **Test projects mirror src/ structure** and reference the project they test.

## Coding Standards

### C# conventions

- Use **file-scoped namespaces** (`namespace Foo;` not `namespace Foo { }`)
- Use **primary constructors** where appropriate (C# 12+)
- Use **collection expressions** (`[1, 2, 3]`) instead of `new List<int> { 1, 2, 3 }`
- Prefer `required` properties over constructor parameters for DTOs/models
- Use `init` setters for immutable data
- Use **raw string literals** (`"""..."""`) for multi-line strings and embedded quotes
- Prefer **pattern matching** (`is`, `switch` expressions) over type casting
- Use `IAsyncEnumerable<T>` for streaming results where appropriate
- Nullable reference types are enabled — avoid `null!` suppressions; fix the nullability instead

### Naming

- `PascalCase` for types, methods, properties, events
- `camelCase` for local variables and parameters
- `_camelCase` for private fields
- `I` prefix for interfaces (`ISkillRegistry`)
- `Async` suffix for async methods (`FetchRegistryAsync`)

### Dependency injection

- Use `Microsoft.Extensions.DependencyInjection` for service registration
- Prefer constructor injection
- Register services with the narrowest lifetime (`Transient` > `Scoped` > `Singleton`)
- Use `IHttpClientFactory` instead of manually creating `HttpClient` instances

### Logging

- Use `ILogger<T>` from `Microsoft.Extensions.Logging`
- Use **structured logging** with message templates: `_logger.LogInformation("Fetched {SkillCount} skills from {Source}", count, source)`
- Never use string interpolation in log messages (`$"Fetched {count}"` — this defeats structured logging)
- Log levels: `Debug` for internals, `Information` for workflow steps, `Warning` for recoverable issues, `Error` for failures

### Error handling

- Use exceptions for exceptional conditions, not for control flow
- Create custom exception types in Core for domain errors (e.g., `SkillRegistryException`, `SkillInstallationException`)
- Always include an inner exception when wrapping: `throw new SkillRegistryException("...", ex)`
- Use `CancellationToken` consistently in all async methods

### Async patterns

- All I/O-bound operations must be `async`/`await`
- Pass `CancellationToken` through the entire call chain
- Use `ConfigureAwait(false)` in library code (Core, MSBuild) but **not** in the CLI entry point
- Never use `.Result` or `.Wait()` — these cause deadlocks

## Testing

- **Framework:** xUnit
- **Mocking:** NSubstitute (use `dotnet add <test-project> package NSubstitute` to add)
- **Assertions:** xUnit built-in (`Assert.Equal`, `Assert.Throws`, etc.) or FluentAssertions
- Follow **Arrange-Act-Assert** pattern
- Test names: `MethodName_Scenario_ExpectedResult` (e.g., `ScanProject_WithNoPackages_ReturnsEmptyList`)
- Place test fixtures and sample data in a `TestData/` subfolder within each test project
- Use `ITestOutputHelper` for test-scoped logging

## MSBuild Task Authoring

- Inherit from `Microsoft.Build.Utilities.Task`
- Use `ITaskItem[]` for inputs and outputs
- Log via `Log.LogMessage`, `Log.LogWarning`, `Log.LogError`
- Use MSBuild error codes (e.g., `SMSK001`) for machine-readable diagnostics
- Keep task classes thin — delegate to Core services
- Implement `ICancelableTask` for cancellation support

## NuGet Packaging

- CLI tool: set `PackAsTool=true` and `ToolCommandName=skills-installer` in Cli `.csproj`
- MSBuild package: include `build/` and `buildTransitive/` folders with `.props` and `.targets`
- Set `DevelopmentDependency=true` for the MSBuild package
- Use `dotnet pack` to create packages — never manually create `.nupkg` files

## Configuration Precedence

CLI flags > project-level config (`.skills-installer.json`) > user-level config (`~/.smart-skills/config.json`) > defaults

## Git & Source Control

- Do not commit `bin/`, `obj/`, or `.nupkg` files
- The `.gitignore` should exclude standard .NET build artifacts
- Do not commit `packages.lock.json` unless Central Package Management is in use
