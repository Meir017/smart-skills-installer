# PRD.json Management Utilities
# Provides functions for creating, reading, writing, and modifying PRD.json files.
#
# Quick start — create a new PRD:
#   . .agents\skills\prd-creator\prd-utils.ps1
#   $prd = New-Prd -Name "My Feature" -Description "What it does" -Goals @("Goal 1","Goal 2")
#   Add-PrdStory $prd -Id "S01" -Title "First Story" -Description "Story description"
#   Add-PrdTask $prd -StoryId "S01" -Id "S01-T01" -Title "First task" `
#       -Description "Do the thing" -Requirements @("Req 1") -Dod "It works" -Verifications @("Test it")
#   Initialize-PrdDirectory $prd -Slug "my-feature"
#
# Work with an existing PRD:
#   $prd = Read-Prd "spec\my-feature\PRD.json"
#   Get-PrdSummary $prd
#   Set-PrdTaskCompleted $prd -TaskId "S01-T01"
#   Write-Prd $prd "spec\my-feature\PRD.json"

# --- Creation & I/O ---

function New-Prd {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Description,
        [string[]]$Goals = @(),
        [string]$Version = "1.0.0",
        [string]$ApiSurface
    )
    $prd = [ordered]@{
        name        = $Name
        version     = $Version
        description = $Description
        goals       = @($Goals)
        stories     = @()
    }
    if ($ApiSurface) { $prd.apiSurface = $ApiSurface }
    Write-Host "Created PRD '$Name' v$Version"
    return [PSCustomObject]$prd
}

function Initialize-PrdDirectory {
    param(
        [Parameter(Mandatory)][object]$Prd,
        [Parameter(Mandatory)][string]$Slug,
        [string]$BaseDir = "spec"
    )
    $dir = Join-Path $BaseDir $Slug
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    $path = Join-Path $dir "PRD.json"
    Write-Prd $Prd $path
    Write-Host "Initialized PRD directory at '$dir' with PRD.json"
    return $path
}

function Read-Prd {
    param(
        [Parameter(Mandatory)][string]$Path
    )
    $content = Get-Content -Path $Path -Raw -Encoding utf8
    # -Depth is only available in PowerShell 7+; omit for compatibility
    return $content | ConvertFrom-Json
}

function Write-Prd {
    param(
        [Parameter(Mandatory)][object]$Prd,
        [Parameter(Mandatory)][string]$Path
    )
    $Prd | ConvertTo-Json -Depth 20 | Set-Content -Path $Path -Encoding utf8
}

# --- Story Functions ---

function Get-PrdStory {
    param(
        [Parameter(Mandatory)][object]$Prd,
        [Parameter(Mandatory)][string]$StoryId
    )
    return $Prd.stories | Where-Object { $_.id -eq $StoryId }
}

function Add-PrdStory {
    param(
        [Parameter(Mandatory)][object]$Prd,
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Description
    )
    $existing = Get-PrdStory $Prd $Id
    if ($existing) {
        Write-Error "Story '$Id' already exists."
        return
    }
    $story = [ordered]@{
        id          = $Id
        title       = $Title
        description = $Description
        tasks       = @()
    }
    $Prd.stories = @($Prd.stories) + $story
    Write-Host "Added story '${Id}: ${Title}'"
}

function Set-PrdStory {
    param(
        [Parameter(Mandatory)][object]$Prd,
        [Parameter(Mandatory)][string]$StoryId,
        [string]$Title,
        [string]$Description
    )
    $story = Get-PrdStory $Prd $StoryId
    if (-not $story) {
        Write-Error "Story '$StoryId' not found."
        return
    }
    if ($Title) { $story.title = $Title }
    if ($Description) { $story.description = $Description }
    Write-Host "Updated story '$StoryId'"
}

function Remove-PrdStory {
    param(
        [Parameter(Mandatory)][object]$Prd,
        [Parameter(Mandatory)][string]$StoryId
    )
    $story = Get-PrdStory $Prd $StoryId
    if (-not $story) {
        Write-Error "Story '$StoryId' not found."
        return
    }
    $Prd.stories = @($Prd.stories | Where-Object { $_.id -ne $StoryId })
    Write-Host "Removed story '$StoryId'"
}

# --- Task Functions ---

function Get-PrdTask {
    param(
        [Parameter(Mandatory)][object]$Prd,
        [Parameter(Mandatory)][string]$TaskId
    )
    foreach ($story in $Prd.stories) {
        $task = $story.tasks | Where-Object { $_.id -eq $TaskId }
        if ($task) { return $task }
    }
    return $null
}

function Add-PrdTask {
    param(
        [Parameter(Mandatory)][object]$Prd,
        [Parameter(Mandatory)][string]$StoryId,
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Description,
        [string[]]$Requirements = @(),
        [string]$Dod = "",
        [string[]]$Verifications = @(),
        [string[]]$ApiReferences = @(),
        [bool]$Completed = $false
    )
    $story = Get-PrdStory $Prd $StoryId
    if (-not $story) {
        Write-Error "Story '$StoryId' not found."
        return
    }
    $existing = $story.tasks | Where-Object { $_.id -eq $Id }
    if ($existing) {
        Write-Error "Task '$Id' already exists in story '$StoryId'."
        return
    }
    $task = [ordered]@{
        id            = $Id
        title         = $Title
        completed     = $Completed
        description   = $Description
        requirements  = @($Requirements)
        dod           = $Dod
        verifications = @($Verifications)
    }
    if ($ApiReferences.Count -gt 0) {
        $task.apiReferences = @($ApiReferences)
    }
    $story.tasks = @($story.tasks) + $task
    Write-Host "Added task '${Id}: ${Title}' to story '${StoryId}'"
}

function Set-PrdTask {
    param(
        [Parameter(Mandatory)][object]$Prd,
        [Parameter(Mandatory)][string]$TaskId,
        [string]$Title,
        [string]$Description,
        [string[]]$Requirements,
        [string]$Dod,
        [string[]]$Verifications,
        [string[]]$ApiReferences,
        [Nullable[bool]]$Completed
    )
    $task = Get-PrdTask $Prd $TaskId
    if (-not $task) {
        Write-Error "Task '$TaskId' not found."
        return
    }
    if ($Title) { $task.title = $Title }
    if ($Description) { $task.description = $Description }
    if ($null -ne $Requirements) { $task.requirements = @($Requirements) }
    if ($Dod) { $task.dod = $Dod }
    if ($null -ne $Verifications) { $task.verifications = @($Verifications) }
    if ($null -ne $ApiReferences) { $task.apiReferences = @($ApiReferences) }
    if ($null -ne $Completed) { $task.completed = $Completed }
    Write-Host "Updated task '$TaskId'"
}

function Set-PrdTaskCompleted {
    param(
        [Parameter(Mandatory)][object]$Prd,
        [Parameter(Mandatory)][string]$TaskId,
        [bool]$Completed = $true
    )
    Set-PrdTask $Prd -TaskId $TaskId -Completed $Completed
}

function Remove-PrdTask {
    param(
        [Parameter(Mandatory)][object]$Prd,
        [Parameter(Mandatory)][string]$TaskId
    )
    foreach ($story in $Prd.stories) {
        $match = $story.tasks | Where-Object { $_.id -eq $TaskId }
        if ($match) {
            $story.tasks = @($story.tasks | Where-Object { $_.id -ne $TaskId })
            Write-Host "Removed task '$TaskId' from story '$($story.id)'"
            return
        }
    }
    Write-Error "Task '$TaskId' not found in any story."
}

function Move-PrdTask {
    param(
        [Parameter(Mandatory)][object]$Prd,
        [Parameter(Mandatory)][string]$TaskId,
        [Parameter(Mandatory)][string]$TargetStoryId
    )
    $targetStory = Get-PrdStory $Prd $TargetStoryId
    if (-not $targetStory) {
        Write-Error "Target story '$TargetStoryId' not found."
        return
    }
    $taskObj = $null
    foreach ($story in $Prd.stories) {
        $match = $story.tasks | Where-Object { $_.id -eq $TaskId }
        if ($match) {
            $taskObj = $match
            $story.tasks = @($story.tasks | Where-Object { $_.id -ne $TaskId })
            break
        }
    }
    if (-not $taskObj) {
        Write-Error "Task '$TaskId' not found."
        return
    }
    $targetStory.tasks = @($targetStory.tasks) + $taskObj
    Write-Host "Moved task '$TaskId' to story '$TargetStoryId'"
}

# --- Query Helpers ---

function Get-PrdSummary {
    param(
        [Parameter(Mandatory)][object]$Prd
    )
    $totalTasks = 0
    $completedTasks = 0
    foreach ($story in $Prd.stories) {
        foreach ($task in $story.tasks) {
            $totalTasks++
            if ($task.completed) { $completedTasks++ }
        }
    }
    Write-Host "PRD: $($Prd.name) v$($Prd.version)"
    Write-Host "Stories: $($Prd.stories.Count)"
    Write-Host "Tasks: $completedTasks / $totalTasks completed"
}

function Get-PrdPendingTasks {
    param(
        [Parameter(Mandatory)][object]$Prd
    )
    foreach ($story in $Prd.stories) {
        foreach ($task in $story.tasks) {
            if (-not $task.completed) {
                [PSCustomObject]@{
                    StoryId = $story.id
                    TaskId  = $task.id
                    Title   = $task.title
                }
            }
        }
    }
}
