---
name: prd-creator
description: Create and manage PRD documents. Use when creating, updating, or querying stories and tasks in a PRD.json file.
---

# PRD Creator Skill

This skill helps manage Product Requirements Documents stored as `PRD.json` files. It provides a PowerShell utility script (`prd-utils.ps1`) with functions for creating, reading, writing, and modifying stories and tasks.

**Key principle:** Use script functions to build PRD content — never write JSON manually. Call PowerShell functions with named parameters and the script handles all serialization.

## Setup

Dot-source the utility script in your PowerShell session:

```powershell
. .agents\skills\prd-creator\prd-utils.ps1
```

## Spec Directory Conventions

All PRDs live under `spec/` in a dedicated subdirectory named with a kebab-case slug:

```
spec/
  <feature-slug>/          # One directory per feature/PRD
    PRD.json               # Machine-readable PRD (required)
    PRD.md                 # Human-readable narrative (optional)
    api.cs                 # API surface sketch (optional, .NET projects)
  PRD.template.md          # Template reference (stays at spec root)
```

Rules:
- **One subdirectory per feature** — never place PRD files at the `spec/` root
- **Directory name** is a kebab-case slug matching the feature (e.g. `multi-language-python`, `lock-file`, `cli-design-gaps`)
- **PRD.json is required** — this is the machine-readable source of truth
- **PRD.md is optional** — a longer narrative form, useful for complex specs
- **api.cs is optional** — a design-review-only API sketch for .NET features
- **Story IDs** use the format `S01`, `S02`, etc.
- **Task IDs** use the format `S01-T01`, `S01-T02`, etc.

## Available Functions

### Creation & I/O

| Function | Description |
|---|---|
| `New-Prd -Name -Description [-Goals] [-Version] [-ApiSurface]` | Create a new in-memory PRD object |
| `Initialize-PrdDirectory <prd> -Slug [-BaseDir "spec"]` | Create `spec/<slug>/PRD.json` on disk |
| `Read-Prd <path>` | Load a PRD.json file into memory |
| `Write-Prd <prd> <path>` | Save the PRD object back to disk |

### Stories

| Function | Description |
|---|---|
| `Get-PrdStory <prd> -StoryId <id>` | Retrieve a story by ID |
| `Add-PrdStory <prd> -Id -Title -Description` | Add a new story |
| `Set-PrdStory <prd> -StoryId [-Title] [-Description]` | Modify an existing story |
| `Remove-PrdStory <prd> -StoryId` | Remove a story and its tasks |

### Tasks

| Function | Description |
|---|---|
| `Get-PrdTask <prd> -TaskId <id>` | Retrieve a task by ID (searches all stories) |
| `Add-PrdTask <prd> -StoryId -Id -Title -Description [-Requirements] [-Dod] [-Verifications] [-ApiReferences]` | Add a task to a story |
| `Set-PrdTask <prd> -TaskId [-Title] [-Description] [-Requirements] [-Dod] [-Verifications] [-Completed]` | Modify an existing task |
| `Set-PrdTaskCompleted <prd> -TaskId [-Completed $true]` | Mark a task as completed or not |
| `Remove-PrdTask <prd> -TaskId` | Remove a task from its story |
| `Move-PrdTask <prd> -TaskId -TargetStoryId` | Move a task to a different story |

### Queries

| Function | Description |
|---|---|
| `Get-PrdSummary <prd>` | Print a summary (story count, task progress) |
| `Get-PrdPendingTasks <prd>` | List all incomplete tasks |

## Workflow: Creating a New PRD

Use script functions to build the PRD step by step — no manual JSON needed:

```powershell
# 1. Create the PRD object
$prd = New-Prd -Name "My Feature" `
    -Description "What this feature does and why" `
    -Goals @("Goal 1", "Goal 2", "Goal 3")

# 2. Add stories and tasks
Add-PrdStory $prd -Id "S01" -Title "Core Implementation" `
    -Description "Build the main functionality"

Add-PrdTask $prd -StoryId "S01" -Id "S01-T01" -Title "Implement widget" `
    -Description "Build the widget component" `
    -Requirements @("Must support X", "Must handle Y") `
    -Dod "Widget renders correctly" `
    -Verifications @("Unit tests pass", "Manual smoke test")

Add-PrdTask $prd -StoryId "S01" -Id "S01-T02" -Title "Add tests" `
    -Description "Write unit tests for the widget" `
    -Requirements @("Cover edge cases") `
    -Dod "All tests pass" `
    -Verifications @("dotnet test passes")

# 3. Save to spec/<slug>/PRD.json
Initialize-PrdDirectory $prd -Slug "my-feature"
```

## Workflow: Updating an Existing PRD

```powershell
# Load
$prd = Read-Prd "spec\my-feature\PRD.json"

# View status
Get-PrdSummary $prd
Get-PrdPendingTasks $prd

# Add more content
Add-PrdStory $prd -Id "S02" -Title "Testing" -Description "Comprehensive tests"
Add-PrdTask $prd -StoryId "S02" -Id "S02-T01" -Title "Integration tests" `
    -Description "End-to-end test coverage" `
    -Requirements @("Test happy path", "Test error cases") `
    -Dod "All tests green" `
    -Verifications @("dotnet test --filter Integration passes")

# Mark tasks done
Set-PrdTaskCompleted $prd -TaskId "S01-T01"

# Save
Write-Prd $prd "spec\my-feature\PRD.json"
```

## PRD.json Schema Reference

See `spec/PRD.template.md` for the full schema documentation. Key fields per task: `id`, `title`, `completed`, `description`, `requirements`, `dod`, `verifications`, and optional `apiReferences`.