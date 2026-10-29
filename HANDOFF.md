# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-29 14:06 +0500 by Claude Code (Opus 5.5), after completing TASK-051 |
| Current task | **None in progress. M1, M2 complete; M3 in progress (TASK-050..051 Done). Next eligible: `TASK-052` breakpoints/Step Over, `TASK-053` input/Ctrl-C/replay; M4 `TASK-061` themes** |
| Last completed | TASK-051 Snapshots (Done 2026-10-29 14:06 +0500) |
| Git state | HEAD `8bfc75f` (TASK-051 committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-29 13:58 +0500).
- `dotnet test --project tests/AssemblyStudio.Debugging.Tests -c Release` → **28/28 passed** (3 consecutive runs).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1021/1021 passed**, two consecutive runs (2026-10-29 14:02 and 14:05 +0500).

## Changes since the previous handoff
- **TASK-051 Snapshots** (Claude Code, uncommitted):
  - Core: `HistoryEntry`; `MachineSnapshot.HistoryDelta` is `IReadOnlyList<HistoryEntry>` (was `int`); `IDisassembler.Decode(bytes, ip)` + `DecodedInstruction`; `IDebugSession.History`, `WatchMemory`, `UnwatchMemory`.
  - `src/AssemblyStudio.Debugging/Snapshots/{ExecutionHistory.cs, SnapshotContent.cs}`. `DebugSession(loadMachine, disassembler, sourceMap?)`.
  - **For the view-model tasks (TASK-081..084):** `MemoryChanges`, `ConsoleDelta` and `HistoryDelta` are deltas **since the previous published snapshot**, including running snapshots (≤ 30/s) and the refresh snapshot that `WatchMemory` posts while paused.
    - The console view must append every delta.
    - For FR-056 ("changed since the previous pause"), merge `MemoryChanges` from all snapshots after the last paused one.
    - Register and flag changes come from diffing paused snapshots' `Cpu`.

## Failed tests and known issues
- `DEF-001`: App smoke test can fail in background runners (WPF menu focus). Open.
- `DEF-002`: one full solution run stalled in the engine tests (10:36). Not reproduced since.
- `DEF-003`: `ComLoader` accepts at most 65,278 bytes while FR-031 and the build allow 65,280. **Needs a user decision.**
- `DEF-004`: toolchain timing tests under full-suite load. **Fixed** (13:20); re-runs green.

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
1. Done: TASK-051 committed. Files were:
   - `src/AssemblyStudio.Core/{MachineSnapshot.cs, IDisassembler.cs, IDebugSession.cs}`
   - `src/AssemblyStudio.Engine.I8086/Disassembly/IcedDisassembler.cs`
   - `src/AssemblyStudio.Debugging/{DebugSession.cs, Snapshots/*}`
   - `tests/AssemblyStudio.Core.Tests/MachineSnapshotTests.cs`, `tests/AssemblyStudio.Debugging.Tests/{DebugSessionTests.cs, SnapshotTests.cs}`
   - `docs/05-planning/backlog.md`, shared memory files
2. `/assembly-implement TASK-052` (breakpoints via `ISourceMap.FirstOffsetOf`, Step Over with a temporary breakpoint) or `TASK-053` (SendInput/SendCtrlC, replay). Or `TASK-061` (themes).
3. User decisions pending: DEF-003 (size limit). TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
