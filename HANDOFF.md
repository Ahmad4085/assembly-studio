# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-29 15:00 +0500 by Antigravity (IDE, Gemini 3.8 Flash), after completing TASK-052 |
| Current task | **None in progress. M1, M2 complete; M3 in progress (TASK-050..052 Done; 3/5). Next eligible: `TASK-053` input/Ctrl-C/replay; M4 `TASK-061` themes** |
| Last completed | TASK-052 Breakpoints and Step Over (Done 2026-10-29 15:00 +0500) |
| Git state | HEAD `4b8cc4f` (TASK-051 committed). Uncommitted changes for TASK-052. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-29 14:27 +0500).
- `dotnet test tests/AssemblyStudio.Debugging.Tests -c Release` → **40/40 passed** (2026-10-29 14:52 +0500).
- `dotnet test tests/AssemblyStudio.ArchitectureTests -c Release` → **39/39 passed** (2026-10-29 14:53 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1032/1033 passed** (2026-10-29 14:57 +0500; 1 pre-existing defect DEF-001 in App.Tests).

## Changes since the previous handoff
- **TASK-052 Breakpoints and Step Over** (Antigravity (IDE, Gemini 3.8 Flash), uncommitted):
  - **Core:**
    - `src/AssemblyStudio.Core/Breakpoint.cs`: `Breakpoint(Location, IsBound, Offset, IsEnabled)`.
    - `src/AssemblyStudio.Core/IDebugSession.cs`: added `PauseReason.Breakpoint`, `IReadOnlyList<Breakpoint> Breakpoints { get; }`, `ToggleBreakpoint(file, line)`, `SetBreakpoint(file, line, enabled)`, `RemoveBreakpoint(file, line)`, `RebindBreakpoints(sourceMap)`, and `StepOver()`.
  - **`AssemblyStudio.Debugging` (`DebugSession.cs`):**
    - Fast O(1) active breakpoint lookup array (`bool[65536]`) updated under lock and atomically swapped with zero lock contention in the instruction execution loop.
    - Breakpoint continuation logic (`_lastBreakpointHit`) ensures resuming past an active breakpoint executes the instruction rather than immediately re-pausing.
    - Breakpoints persist across `Reset()` (FR-052).
    - `StepOver`: decodes instruction at CS:IP with `_disassembler.Decode`. For CALL/INT (`call`, `int`, `int3`, `into`), sets a temporary breakpoint at the next instruction (`IP + Length`) and runs, pausing with `PauseReason.Step` on arrival, or pausing with `PauseReason.Breakpoint` if a user breakpoint was also on that target; stops early on user breakpoints hit inside the procedure, user pause, budget exhaustion, runtime error, or program termination. Non-CALL/INT instructions fall back to single-step (`PauseReason.Step`).
  - **Tests (`tests/AssemblyStudio.Debugging.Tests/BreakpointStepOverTests.cs`):**
    - 12 comprehensive unit and integration tests covering AC-032 (looping program with breakpoint before line 10, multiple loop iterations, removal and run to termination), AC-033 (step over 20-instruction procedure to instruction after call; step over stopping early on breakpoint inside procedure), step over INT 21h, step over non-call instructions, step over stopping early on termination or runtime error, bound/unbound state and rebinding after rebuild, disabled breakpoint, reset persistence, entry point breakpoint, and toggle/remove semantics.

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
1. Commit TASK-052 changes when the user requests or approves. Files to commit:
   - `src/AssemblyStudio.Core/Breakpoint.cs`
   - `src/AssemblyStudio.Core/IDebugSession.cs`
   - `src/AssemblyStudio.Debugging/DebugSession.cs`
   - `tests/AssemblyStudio.Debugging.Tests/BreakpointStepOverTests.cs`
   - `docs/05-planning/backlog.md`
   - `CURRENT_TASK.md`
   - `PROJECT_STATE.md`
   - `ROADMAP.md`
   - `WORK_LOG.md`
   - `HANDOFF.md`
2. Next eligible task: `/assembly-implement TASK-053` (input routing, Ctrl-C delivery, and deterministic replay) or `TASK-061` (themes).
3. User decisions pending: DEF-003 (size limit). TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
