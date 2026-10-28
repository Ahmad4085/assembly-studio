# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` A6).

| Field | Value |
|---|---|
| Written | 2026-10-28 11:31 +0500 by Claude Code (Opus 5.5), after completing TASK-060 |
| Current task | **TASK-016 Control transfer and interrupts: `Approved`, not started, no owner** (TASK-061 themes also eligible) |
| Last completed | TASK-060 WPF shell and layout (Done 2026-10-28 11:31 +0500) |
| Git state | TASK-060 committed as `ff88c29` (on the user's request, 2026-10-28) after `d56a1b4`/`37e7422`/`ddc5fe5`; this HANDOFF update is a follow-up docs commit. **Not pushed** (origin/master at `357251e`). Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` (OpenCode's analysis scripts) |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` -> 0 warnings, 0 errors (2026-10-28 11:31 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` -> **465/465 passed** (2026-10-28 11:31 +0500); 229 oracle files at 100%; 41 App tests.
- The app runs: `src/AssemblyStudio.App/bin/Release/net10.0-windows/AssemblyStudio.App.exe` opens the empty shell (startup 1.5–2.5 s here).

## Changes since the previous handoff
- TASK-060: WPF shell in `src/AssemblyStudio.App/Shell/` (see `CURRENT_TASK.md` and `docs/06-implementation/task-060-verification.md`).
- New dependency: CommunityToolkit.Mvvm 8.4.2 (MIT, ADR-0007), App project only.
- Layout is saved to `%APPDATA%\AssemblyStudio\settings.json` under `"layout"`; `--settings <path>` overrides the file, `--smoke-report <path>`
  measures startup and exits without saving.
- Strings: edit `Shell/Strings.resx` and regenerate `Shell/Strings.cs` together (plain accessor; `.resx` code generation doesn't run before
  WPF's markup pass).

## Failed tests and known issues
- None failing. Engine: F6/F7 /1 and FF arms 2/3/4/5/7 halt until their tasks. UI: dark theme only (TASK-061); later-task commands disabled.
- Cascadia Mono isn't installed on this dev machine (code font falls back to Consolas).

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-016. Core has no project references; Engine.I8086 references only Core.
- UI: views use token resources only; panels show only snapshot data (NFR-018) — empty states until their tasks.
- WPF tests run on the shared STA thread in `tests/AssemblyStudio.App.Tests/StaTestThread.cs` (one `Application` per process).
- `Cpu8086` is not thread-safe (ADR-0004 A2). Runtime error policy for INT 0 belongs to TASK-032.

## Exact next action
1. Optional: push `ddc5fe5`, `37e7422`, `d56a1b4`, `ff88c29` and the HANDOFF commit — only on the user's request.
2. User runs `/assembly-implement TASK-016` (engine) or `/assembly-implement TASK-061` (themes and contrast test).
3. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/` and `.mcp.json`.
