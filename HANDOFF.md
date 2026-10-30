# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-29 16:47 +0500 by Claude Code (Opus 5.5), after completing TASK-054 (M3 complete) |
| Current task | **None in progress. M1, M2, M3 complete. Next: `/assembly-test` for M1–M3, then M4 (`TASK-061` themes, `TASK-062` document service, …)** |
| Last completed | TASK-054 Fault containment, fuzz and soak (Done 2026-10-29 16:47 +0500) |
| Git state | HEAD `d3accdf` (TASK-054 committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-29 16:20 +0500).
- `ASSEMBLY_STUDIO_SOAK_MINUTES=30 dotnet test --project tests/AssemblyStudio.Debugging.Tests -c Release --filter-method "*Soak_random_programs*"` → **passed** (16:10–16:40): 396,642 programs, 1,586,560 random commands, 0 faults; memory 1.1 → 2.2 MiB; threads 18 → 18.
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1055/1055 passed**, two consecutive runs (2026-10-29 16:43 and 16:47 +0500).

## Changes since the previous handoff
- **TASK-054 Fault containment, fuzz and soak** (Claude Code, uncommitted). **Milestone M3 is complete.**
  - Core: `IDebugSession.LastFault`.
  - `DebugSession`: `ContainFault`, `Log`, isolated subscriber calls; new optional constructor parameter `faultLog` (`Action<Exception>`). TASK-068 (logging) should pass its local-log writer here.
  - `ConsoleDevice`: the snapshot delta is a capped counter over the ring buffer (DEF-005 fix).
  - Tests: `tests/AssemblyStudio.Debugging.Tests/Robustness/` (`RobustnessKit`, `FaultContainmentTests`, `HostileProgramTests`, `FuzzAndSoakTests`) and 2 new `ConsoleDeviceTests`.
  - Soak: set `ASSEMBLY_STUDIO_SOAK_MINUTES=30` and filter `*Soak_random_programs*` for the NFR-010 run. A run that doesn't settle reports CS:IP and the bytes there, and saves the image under `%TEMP%`.
  - `SnapshotTests` AC-048: the minimum running-snapshot count was lowered to 3 (it saw 7/s under full-suite load); the ≤ 30/s limit is unchanged.

## Failed tests and known issues
- `DEF-001`: App smoke test can fail in background runners (WPF menu focus). Open.
- `DEF-002`: one full solution run stalled in the engine tests (10:36). Not reproduced since.
- `DEF-003`: `ComLoader` accepts at most 65,278 bytes while FR-031 and the build allow 65,280. **Needs a user decision.**
- `DEF-004` (timing tests under load) and `DEF-005` (output-flood hang in `ConsoleDevice`): fixed.

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
1. Done: TASK-054 committed. Files were:
   - `src/AssemblyStudio.Core/IDebugSession.cs`, `src/AssemblyStudio.Debugging/DebugSession.cs`, `src/AssemblyStudio.Runtime/Console/ConsoleDevice.cs`
   - `tests/AssemblyStudio.Debugging.Tests/Robustness/*`, `tests/AssemblyStudio.Debugging.Tests/SnapshotTests.cs`, `tests/AssemblyStudio.Runtime.Tests/ConsoleDeviceTests.cs`
   - `docs/05-planning/backlog.md`, `docs/07-testing/defects.md`, shared memory files
2. `/assembly-test` to verify milestones M1–M3, then M4 tasks (`TASK-061` themes, `TASK-062` document service, …).
3. User decisions pending: DEF-003 (size limit). TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
