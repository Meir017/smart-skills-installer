# PRD Template

Use this template when creating a new Product Requirements Document. It mirrors the structure used by the existing PRD files in this repository (`PRD.md`, `PRD.json`, `api.cs`).

---

## How to Use

**Preferred approach — use the `prd-creator` skill** to scaffold PRDs via script functions. See `.agents/skills/prd-creator/SKILL.md` for details.

Manual approach:
1. Create a subdirectory under `spec/` with a kebab-case slug (e.g. `spec/my-feature/`)
2. Create `PRD.json` in that directory (required — machine-readable source of truth)
3. Optionally create `PRD.md` for a human-readable narrative and `api.cs` for the public API surface

---

## 1. Project Name & Description

> **Name:** _Your project name_
>
> **Version:** _1.0.0_
>
> **Description:** _A concise paragraph describing what the project/feature does, why it exists, and the primary mechanism of delivery._

## 2. Goals

List the high-level objectives this project aims to achieve.

1. _Goal 1_
2. _Goal 2_
3. _Goal 3_

## 3. Stories & Tasks

Organize work into **stories** (user-facing capabilities) and **tasks** (implementation steps within each story). Each story groups related tasks under a single theme.

### S01: _Story Title_

_Brief description of the story — what capability does it deliver?_

- **S01-T01**: _Task Title_
  _Detailed description of what this task accomplishes._
  - **Requirements:**
    - _Requirement 1_
    - _Requirement 2_
  - **Definition of Done:** _What must be true for this task to be considered complete._
  - **Verifications:**
    - _How to verify this task is done (e.g. commands to run, expected output)_
    - _Another verification step_

- **S01-T02**: _Task Title_
  _Description..._
  - **Requirements:**
    - _..._
  - **Definition of Done:** _..._
  - **Verifications:**
    - _..._

### S02: _Story Title_

_Description..._

- **S02-T01**: _Task Title_
  _Description..._
  - **Requirements:**
    - _..._
  - **Definition of Done:** _..._
  - **Verifications:**
    - _..._

_Repeat for additional stories..._

## 4. API Surface (optional)

If the project exposes a public API, create a companion `api.cs` file that sketches out the interfaces, records, and key types. This file is for **design review only** — it is not compiled.

Structure the file by story region:

```csharp
// ProjectName Public API Surface
// This file is for design review only — not compiled.

namespace MyProject;

#region S01: Story Title

public interface IMyInterface
{
    Task DoSomethingAsync(CancellationToken cancellationToken = default);
}

public record MyRecord
{
    public required string Name { get; init; }
}

#endregion
```

## 5. Machine-Readable PRD (required)

The `PRD.json` file is the machine-readable source of truth. Use the `prd-creator` skill to generate it, or follow this schema:

```json
{
  "name": "Project Name",
  "version": "1.0.0",
  "description": "Project description",
  "apiSurface": "api.cs",
  "goals": [
    "Goal 1",
    "Goal 2"
  ],
  "stories": [
    {
      "id": "S01",
      "title": "Story Title",
      "description": "Story description",
      "tasks": [
        {
          "id": "S01-T01",
          "title": "Task Title",
          "completed": false,
          "description": "Task description",
          "requirements": [
            "Requirement 1"
          ],
          "dod": "Definition of done",
          "verifications": [
            "Verification step 1"
          ]
        }
      ]
    }
  ]
}
```

## 6. Conventions

- All PRDs live in a **dedicated subdirectory** under `spec/` with a kebab-case slug (e.g. `spec/my-feature/`)
- **PRD.json is required** in each spec subdirectory — it is the machine-readable source of truth
- **PRD.md is optional** — a longer narrative form for complex specs
- **api.cs is optional** — a design-review-only API sketch for .NET features
- **Story IDs** use the format `S01`, `S02`, etc.
- **Task IDs** use the format `S01-T01`, `S01-T02`, etc.
- Each task should have **requirements**, a **definition of done**, and **verifications** so progress can be tracked objectively.
- The `PRD.json` `completed` field on each task is updated as implementation progresses.
- The `api.cs` file uses `#region` blocks matching story IDs to keep the API surface organized by feature area.
