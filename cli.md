# SmartSkills CLI — Verb & Design Review

A deep analysis of the `smart-skills` CLI command verbs, options, and overall design against industry conventions and best practices.

## Sources Consulted

| Source | URL |
|--------|-----|
| Command Line Interface Guidelines (clig.dev) | https://clig.dev |
| System.CommandLine Design Guidance (Microsoft Learn) | https://learn.microsoft.com/en-us/dotnet/standard/commandline/design-guidance |
| PatternFly CLI Handbook | https://www.patternfly.org/developer-resources/cli-handbook/ |
| GNU Coding Standards — CLI | https://www.gnu.org/prep/standards/html_node/Command_002dLine-Interfaces.html |
| Better CLI Design Guide | https://bettercli.org/ |
| ThoughtWorks CLI Design Guidelines | https://www.thoughtworks.com/insights/blog/engineering-effectiveness/elevate-developer-experiences-cli-design-guidelines |
| Helm CLI Reference | https://helm.sh/docs/helm/ |
| Ubuntu CLI Verbosity Guidelines | https://discourse.ubuntu.com/t/cli-verbosity-levels/26973 |

---

## Current CLI Surface

```
smart-skills
├── scan          Scan a project/solution for installed packages
├── install       Install skills based on detected packages
├── uninstall     Remove an installed skill
├── restore       Restore skills from the lock file
├── list          List installed skills
└── status        Show status of installed skills and available updates
```

### Global Options

| Option | Alias | Type | Description |
|--------|-------|------|-------------|
| `--verbose` | `-v` | `bool` | Enable detailed logging |
| `--dry-run` | — | `bool` | Preview actions without executing |
| `--base-dir` | — | `string?` | Base directory for `.agents/skills` |

---

## Verb-by-Verb Analysis

### ✅ `install` — **Standard and Clear**

**Verdict: Excellent choice.**

| Tool | Install verb |
|------|-------------|
| npm | `npm install` |
| pip | `pip install` |
| Helm | `helm install` |
| dotnet | `dotnet add package` / `dotnet tool install` |
| cargo | `cargo install` |
| brew | `brew install` |

`install` is the most universal verb in package/dependency management CLIs. It immediately communicates "fetch and set up." SmartSkills' use is perfectly aligned with industry convention.

**Note:** The README documents `--force` flag for `install`, but it is **not implemented** in `Program.cs`. This is a documentation/implementation mismatch that should be resolved.

---

### ✅ `uninstall` — **Standard and Clear**

**Verdict: Correct verb choice.**

| Tool | Removal verb |
|------|-------------|
| npm | `uninstall` |
| pip | `uninstall` |
| Helm | `uninstall` |
| brew | `uninstall` |
| apt | `remove` |
| PowerShell | `Remove-` (approved verb) |

The ecosystem is split between `uninstall` and `remove`. For software/package-oriented CLIs, `uninstall` is the dominant convention (npm, pip, Helm, dotnet tool). System-level package managers (apt, yum) tend to use `remove`. Since SmartSkills operates at the package/tool level, `uninstall` is the right choice.

**Consideration:** npm provides `remove` as an alias for `uninstall`. Adding `remove` as a hidden alias could improve discoverability for users coming from different ecosystems. This is optional, not required.

---

### ✅ `list` — **Standard and Clear**

**Verdict: Excellent choice.**

| Tool | List verb |
|------|----------|
| npm | `list` / `ls` |
| pip | `list` |
| Helm | `list` / `ls` |
| dotnet | `dotnet list package` |
| brew | `list` |
| kubectl | `get` |

`list` is the clearest, most widely understood verb for displaying installed items. It is preferred over `ls` for cross-platform tools (especially .NET, which targets Windows prominently). The Microsoft System.CommandLine design guidance favors full English words over abbreviations.

**Consideration:** Adding `ls` as a hidden alias would benefit Unix-native users, following npm and Helm precedent. Optional but nice-to-have.

---

### ✅ `restore` — **Standard and Clear**

**Verdict: Excellent choice. Aligns perfectly with .NET ecosystem conventions.**

| Tool | Restore-from-lock verb |
|------|----------------------|
| dotnet | `dotnet restore` |
| npm | `npm ci` (equivalent; `npm install` with lockfile) |
| cargo | `cargo fetch` |
| NuGet CLI | `nuget restore` |
| composer | `composer install` (from lock) |

In the .NET ecosystem, `restore` is the canonical verb for "reconstruct the dependency graph from a lock file." SmartSkills targets .NET developers as its primary audience, so using `restore` to mean "reinstall skills at their exact locked versions" is immediately intuitive. The distinction between `install` (fetch latest, update lock) and `restore` (replay lock file exactly) matches the `dotnet add package` vs `dotnet restore` mental model.

---

### 🟡 `scan` — **Functional but Somewhat Non-standard**

**Verdict: Works, but has some semantic ambiguity.**

`scan` is used in SmartSkills to mean: "detect project dependencies and match them against the skill registry." Let's look at what `scan` typically means in other tools:

| Tool | Verb | What it does |
|------|------|-------------|
| Snyk | `snyk test` / `snyk scan` | Vulnerability scanning |
| npm | `npm audit` | Security vulnerability scan |
| Black Duck | `detect` / `scan` | Discover components & vulnerabilities |
| OWASP | `dependency-check --scan` | Vulnerability scan |
| Trivy | `trivy filesystem .` | Vulnerability scan |
| dotnet | `dotnet list package --vulnerable` | Show vulnerable packages |

**Observation:** In the security/DevOps space, `scan` overwhelmingly implies **vulnerability or security scanning**, not dependency discovery. This creates potential user confusion: a user running `smart-skills scan` might expect a security audit, not a dependency listing.

However, in SmartSkills' context, `scan` does two things:
1. **Discovers dependencies** (reads `.csproj`, `package.json`, etc.)
2. **Matches them** against the skill registry

**Alternative verbs to consider:**

| Verb | Pros | Cons |
|------|------|------|
| `scan` (current) | Action-oriented, suggests thoroughness | Ambiguity with security scanning |
| `detect` | Emphasizes discovery, used by Black Duck | Less common as a top-level verb |
| `match` | Accurately describes the registry-matching step | Doesn't capture the dependency-discovery step |
| `check` | Familiar (e.g., `cargo check`) | Vague; could mean many things |

**Recommendation:** `scan` is acceptable. While there is some semantic overlap with security tools, SmartSkills' context (skill discovery, not vulnerability auditing) makes the meaning sufficiently clear in practice. The description "Scan a project or solution for installed packages" is well-worded and disambiguates it. No change needed, but if ever reconsidering, `detect` would be the strongest alternative.

**Design decision:** `scan` was considered for merging into `install --dry-run`, but kept separate because it serves a distinct diagnostic purpose — it exposes the raw dependency graph and match strategies (which packages matched, by what pattern), while `install --dry-run` would only preview which skills get installed. `scan` answers "why/how were skills matched?" while `install --dry-run` answers "what would change?"

---

### 🟡 `status` — **Somewhat Non-standard for Package Managers**

**Verdict: Defensible, but unusual in the package management domain.**

| Tool | Has `status` verb? | What users do instead |
|------|-------------------|----------------------|
| git | ✅ `git status` | Canonical — shows working tree state |
| Helm | ✅ `helm status` | Shows release status |
| kubectl | ✅ `kubectl rollout status` | Shows deployment status |
| npm | ❌ | `npm list`, `npm outdated` |
| pip | ❌ | `pip list`, `pip show`, `pip check` |
| dotnet | ❌ | `dotnet list package`, `--outdated` |
| brew | ❌ | `brew outdated`, `brew info` |
| apt | ❌ | `apt list`, `dpkg -s` |
| cargo | ❌ | `cargo tree`, `cargo update --dry-run` |

**Observation:** Most package managers do **not** have a `status` verb. Instead, they use `list` with flags (`--outdated`, `--vulnerable`) or separate verbs like `outdated`. The `status` verb is most at home in **VCS tools** (Git) and **deployment tools** (Helm, kubectl) where resources have a lifecycle state.

However, SmartSkills skills _do_ have lifecycle states: "up-to-date", "modified", "missing", "update available." This makes `status` semantically appropriate — it's more like Helm's `status` than npm's nonexistent one.

**Recommendation:** `status` is defensible and arguably better than alternatives like `check` or `outdated`, because it provides a holistic view (up-to-date, modified, missing, update-available) rather than just one dimension. Keep it. The `--check-remote` flag is also well-named.

**Alternative considered:** Merging `status` functionality into `list` with flags (e.g., `list --status`, `list --outdated`). However, `list` and `status` serve genuinely different purposes in SmartSkills (lock file entries vs. filesystem state), so keeping them separate is correct.

---

## Global Options Review

### 🟡 `--verbose` (`-v`) — **Acceptable, but diverges from .NET convention**

The .NET CLI ecosystem uses `--verbosity` with enum levels (`quiet`, `minimal`, `normal`, `detailed`, `diagnostic`) rather than a boolean `--verbose`. The [System.CommandLine design guidance](https://learn.microsoft.com/en-us/dotnet/standard/commandline/design-guidance) specifically recommends the `--verbosity` pattern.

However, for a focused tool like SmartSkills that only needs two modes (normal and detailed), a boolean `--verbose` is simpler and more user-friendly. Many popular tools outside .NET (npm, pip, curl) use `--verbose` as a boolean flag.

**Recommendation:** Current approach is pragmatic and fine. If the tool grows in complexity and needs quieter modes (e.g., for CI pipelines), consider migrating to `--verbosity` with at least `quiet`, `normal`, `detailed` levels.

**⚠️ Alias conflict:** The Microsoft design guidance says `-v` should conventionally map to `--verbosity`, not `--verbose`. This is a minor convention mismatch that most users won't notice, but it's worth noting.

---

### 🟡 `--dry-run` — **Standard verb, but questionable as global option**

`--dry-run` is a well-established convention (Git, AWS CLI, Helm). However, best practice is to define it **per-command** rather than globally, because:

1. Not all commands can meaningfully support dry-run (e.g., `scan`, `list`, `status` are already read-only).
2. A global `--dry-run` creates user expectations that it works everywhere.
3. Git, Helm, and AWS all implement `--dry-run` per-command, not globally.

**Current state:** `--dry-run` is declared as `Recursive = true` (global), but only `install` reads and uses the `dryRun` value. `scan`, `list`, `status`, `restore`, and `uninstall` ignore it.

**Recommendation:** Move `--dry-run` to only the commands where it makes sense: `install`, `uninstall`, and potentially `restore`. This prevents user confusion when they run `smart-skills restore --dry-run` and nothing happens differently.

---

### ✅ `--base-dir` — **Clear and Well-named**

Follows kebab-case naming convention. Clearly communicates its purpose. No concerns.

---

### ✅ `--project` (`-p`) — **Standard**

Follows .NET ecosystem conventions. Well-aliased.

**⚠️ Minor note:** The .NET CLI design guidance reserves `-p` for `--property` (MSBuild properties). Since SmartSkills is not wrapping MSBuild commands directly (the CLI), this conflict is minor. Users of the CLI tool are unlikely to confuse it.

---

### ✅ `--json` — **Standard**

Machine-readable output flag. Widely used across modern CLIs (GitHub CLI, Azure CLI, kubectl with `-o json`). Clear and conventional.

---

### ✅ `--recursive` (`-r`) — **Standard**

Well-known flag pattern. Used in `find`, `cp`, `grep`, etc.

**⚠️ Minor note:** The .NET CLI design guidance reserves `-r` for `--runtime`. Same caveat as `-p` above — unlikely to cause real confusion in SmartSkills' context.

---

## Cross-Cutting Observations

### 1. Option Availability Inconsistency

Not all commands support the same options, and the current structure doesn't make this obvious:

| Option | scan | install | uninstall | restore | list | status |
|--------|------|---------|-----------|---------|------|--------|
| `--project` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `--json` | ✅ | ❌ | ❌ | ❌ | ✅ | ✅ |
| `--recursive` | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `--depth` | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `--check-remote` | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| `--dry-run` (global) | ignored | ✅ | ignored | ignored | ignored | ignored |
| `--verbose` (global) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `--base-dir` (global) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

**Issue:** `--json` is available on `scan`, `list`, and `status`, but not on `install`, `restore`, or `uninstall`. For automation/CI use cases, having JSON output on `install` and `restore` would be valuable.

### 2. Missing `--force` Implementation

The CLI README documents `smart-skills install --project ./src/MyProject --force`, but no `--force` option is defined or handled in `Program.cs`. This is a doc/code mismatch.

### 3. Missing `upgrade` Verb?

Many package managers distinguish between `install` (first-time installation) and `upgrade`/`update` (update existing):

| Tool | Upgrade verb |
|------|-------------|
| Helm | `upgrade` |
| pip | `install --upgrade` |
| brew | `upgrade` |
| apt | `upgrade` |
| npm | `update` |

SmartSkills' `install` command currently handles both initial installation and updates (it reports "Installed" and "Updated" separately in its output). This is the same approach as `npm install`, which is reasonable. However, users coming from Helm or brew might look for an `upgrade` command.

**Recommendation:** The current approach (single `install` for both) is fine and matches npm/dotnet conventions. No change needed.

---

## Comparison with Closest Analogues

SmartSkills is most similar to **Helm** (installs "charts"/skills from registries) and **dotnet tool** (installs .NET tools). Here's how the verb set compares:

| Action | SmartSkills | Helm | dotnet tool | npm |
|--------|-------------|------|-------------|-----|
| Discover/scan | `scan` | — | `search` | `audit` |
| Install | `install` | `install` | `install` | `install` |
| Remove | `uninstall` | `uninstall` | `uninstall` | `uninstall` |
| List installed | `list` | `list` | `list` | `list` |
| Check state | `status` | `status` | — | — |
| Restore from lock | `restore` | — | `restore` | `ci` |

SmartSkills' verb set aligns well with the intersection of Helm and dotnet conventions.

---

## Summary & Recommendations

### Overall Assessment: **Good — the verb set is largely standard and intuitive**

The CLI follows CLI design best practices well. The verb choices are familiar to developers from both the .NET and Node.js ecosystems. The design is consistent, uses kebab-case, provides short aliases where appropriate, and the command descriptions are clear.

### Priority Recommendations

| # | Severity | Item | Recommendation |
|---|----------|------|----------------|
| 1 | 🔴 Bug | `--force` documented but not implemented | Implement the option or remove from docs |
| 2 | 🟡 Convention | `--dry-run` is global but only used by `install` | Move to per-command on `install` (and `uninstall`/`restore` if applicable) |
| 3 | 🟡 Feature | `--json` inconsistently available | Add `--json` output to `install` and `restore` for CI/automation |
| 4 | 🟢 Nice-to-have | No `ls` alias for `list` | Add hidden `ls` alias for Unix users |
| 5 | 🟢 Nice-to-have | No `remove` alias for `uninstall` | Add hidden `remove` alias |
| 6 | 🟢 Informational | `--verbose` vs `--verbosity` | Acceptable today; consider `--verbosity` if adding quiet/CI mode later |
| 7 | 🟢 Informational | `-v` and `-p` and `-r` alias conventions | Minor divergence from .NET CLI guidance (which reserves these for `--verbosity`, `--property`, `--runtime`); acceptable for this tool's domain |
| 8 | 🟢 Informational | `scan` verb semantics | Slightly non-standard (implies security scanning elsewhere), but acceptable in context |

### What the CLI Gets Right

- ✅ **Clear, action-oriented verb names** — all commands are verbs, not nouns
- ✅ **Consistent kebab-case naming** for all options (`--dry-run`, `--base-dir`, `--check-remote`)
- ✅ **Well-written descriptions** for every command and option
- ✅ **Lowercase commands** throughout
- ✅ **Logical command grouping** — the flat structure works well for 6 commands
- ✅ **`install`/`restore` distinction** mirrors the well-understood dotnet add/restore pattern
- ✅ **`--json` flag** for machine-readable output on read-only commands
- ✅ **Sensible defaults** (current directory, depth=5)
- ✅ **Argument for `uninstall`** — skill name is correctly a positional argument, not an option

---

## Deep Dive: Error Handling & Robustness

### 🔴 No Structured Exception Handling

Commands have **no try-catch blocks** at the top level. If `installer.InstallAsync()`, `scanner.ScanProjectAsync()`, or any service method throws, the exception propagates as an unhandled crash with a raw stack trace — not a user-friendly error message.

The **only** exception handling in the CLI is in the `status` command's remote check loop:

```csharp
catch
{
    status += " (remote check failed)";
}
```

**Best practice** (per [clig.dev](https://clig.dev) and [Better CLI](https://bettercli.org/design/exit-codes/)): Wrap all command actions in structured error handling. Display human-readable messages to stderr. Show stack traces only when `--verbose` is enabled.

**Recommendation:** Add a top-level try-catch in each command action, or use a shared error-handling wrapper that:
1. Catches known exception types and writes user-friendly messages to `Console.Error`
2. Returns non-zero exit codes on failure
3. Shows full stack traces only with `--verbose`

---

### 🔴 No Custom Exit Codes

The CLI relies entirely on System.CommandLine's default behavior:
- Returns `0` on success
- Returns `1` on argument parsing errors

There are **no custom exit codes** for operational failures. For example, if `install` completes but some skills fail, it still returns `0`. This makes it difficult to use in CI/CD pipelines.

**Best practice:** Define meaningful exit codes and document them:

| Exit Code | Meaning |
|-----------|---------|
| 0 | Success — all operations completed |
| 1 | General error / unhandled exception |
| 2 | Invalid arguments or options |
| 3 | Partial failure — some items failed |
| 4 | Lock file not found / no skills to process |

**Recommendation:** Return a non-zero exit code when `result.Failed.Count > 0` in `install` and `restore`. This is critical for CI/CD integration.

---

### 🔴 All Output Goes to stdout — No stderr Separation

All output (success messages, failure messages, progress info) is written via `Console.WriteLine()` to stdout. Error messages, warnings, and diagnostic info are mixed with normal output.

**Best practice** (per POSIX, GNU, and clig.dev): Normal program output goes to stdout; errors, warnings, and diagnostics go to stderr. This allows users to pipe/redirect output cleanly:

```shell
smart-skills scan --json > skills.json    # errors should NOT pollute JSON
smart-skills install 2> errors.log        # capture only errors
```

**Recommendation:** Use `Console.Error.WriteLine()` for:
- Failure entries ("x skill: reason")
- Warning messages
- Diagnostic/verbose output (when `--verbose` is enabled)

This is especially important when `--json` is used, because errors written to stdout corrupt the JSON.

---

## Deep Dive: Ghost Options & Documentation Mismatches

### 🔴 `--base-dir` Is a Ghost Option

The `--base-dir` option is declared, parsed, and passed into `CreateHost()`, but **`CreateHost()` never uses the value**:

```csharp
static IHost CreateHost(bool verbose, string? baseDir = null, bool suppressLogging = false)
{
    var builder = Host.CreateApplicationBuilder();
    builder.Services.AddSmartSkills();  // baseDir is never passed to DI
    return builder.Build();
}
```

The actual install/restore logic derives its base directory from `projectPath`, completely ignoring `--base-dir`. A user running `smart-skills install --base-dir /custom/path` gets no error but the option is silently ignored.

**Recommendation:** Either wire `--base-dir` into the DI container so it's actually used by `ISkillInstaller`, or remove it from the CLI surface entirely. Silent no-op options are a significant UX anti-pattern.

---

### 🔴 `--force` Documented but Not Implemented

The CLI README shows:

```shell
smart-skills install --project ./src/MyProject --force
```

But no `--force` option exists in `Program.cs`. This is a documentation/code mismatch that will confuse users.

**Recommendation:** Either implement `--force` or remove the example from the README.

---

## Deep Dive: Discoverability & Help UX

### 🟡 Root Command Shows Custom Message Instead of Help

When invoked with no arguments, the root command shows:

```
SmartSkills CLI - Use --help for usage information.
```

While this is better than showing nothing, it adds friction. The user must run the command again with `--help` to see what's available. Most modern CLIs (git, docker, kubectl, dotnet) show the full help output when no subcommand is given.

**Recommendation:** Remove the custom `SetAction` on the root command and let System.CommandLine show the default help output automatically. Or at minimum, print the available subcommands alongside the custom message.

---

### 🟡 No Command Aliases

None of the commands define aliases. Popular CLIs provide shorthand aliases for frequently used commands:

| CLI | Long Form | Alias |
|-----|-----------|-------|
| Helm | `list` | `ls` |
| npm | `install` | `i` |
| Docker | `images` | `image ls` |
| kubectl | `get pods` | (tab-completion) |

Adding hidden aliases like `ls` → `list` and `remove` → `uninstall` would reduce friction for experienced CLI users without cluttering the help output.

---

### 🟡 No Shell Completion Support

System.CommandLine supports generating shell completions (bash, zsh, PowerShell). This is not currently exposed. Shell completion dramatically improves discoverability and reduces errors.

**Recommendation:** Consider adding the `dotnet-suggest` integration or exposing a `complete` subcommand for shell completion setup.

---

## Deep Dive: Automation & Scripting Readiness

### 🟡 `--json` Output Not Available on Mutation Commands

Only read-only commands (`scan`, `list`, `status`) support `--json`. Mutation commands (`install`, `restore`, `uninstall`) output human-readable text only.

For CI/CD pipelines, having structured output from `install` and `restore` is valuable:
- Knowing exactly which skills were installed/updated/failed
- Parsing results programmatically for notifications or dashboards

**Recommendation:** Add `--json` support to `install` and `restore`. The data structures are already there (`result.Installed`, `result.Updated`, `result.Failed`).

---

### 🟡 No `--quiet` / `-q` Mode

There is no way to suppress all output. In CI pipelines, users often want silent operation unless something fails. The `--verbose` flag adds detail, but there's no inverse.

**Best practice** (per System.CommandLine design guidance): If supporting verbosity, support at least `quiet`, `normal`, and `detailed` levels.

**Recommendation:** Add a `--quiet` / `-q` flag (or evolve `--verbose` into `--verbosity` with levels) for CI/script use cases.

---

## Comprehensive Issue Tracker

| # | Severity | Category | Item | Recommendation |
|---|----------|----------|------|----------------|
| 1 | 🔴 Bug | Code | `--base-dir` parsed but never used | Wire into DI or remove from CLI surface |
| 2 | 🔴 Bug | Documentation | `--force` documented but not implemented | Implement or remove from README |
| 3 | 🔴 Design | Error handling | No try-catch; exceptions crash with stack traces | Add structured error handling wrapper |
| 4 | 🔴 Design | Exit codes | No custom exit codes; failures return 0 | Return non-zero on operational failures |
| 5 | 🔴 Design | Output streams | Errors written to stdout, not stderr | Use `Console.Error` for errors/warnings |
| 6 | 🟡 Convention | Global options | `--dry-run` global but only used by `install` | Move to per-command on `install`/`uninstall`/`restore` |
| 7 | 🟡 Feature | Output | `--json` not available on `install`/`restore` | Add JSON output for CI/CD automation |
| 8 | 🟡 UX | Help | Root command shows stub message, not help | Show full help or subcommand list |
| 9 | 🟡 UX | Quiet mode | No `--quiet`/`-q` for silent operation | Add quiet mode for CI pipelines |
| 10 | 🟢 Nice-to-have | Aliases | No `ls` alias for `list` | Add hidden alias for Unix users |
| 11 | 🟢 Nice-to-have | Aliases | No `remove` alias for `uninstall` | Add hidden alias |
| 12 | 🟢 Nice-to-have | Completions | No shell completion support | Integrate `dotnet-suggest` |
| 13 | 🟢 Informational | Convention | `--verbose` vs `--verbosity` | Acceptable today; evolve if adding quiet mode |
| 14 | 🟢 Informational | Convention | `-v`, `-p`, `-r` alias reservations | Minor divergence from .NET CLI guidance |
| 15 | 🟢 Informational | Verb | `scan` implies security scanning elsewhere | Acceptable in context; `detect` is strongest alternative |
