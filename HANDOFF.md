# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-30 11:27 +0500 by Claude Code (Opus 5.5), after TASK-061 (automated part done; blocked on BLK-002) |
| Current task | **None in progress. M1–M3 complete; M4: TASK-060 Done, TASK-061 Blocked (BLK-002: user AC-059 check). Next eligible: `TASK-062` document service** |
| Last completed | TASK-054 (Done 2026-10-29 16:47 +0500). TASK-061 automated work done 2026-10-30 11:27 +0500, status Blocked |
| Git state | HEAD `94b7e18` (TASK-061 committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-30 11:20 +0500).
- `dotnet test --project tests/AssemblyStudio.App.Tests -c Release` → **60/60 passed** (2026-10-30 11:21 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1074/1074 passed**, two consecutive runs (2026-10-30 11:22 and 11:25 +0500).

## Changes since the previous handoff
- **TASK-061 Themes, tokens and contrast test** (Claude Code, uncommitted, **Blocked** on BLK-002):
  - `src/AssemblyStudio.App/Themes/{Shared,Dark,Light,HighContrast}.xaml`; `App.xaml` merges Shared, theme and ShellStyles.
  - `src/AssemblyStudio.App/Shell/ThemeManager.cs`: `Resolve`, `Apply(AppTheme)`, `Watch()`, `Preference`. TASK-067 should persist the preference and set `ThemeManager.Preference`; App creates the manager in `OnStartup`.
  - New views must reference color tokens with `DynamicResource` (live theme switching).
  - `tests/AssemblyStudio.App.Tests/ThemeTokenTests.cs`.
  - Verification and the user checklist: `docs/06-implementation/task-061-verification.md`.

## Failed tests and known issues
- `DEF-001`: App smoke test can fail in background runners (WPF menu focus). Open.
- `DEF-002`: one full solution run stalled in the engine tests (2026-10-29 10:36). Not reproduced since.
- `DEF-003`: `ComLoader` accepts at most 65,278 bytes while FR-031 and the build allow 65,280. **Needs a user decision.**
- `BLK-002`: TASK-061 waits for the user's manual AC-059 display check.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-016.
- `AssemblyStudio.Runtime`, `AssemblyStudio.Os.Dos`, and `AssemblyStudio.Toolchain.Nasm` reference only `AssemblyStudio.Core` (ADR-0002 graph, enforced by architecture tests).
- Console buffer cap is 1 MB with oldest output trimmed. Input events stamped with instruction count for replay determinism.
- Composition for a runnable machine (as in `DosServicesTests.Rig`):
  1. `memory = new RealModeMemory()`
  2. `dos = new DosServices(psp)`
  3. `router = new InterruptRouter(memory, dos)`
  4. `cpu = new Cpu8086(memory, router)`
  5. `new Machine(cpu, memory, console, router, dos)`, then `ComLoader.Load(machine, image)`
- For TASK-050: a `RuntimeError` halt from a DOS service carries its message in `DosServices.LastError`. The router's `LastHalt` covers only unsupported services and CPU exceptions.

## Exact next action
1. Done: TASK-061 committed. Files were:
   - `src/AssemblyStudio.App/{App.xaml, App.xaml.cs, Themes/*, Shell/ThemeManager.cs}`
   - `tests/AssemblyStudio.App.Tests/ThemeTokenTests.cs`
   - `docs/06-implementation/task-061-verification.md`, `docs/05-planning/backlog.md`, shared memory files
2. `/assembly-implement TASK-062` (document service and file workflow), or other M4 tasks whose dependencies are Done.
3. User: AC-059 manual check (then TASK-061 → Done), TASK-006 test VMs, DEF-003 decision.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
