# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-26 19:45 +0500 by Antigravity (IDE, Gemini 3.8 Flash), after completing TASK-001 |
| Current task | **TASK-002 Architecture rule test: `Ready`, approved, not started, no owner** |
| Last completed | TASK-001 Solution skeleton and build settings (Done 2026-10-26 19:42 +0500) |
| Git state | At writing: HEAD `4faf215` (committed `TASK-001: Solution skeleton and build settings`, pushed to `origin/master` at `https://github.com/Ahmad4085/assembly-studio`). Working tree clean. **Untracked, not ours:** `.idea/vcs.xml` |

## Last verified working state
- Product solution `AssemblyStudio.slnx` builds cleanly: `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors**.
- All 8 test projects run under Microsoft.Testing.Platform and pass: `dotnet test --solution AssemblyStudio.slnx -c Release` → **8/8 passed** (0 failed, 0 skipped).
- Target framework: `net10.0` for core/engine/runtime/toolchain/os/content/debugging and test projects; `net10.0-windows` with WPF for `AssemblyStudio.App` and `AssemblyStudio.App.Tests`.
- Central Package Management active via `Directory.Packages.props`. `xunit.v3` pinned to `4.0.2`.
- Root `global.json` pins SDK `10.0.403` and sets `test.runner = Microsoft.Testing.Platform`.

## Changes since the previous handoff
- Implemented **TASK-001**:
  - Created root `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, and `AssemblyStudio.slnx`.
  - Created 8 product projects in `src/`: `AssemblyStudio.Core`, `AssemblyStudio.Engine.I8086`, `AssemblyStudio.Runtime`, `AssemblyStudio.Os.Dos`, `AssemblyStudio.Toolchain.Nasm`, `AssemblyStudio.Debugging`, `AssemblyStudio.Content`, `AssemblyStudio.App` (WPF shell with `App.xaml`/`App.xaml.cs`).
  - Created 8 test projects in `tests/`: `AssemblyStudio.Core.Tests`, `AssemblyStudio.Engine.I8086.Tests`, `AssemblyStudio.Runtime.Tests`, `AssemblyStudio.Os.Dos.Tests`, `AssemblyStudio.Toolchain.Nasm.Tests`, `AssemblyStudio.Debugging.Tests`, `AssemblyStudio.Content.Tests`, `AssemblyStudio.App.Tests`.
  - Added trivial passing tests to each test project.
- Backlog updated: TASK-001 marked `Done`. Milestone M0 in progress.
- `CURRENT_TASK.md` updated: TASK-001 completed, TASK-002 set to `Ready`.

## Failed tests and known issues
- None. 0 failed tests, 0 warnings, 0 errors.

## Decisions and constraints to respect
- ADR-0001 through ADR-0009, D-001 through D-015.
- Compiler-enforced boundary: only `App` references WPF or AvalonEdit. Non-App projects target `net10.0` without UI dependencies.
- Never use destructive Git commands without explicit user instruction. Commit only when requested.

## Exact next action
1. User runs `/assembly-implement TASK-002` (backlog § TASK-002: Architecture rule test in `tests/AssemblyStudio.ArchitectureTests/`).
2. In parallel (user action): TASK-006, provision clean Windows 11 24H2 x64 and Windows 10 Enterprise LTSC 21H2 x64 VMs and record them in `docs/06-implementation/environments.md`.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/` and `.mcp.json`.
