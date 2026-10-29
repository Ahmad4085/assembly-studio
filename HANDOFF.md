# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-29 10:16 +0500 by Claude Code (Opus 5.5), after completing TASK-034 |
| Current task | **None in progress. Milestone 2 in progress (TASK-030..034 Done). Next eligible: M2 `TASK-035` DOS character input services; M2 `TASK-040` NASM runner; M4 `TASK-061` Themes, tokens and contrast test** |
| Last completed | TASK-034 DOS output and termination services (Done 2026-10-29 10:16 +0500) |
| Git state | HEAD `113331d` + uncommitted TASK-034 changes (commit only when the user asks). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-29 10:12 +0500).
- `dotnet test --project tests/AssemblyStudio.Os.Dos.Tests -c Release` → **30/30 passed** (2026-10-29 10:12 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **817/817 passed** (2026-10-29 10:15 +0500).

## Changes since the previous handoff
- **TASK-034 DOS output and termination services** (Claude Code):
  - `src/AssemblyStudio.Os.Dos/DosServices.cs` implements `IOsServices`: INT 21h AH=02h, 09h, 00h and 4Ch, plus INT 20h.
    - DOS's own output column: TAB → spaces to the next multiple of 8; CR → 0; BS → −1; DEL and bytes below 20h raw.
    - AL returns: 02h → the last character output (20h for TAB); 09h → 24h.
    - IP moves past the INT, skipping prefix bytes.
    - INT 20h and AH=00h require CS = `PspSegment` (default 0800h); otherwise `Halted/RuntimeError` + `LastError` DOS001.
    - AH=4Ch works from any CS. INT 20h at PSP:0000 is reported as `ReturnToPsp`.
    - Deviation: 09h with no '$' in the segment → DOS002 halt with no output.
  - `src/AssemblyStudio.Os.Dos/DosServiceError.cs`: the What/Where/Next record for refused calls.
  - Core, additive: `StepOutcome.Terminated`; `StepResult.Termination` (`ProgramTermination(TerminationPath Path, byte ExitCode)`) in `src/AssemblyStudio.Core/ProgramTermination.cs`. A terminated step leaves CS:IP on the INT.
  - Tests: `tests/AssemblyStudio.Os.Dos.Tests/DosServicesTests.cs` (29 tests, real engine + router + console). The test project now also references Engine.I8086 and Runtime.

## Failed tests and known issues
- `DEF-001`: `MainWindowSmokeTests.View_menu_opens_with_shortcuts_and_reset_layout` in App.Tests can fail in non-interactive/background runners because WPF top-level Menu does not maintain submenu open state without active window focus. Recorded in `docs/07-testing/defects.md`. (It passed in the 817/817 run above.)

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-016.
- `AssemblyStudio.Runtime` and `AssemblyStudio.Os.Dos` reference only `AssemblyStudio.Core` (ADR-0002 graph, enforced by the architecture tests).
- Console buffer cap is 1 MB with oldest output trimmed. Input events stamped with instruction count for replay determinism.
- Composition for a runnable machine (as in `DosServicesTests.Rig`):
  1. `memory = new RealModeMemory()`
  2. `dos = new DosServices(psp)`
  3. `router = new InterruptRouter(memory, dos)`
  4. `cpu = new Cpu8086(memory, router)`
  5. `new Machine(cpu, memory, console, router, dos)`, then `ComLoader.Load(machine, image)`
- For TASK-035/036: route input bytes through the same `Output` path for echo (01h echo, 0Ah echo) so the DOS column stays right. Add the Ctrl-C checks to 02h/09h there (they're out of TASK-034's scope).
- For TASK-050: a `RuntimeError` halt from a DOS service carries its message in `DosServices.LastError`. The router's `LastHalt` covers only unsupported services and CPU exceptions.

## Exact next action
1. When the user asks: commit TASK-034. Files:
   - `src/AssemblyStudio.Core/ProgramTermination.cs`, `src/AssemblyStudio.Core/StepResult.cs`, `src/AssemblyStudio.Core/MachineSnapshot.cs`
   - `src/AssemblyStudio.Os.Dos/DosServices.cs`, `src/AssemblyStudio.Os.Dos/DosServiceError.cs`
   - `tests/AssemblyStudio.Os.Dos.Tests/*`
   - `docs/05-planning/backlog.md` and the shared memory files
2. `/assembly-implement TASK-035` (DOS character input services 01h/07h/08h), or `TASK-040` (NASM runner), or `TASK-061` (themes).
3. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
