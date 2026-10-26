# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## TASK-003: SingleStepTests oracle data fetch

| Field | Value |
|---|---|
| Status | **Ready** (approved, not started) |
| Owner | — (unassigned. The next agent sets itself as owner when starting) |
| Started | — |
| Definition | `docs/05-planning/backlog.md` § TASK-003 (goal, scope, tests, DoD) |
| Procedure | `docs/sdlc/procedures/assembly-implement.md` (invoke with `/assembly-implement TASK-003`) |
| Depends on | TASK-001 (Done ✔). ADR-0008 approved ✔ |

**Objective:** Fetch, verify and cache the external SingleStepTests 8086 oracle data so engine tests can run offline.

**Acceptance criteria:** `build/fetch-singlesteptests.ps1` runs idempotently, verifies SHA-256 / commit pin, caches data in `testdata/singlesteptests/` (git-ignored), and generates `testdata/singlesteptests/SOURCE.txt` with commit, source URL and license. Missing data produces a clear error. Standard DoD (backlog §4).

**Validation performed:** none yet.
**Remaining work:** all of it. **Parallel user action:** TASK-006 (clean Windows 11 24H2 and Windows 10 Enterprise LTSC 21H2 VMs) can start any time.

---

## Last completed: TASK-002, Architecture rule test (Done 2026-10-26, Antigravity)
- Created `tests/AssemblyStudio.ArchitectureTests/` with `ArchitectureRuleTests.cs`.
- Enforces ADR-0002 dependency graph rules using `System.Reflection.Metadata` and `PEReader`.
- Verified non-App assemblies do not reference UI frameworks (`PresentationFramework`, `PresentationCore`, `WindowsBase`, `AvalonEdit`, `System.Windows.Forms`).
- Verified non-App assemblies never reference `AssemblyStudio.App`.
- Verified `AssemblyStudio.Debugging` never references concrete engine, runtime, OS, or toolchain assemblies.
- Verified deliberate forbidden reference makes test fail (exit code 2, 2 errors), and removal returns tests to green (31/31 passed across solution).
