# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-29 15:43 +0500 by Claude Code (Opus 5.5), after completing TASK-053 |
| Current task | **None in progress. M1, M2 complete; M3 in progress (TASK-050..053 Done; 4/5). Next eligible: `TASK-054` fault containment, fuzz and soak; M4 `TASK-061` themes** |
| Last completed | TASK-053 Input routing, Ctrl-C delivery and replay (Done 2026-10-29 15:43 +0500) |
| Git state | HEAD `e9bcdd1` + uncommitted TASK-053 changes (commit only when the user asks). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-29 15:34 +0500).
- `dotnet test --project tests/AssemblyStudio.Debugging.Tests -c Release` → **50/50 passed**, 5 consecutive runs (2026-10-29 15:36 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1043/1043 passed**, two consecutive runs (2026-10-29 15:38 and 15:42 +0500).

## Changes since the previous handoff
- **TASK-053 Input routing, Ctrl-C delivery and replay** (Claude Code, uncommitted):
  - Core: `IConsoleDevice.EnqueueInput(byte)`; `InputRecord`; `IDebugSession.SendInput(IReadOnlyList<byte>)`, `SendCtrlC()`, `RecordedInput`, `Replay(recording)`.
  - `DebugSession`:
    - The inline queue (`PostInline`/`ProcessInline`) carries input and memory refreshes. A run handles it each instruction without stopping; an idle worker is woken by `CommandKind.Inline`, which does not count as a pending (interrupting) command.
    - `WaitResume` decides what continues when a waiting program gets input.
    - Replay delivery happens in `Step()` before the instruction.
  - `InstructionsExecuted` (public) is now taken from the published record, like State, PauseReason and LastStep. The worker keeps the live counter in `_instructionsExecuted`, which input stamping uses.
  - Fixed a TASK-051 bug: `WatchMemory` during a run paused the program (it posted an interrupting command).
  - For TASK-074 (UI keys): call `SendInput` for typed keys (extended keys as 00h + scan code) and `SendCtrlC` per D-015.

## Failed tests and known issues
- `DEF-001`: App smoke test can fail in background runners (WPF menu focus). Open.
- `DEF-002`: one full solution run stalled in the engine tests (10:36). Not reproduced since.
- `DEF-003`: `ComLoader` accepts at most 65,278 bytes while FR-031 and the build allow 65,280. **Needs a user decision.**
- `DEF-004`: toolchain timing tests under full-suite load. Fixed (13:20).

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
1. When the user asks: commit TASK-053. Files:
   - `src/AssemblyStudio.Core/{IConsoleDevice.cs, IDebugSession.cs}`
   - `src/AssemblyStudio.Runtime/Console/{ConsoleDevice.cs, NullConsoleDevice.cs}`
   - `src/AssemblyStudio.Debugging/DebugSession.cs`
   - `tests/AssemblyStudio.Runtime.Tests/MachineTests.cs`, `tests/AssemblyStudio.Debugging.Tests/InputAndReplayTests.cs`
   - `docs/05-planning/backlog.md`, shared memory files
2. `/assembly-implement TASK-054` (fault containment, fuzz and soak: random images under a budget never crash the host; 30-minute soak), the last M3 task. Or `TASK-061` (themes).
3. User decisions pending: DEF-003 (size limit). TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
