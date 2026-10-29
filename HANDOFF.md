# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-29 13:41 +0500 by Claude Code (Opus 5.5), after completing TASK-050 |
| Current task | **None in progress. M1, M2 complete; M3 in progress (TASK-050 Done). Next eligible: `TASK-051` snapshots, `TASK-053` input/Ctrl-C/replay; M4 `TASK-061` themes** |
| Last completed | TASK-050 Debug session worker and commands (Done 2026-10-29 13:41 +0500) |
| Git state | HEAD `3431cf2` + uncommitted TASK-050 changes (commit only when the user asks). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-29 13:36 +0500).
- `dotnet test --project tests/AssemblyStudio.Debugging.Tests -c Release` → **16/16 passed**, 5 consecutive runs (2026-10-29 13:37 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1009/1009 passed** in 3m 19s (2026-10-29 13:40 +0500).

## Changes since the previous handoff
- **TASK-050 Debug session worker and commands** (Claude Code, uncommitted):
  - Core `src/AssemblyStudio.Core/IDebugSession.cs`: `IDebugSession` and `PauseReason`. The stop command is `StopSession()` (CA1716).
  - `src/AssemblyStudio.Debugging/DebugSession.cs`: constructor `DebugSession(Func<IMachine> loadMachine)`. The composition root (App) or tests build the machine (engine + runtime + DOS + `ComLoader.Load`), as in `DebugSessionTests.Loader`.
  - Notes for TASK-051 (snapshots):
    - `Publish()` builds the snapshot on the worker; extend it with CurrentInstruction, windows, stack and history; add ≤ 30/s publishing during Run.
    - The run loop currently publishes only at start and stop. Memory changes are folded per byte in `_changedBytes`.
  - Note for TASK-053: a `SendInput` command must enqueue into the console and, when the state is WaitingForInput, continue the interrupted Run (or step); `ConsoleDevice.InstructionCountProvider` should use `InstructionsExecuted`.
  - `tests/AssemblyStudio.Debugging.Tests` now references Engine.I8086, Runtime and Os.Dos (test project only).

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
1. When the user asks: commit TASK-050. Files:
   - `src/AssemblyStudio.Core/IDebugSession.cs`, `src/AssemblyStudio.Debugging/DebugSession.cs`
   - `tests/AssemblyStudio.Debugging.Tests/{AssemblyStudio.Debugging.Tests.csproj, DebugSessionTests.cs}`
   - `docs/05-planning/backlog.md`, shared memory files
2. `/assembly-implement TASK-051` (snapshots) or `TASK-053` (input routing, Ctrl-C, replay). Or `TASK-061` (themes).
3. User decisions pending: DEF-003 (size limit). TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
