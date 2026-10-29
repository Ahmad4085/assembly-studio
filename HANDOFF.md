# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-29 13:25 +0500 by Claude Code (Opus 5.5), after completing TASK-044 (M2 complete) |
| Current task | **None in progress. M1 and M2 complete. Next: `/assembly-test` for M2, then M3 `TASK-050` debug session worker; M4 `TASK-061` themes also eligible** |
| Last completed | TASK-044 Program-level test harness (Done 2026-10-29 13:25 +0500) |
| Git state | HEAD `1e4ea1d` (TASK-044 committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-29 13:20 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **994/994 passed** in 3m 01s (2026-10-29 13:24 +0500), 10 test projects including the new ProgramTests (14/14).

## Changes since the previous handoff
- **TASK-044 Program-level test harness** (Claude Code, uncommitted):
  - `tests/AssemblyStudio.ProgramTests/` (new; in `AssemblyStudio.slnx`): `ProgramHarness.cs` (API: `BuildAsync`, `RunAsync`, `Run(build, InputScript, budget, trace)` → `ProgramRun`), `ProgramTests.cs`, `Programs/*.asm`.
  - Input model: `InputScript.Typed(chunks…)` queues the next chunk each time the program waits. `InputScript.Replaying(recording)` replays bytes at their instruction counts. The console's instruction count = executed instructions (waiting steps don't count).
  - TASK-091 (samples) should reuse this harness.
  - DEF-004 fixed in TASK-040 code: `NasmOptions.VersionCheckTimeout`; `tests/AssemblyStudio.Toolchain.Nasm.Tests/BuildTimeTests.cs` (moved from `NasmAssemblerTests`).
- **Milestone M2 is complete** (TASK-030..036, TASK-040..044).

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
1. Done: TASK-044 committed. Files were:
   - `AssemblyStudio.slnx`, `tests/AssemblyStudio.ProgramTests/**`
   - `src/AssemblyStudio.Toolchain.Nasm/NasmAssembler.cs`, `src/AssemblyStudio.Toolchain.Nasm/NasmOptions.cs`
   - `tests/AssemblyStudio.Toolchain.Nasm.Tests/NasmAssemblerTests.cs`, `tests/AssemblyStudio.Toolchain.Nasm.Tests/BuildTimeTests.cs`
   - `docs/05-planning/backlog.md`, `docs/07-testing/defects.md`, shared memory files
2. `/assembly-test` to verify milestone M2, then `/assembly-implement TASK-050` (M3 debug session worker). Or `TASK-061` (themes).
3. User decisions pending: DEF-003 (size limit). TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
