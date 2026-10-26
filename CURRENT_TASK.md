# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## TASK-001: Solution skeleton and build settings

| Field | Value |
|---|---|
| Status | **Ready** (approved, not started) |
| Owner | — (unassigned. The next agent sets itself as owner when starting) |
| Started | — |
| Definition | `docs/05-planning/backlog.md` § TASK-001 (goal, scope, tests, DoD) |
| Procedure | `docs/sdlc/procedures/assembly-implement.md` (invoke with `/assembly-implement TASK-001`) |
| Depends on | — (first task). ADR-0001, ADR-0002, ADR-0008 approved ✔. .NET SDK 10.0.403 ✔ |

**Objective:** create `AssemblyStudio.slnx` with all projects from ADR-0002 (empty), one test project per production project, shared build settings,
central package versions, root `global.json` (SDK pin + Microsoft.Testing.Platform runner) and `.editorconfig`, with trivial tests passing.

**Acceptance criteria:** `dotnet build -c Release` → 0 warnings. `dotnet test --solution AssemblyStudio.slnx` → all trivial tests pass. Standard DoD (backlog §4).

**Validation performed:** none yet.
**Remaining work:** all of it. **Parallel user action:** TASK-006 (clean Windows 11 24H2 and Windows 10 Enterprise LTSC 21H2 VMs) can start any time.

---

## Last completed: SDLC-008, Implementation plan (Done 2026-10-26, Claude Code)
- Backlog v1.0 **Approved** 2026-10-26 19:17 +0500 ("approve backlog v0.1"): 8 milestones, 62 tasks (3 user actions), all `Approved`.
- Validation: plan checks OK (77/77 P0, 40/40 P1 covered, acyclic, milestone order, unverified gating). Traceability v2.3 (Tasks column). Validators ALL OK.
