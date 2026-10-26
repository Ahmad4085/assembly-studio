# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-26 22:35 +0500 by Antigravity (IDE, Gemini 3.8 Flash), after completing TASK-003 |
| Current task | **TASK-004 Coverage tooling (coverlet with MTP): `Ready`, approved, not started, no owner** |
| Last completed | TASK-003 SingleStepTests oracle data fetch (Done 2026-10-26 20:18 +0500) |
| Git state | At writing: HEAD `df5291c` (committed `TASK-003: SingleStepTests oracle data fetch`, pushed to `origin/master`). Working tree clean. **Untracked, not ours:** `.idea/vcs.xml` |

## Last verified working state
- Product solution `AssemblyStudio.slnx` builds cleanly: `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors**.
- All 9 test projects run under Microsoft.Testing.Platform and pass: `dotnet test --solution AssemblyStudio.slnx -c Release` → **37/37 passed** (0 failed, 0 skipped).
- SingleStepTests 8086 hardware oracle dataset: 324 files verified and checksummed via `SHA256SUMS.txt` in `testdata/singlesteptests/8086/` (git-ignored per ADR-0008). Idempotence verified.
- `SingleStepOracleHelper` passes all tests, including failing with clear instructions when test data is absent.

## Changes since the previous handoff
- Implemented **TASK-003**:
  - Created `build/fetch-singlesteptests.ps1` with SHA-256 generation, `SOURCE.txt` (MIT license, commit `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`), and `-Include8088` switch.
  - Added `build/singlesteptests-8086-v1-files.txt` with exact 324 file list.
  - Added `testdata/` and `tools/nasm/` to `.gitignore`.
  - Created `SingleStepOracleHelper.cs` and unit tests in `tests/AssemblyStudio.Engine.I8086.Tests/Oracle/`.
- Backlog updated: TASK-003 marked `Done`.
- `CURRENT_TASK.md` updated: TASK-003 completed, TASK-004 set to `Ready`.

## Failed tests and known issues
- None. 0 failed tests, 0 warnings, 0 errors.

## Decisions and constraints to respect
- ADR-0001 through ADR-0009, D-001 through D-015.
- Hardware oracle data stays out of Git (`testdata/` is git-ignored).

## Exact next action
1. User runs `/assembly-implement TASK-004` (backlog § TASK-004: Coverage tooling with coverlet and MTP).
2. In parallel (user action): TASK-006, provision clean Windows 11 24H2 x64 and Windows 10 Enterprise LTSC 21H2 x64 VMs and record them in `docs/06-implementation/environments.md`.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/` and `.mcp.json`.
