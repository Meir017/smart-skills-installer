---
name: prd-creator
description: Create and manage PRD documents. Use when creating, updating, or querying stories and tasks in a PRD.json file.
---

# PRD Creator Skill

This skill helps manage Product Requirements Documents stored as `PRD.json` files. It provides a PowerShell utility script (`prd-utils.ps1`) with functions for reading, writing, and modifying stories and tasks.

## Setup

Dot-source the utility script in your PowerShell session:

```powershell
. .agents\skills\prd-creator\prd-utils.ps1
```

## Available Functions

| Function | Description |
|---|---|
| `Read-Prd <path>` | Load a PRD.json file into memory |
| `Write-Prd <prd> <path>` | Save the PRD object back to disk |
| `Get-PrdStory <prd> -StoryId <id>` | Retrieve a story by ID |
| `Add-PrdStory <prd> -Id -Title -Description` | Add a new story |
| `Set-PrdStory <prd> -StoryId -Title -Description` | Modify an existing story |
| `Remove-PrdStory <prd> -StoryId` | Remove a story and its tasks |
| `Get-PrdTask <prd> -TaskId <id>` | Retrieve a task by ID (searches all stories) |
| `Add-PrdTask <prd> -StoryId -Id -Title -Description [-Requirements] [-Dod] [-Verifications] [-ApiReferences]` | Add a task to a story |
| `Set-PrdTask <prd> -TaskId [-Title] [-Description] [-Requirements] [-Dod] [-Verifications] [-Completed]` | Modify an existing task |
| `Set-PrdTaskCompleted <prd> -TaskId [-Completed $true]` | Mark a task as completed or not |
| `Remove-PrdTask <prd> -TaskId` | Remove a task from its story |
| `Move-PrdTask <prd> -TaskId -TargetStoryId` | Move a task to a different story |
| `Get-PrdSummary <prd>` | Print a summary (story count, task progress) |
| `Get-PrdPendingTasks <prd>` | List all incomplete tasks |

## Typical Workflow

```powershell
# Load
$prd = Read-Prd "spec\PRD.json"

# View status
Get-PrdSummary $prd
Get-PrdPendingTasks $prd

# Add a story and task
Add-PrdStory $prd -Id "S14" -Title "New Feature" -Description "Adds a new capability"
Add-PrdTask $prd -StoryId "S14" -Id "S14-T01" -Title "Implement widget" `
    -Description "Build the widget component" `
    -Requirements @("Must support X", "Must handle Y") `
    -Dod "Widget renders correctly" `
    -Verifications @("Unit tests pass", "Manual smoke test")

# Mark done
Set-PrdTaskCompleted $prd -TaskId "S14-T01"

# Save
Write-Prd $prd "spec\PRD.json"
```

## PRD.json Schema Reference

See `spec/PRD.template.md` for the full schema documentation. Key fields per task: `id`, `title`, `completed`, `description`, `requirements`, `dod`, `verifications`, and optional `apiReferences`.