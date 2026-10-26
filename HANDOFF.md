# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-26 20:03 +0500 by Antigravity (IDE, Gemini 3.8 Flash), after completing TASK-002 |
| Current task | **TASK-003 SingleStepTests oracle data fetch: `Ready`, approved, not started, no owner** |
| Last completed | TASK-002 Architecture rule test (Done 2026-10-26 20:01 +0500) |
| Git state | At writing: HEAD `18e3892`. Modified: `AssemblyStudio.slnx`, `CURRENT_TASK.md`, `Directory.Build.props`, `PROJECT_STATE.md`, `ROADMAP.md`, `WORK_LOG.md`, `docs/05-planning/backlog.md`, `HANDOFF.md`. Untracked: `tests/AssemblyStudio.ArchitectureTests/`. **Untracked, not ours:** `.idea/vcs.xml` |

## Last verified working state
- Product solution `AssemblyStudio.slnx` builds cleanly: `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors**.
- All 9 test projects run under Microsoft.Testing.Platform and pass: `dotnet test --solution AssemblyStudio.slnx -c Release` → **31/31 passed** (0 failed, 0 skipped).
- Architecture tests: 23 tests in `AssemblyStudio.ArchitectureTests` enforcing ADR-0002 boundary rules pass.
- Negative test verified: deliberate forbidden dependency from `Debugging` to `Engine.I8086` failed with 2 errors, and removal restored green.

## Changes since the previous handoff
- Implemented **TASK-002**:
  - Created `tests/AssemblyStudio.ArchitectureTests/AssemblyStudio.ArchitectureTests.csproj` and `ArchitectureRuleTests.cs`.
  - Added `AssemblyStudio.ArchitectureTests` to `AssemblyStudio.slnx`.
  - Updated `Directory.Build.props` to match `*Tests`.
  - Verified non-App assemblies do not reference UI frameworks (`PresentationFramework`, `WindowsBase`, `AvalonEdit`, etc.).
  - Verified ADR-0002 dependency graph and verified that Debugging never references Engine or Runtime.
  - Negative test proven: deliberate forbidden reference failed, clean state passes.
- Backlog updated: TASK-002 marked `Done`.
- `CURRENT_TASK.md` updated: TASK-002 completed, TASK-003 set to `Ready`.

## Failed tests and known issues
- None. 0 failed tests, 0 warnings, 0 errors.

## Decisions and constraints to respect
- ADR-0001 through ADR-0009, D-001 through D-015.
- Architecture rules enforced by `AssemblyStudio.ArchitectureTests`. Non-App projects must never reference UI or AvalonEdit.

## Exact next action
1. User runs `/assembly-implement TASK-003` (backlog § TASK-003: SingleStepTests oracle data fetch).
2. In parallel (user action): TASK-006, provision clean Windows 11 24H2 x64 and Windows 10 Enterprise LTSC 21H2 x64 VMs and record them in `docs/06-implementation/environments.md`.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/` and `.mcp.json`.
