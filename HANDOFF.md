# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-29 12:58 +0500 by Claude Code (Opus 5.5), after completing TASK-043 |
| Current task | **None in progress. Milestone 2 in progress (TASK-030..036, TASK-040..043 Done; 11/12). Next eligible: M2 `TASK-044` program-level test harness; M4 `TASK-061` themes** |
| Last completed | TASK-043 Listing/map parsing and source map (Done 2026-10-29 12:58 +0500) |
| Git state | HEAD `40024f0` (TASK-043 committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-29 12:52 +0500).
- `dotnet test --project tests/AssemblyStudio.Toolchain.Nasm.Tests -c Release` → **124/124 passed** (2026-10-29 12:52 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **980/980 passed** in 4m 19s (2026-10-29 12:57 +0500).

## Changes since the previous handoff
- **TASK-043 Listing/map parsing and source map** (Claude Code, uncommitted):
  - Core: `src/AssemblyStudio.Core/ISourceMap.cs` (`ISourceMap`, `SourceLocation`, `SourceMapping`); `BuildResult.SourceMap`.
  - `src/AssemblyStudio.Toolchain.Nasm/SourceMapping/NasmListing.cs` (+ `NasmMapFile`), `NasmSourceMap.cs`. Wired into `NasmAssembler`. BLD010 added to the catalog.
  - The namespace `AssemblyStudio.Toolchain.Nasm.SourceMapping` clashes with the Core type `SourceMapping` inside `AssemblyStudio.Toolchain.Nasm.*`; those files use `using LineMapping = AssemblyStudio.Core.SourceMapping;`.
  - Fixtures 01–12 in `tests/AssemblyStudio.Toolchain.Nasm.Tests/fixtures/`; verification note `docs/06-implementation/task-043-verification.md`.
- **TASK-042 CPU-directive guard** was done and committed by Antigravity (12:35, `71d32fe`).

## Failed tests and known issues
- `DEF-001`: App smoke test can fail in background runners (WPF menu focus). Open.
- `DEF-002`: one full solution run stalled in the engine tests (2026-10-29 10:36). Not reproduced since.
- `DEF-003`: `ComLoader` accepts at most 65,278 bytes, while FR-031 and the build allow 65,280 (and the loader's message says 65,280). **Needs a user decision**: loader accepts 65,280, or FR-031 → 65,278.

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
1. Done: TASK-043 committed. Files were:
   - `src/AssemblyStudio.Core/ISourceMap.cs`, `src/AssemblyStudio.Core/BuildResult.cs`
   - `src/AssemblyStudio.Toolchain.Nasm/NasmAssembler.cs`, `src/AssemblyStudio.Toolchain.Nasm/SourceMapping/*`, `src/AssemblyStudio.Toolchain.Nasm/Diagnostics/BuildMessages.{cs,resx}`
   - `tests/AssemblyStudio.Toolchain.Nasm.Tests/SourceMapTests.cs`, `tests/AssemblyStudio.Toolchain.Nasm.Tests/fixtures/**`
   - `docs/06-implementation/task-043-verification.md`, `docs/05-planning/backlog.md`, shared memory files
2. `/assembly-implement TASK-044` (program-level harness: NASM fixtures → build → load → run → assert output/termination; covers AC-021/022/023/029/031/064 and the POC-002 fixtures as regressions), or `TASK-061` (themes).
3. User decisions pending: DEF-003 (size limit). TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
