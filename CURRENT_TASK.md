# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## TASK-002: Architecture rule test

| Field | Value |
|---|---|
| Status | **Ready** (approved, not started) |
| Owner | — (unassigned. The next agent sets itself as owner when starting) |
| Started | — |
| Definition | `docs/05-planning/backlog.md` § TASK-002 (goal, scope, tests, DoD) |
| Procedure | `docs/sdlc/procedures/assembly-implement.md` (invoke with `/assembly-implement TASK-002`) |
| Depends on | TASK-001 (Done ✔). ADR-0002 approved ✔ |

**Objective:** Make the dependency rules of ADR-0002 a failing test, not a convention. Test loads every product assembly and asserts allowed references (graph from ADR-0002) and asserts no assembly except App references PresentationFramework, WindowsBase or AvalonEdit.

**Acceptance criteria:** Rule test passes on the skeleton; deliberately added forbidden reference makes it fail (then removed). Standard DoD (backlog §4).

**Validation performed:** none yet.
**Remaining work:** all of it. **Parallel user action:** TASK-006 (clean Windows 11 24H2 and Windows 10 Enterprise LTSC 21H2 VMs) can start any time.

---

## Last completed: TASK-001, Solution skeleton and build settings (Done 2026-10-26, Antigravity)
- Solution skeleton created: `AssemblyStudio.slnx` with 8 product projects in `src/` and 8 test projects in `tests/`.
- Root `global.json` pinning SDK 10.0.403 with Microsoft.Testing.Platform runner.
- `Directory.Build.props` (net10.0, nullable enable, implicit usings, treat warnings as errors, latest-recommended analyzers, deterministic).
- `Directory.Packages.props` (central package management, xunit.v3 4.0.2).
- `.editorconfig` (coding conventions).
- Validation: `dotnet build AssemblyStudio.slnx -c Release` → 0 warnings, 0 errors. `dotnet test --solution AssemblyStudio.slnx -c Release` → 8/8 passed.
