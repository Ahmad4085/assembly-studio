# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-29 11:16 +0500 by Claude Code (Opus 5.5), after completing TASK-040 |
| Current task | **None in progress. Milestone 2 in progress (TASK-030..036, TASK-040 Done). Next eligible: M2 `TASK-041` diagnostics, `TASK-042` CPU guard, `TASK-043` source map; M4 `TASK-061` themes** |
| Last completed | TASK-040 NASM runner (Done 2026-10-29 11:16 +0500) |
| Git state | HEAD `03fefeb` (TASK-040 committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-29 11:12 +0500).
- `dotnet test --project tests/AssemblyStudio.Toolchain.Nasm.Tests -c Release` → **36/36 passed** (2026-10-29 11:13 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **892/892 passed** in 3m 17s (2026-10-29 11:15 +0500).

## Changes since the previous handoff
- **TASK-040 NASM runner** (Claude Code, uncommitted):
  - Core: `IAssembler`, `BuildRequest`, `BuildResult` (+ `BuildOutcome`), `AssemblerInfo` (+ `AssemblerOrigin`, `AssemblerStatus`) in `src/AssemblyStudio.Core/IAssembler.cs` and `BuildResult.cs`.
  - `src/AssemblyStudio.Toolchain.Nasm/NasmAssembler.cs`, `NasmOptions.cs`, `ProcessRunner.cs` (internal). `InternalsVisibleTo` the toolchain tests.
  - Map: `--before "[map all]"` writes the map to stdout (diagnostics on stderr), because NASM's map directive can't take a path containing spaces. This deviates from the literal ADR-0005 §2 flag text; the user should be told and may want ADR-0005 amended.
  - `BuildResult` has no diagnostics, listing or source map yet: TASK-041 parses `Output`, and TASK-043 adds the source map. The internal run already captures stdout (the map) for TASK-043. The listing file is written to the temp folder and deleted; TASK-043 must read it before the folder is deleted.
- **Earlier today (all committed):** TASK-034/035/036 DOS services; Core `StepOutcome.Terminated`, `ProgramTermination`, `IConsoleDevice.TryPeekInput`.

## Failed tests and known issues
- `DEF-001`: App smoke test can fail in background runners (WPF menu focus). Open.
- `DEF-002`: one full solution run stalled in the engine tests (2026-10-29 10:36). Not reproduced since (11:03 and 11:15 runs normal). If it recurs, capture the running test with `--output Detailed` before stopping it.

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
1. Done: TASK-040 committed. Files were:
   - `src/AssemblyStudio.Core/IAssembler.cs`, `src/AssemblyStudio.Core/BuildResult.cs`
   - `src/AssemblyStudio.Toolchain.Nasm/*`
   - `tests/AssemblyStudio.Toolchain.Nasm.Tests/NasmAssemblerTests.cs`
   - `docs/05-planning/backlog.md` and the shared memory files
2. `/assembly-implement TASK-041` (GNU diagnostics parser over `BuildResult.Output`, message catalog, MASM hint, 65,280-byte size limit), or `TASK-042` (CPU guard via `nasm -E`, reuse `ProcessRunner`), or `TASK-043` (listing + map → `ISourceMap`), or `TASK-061` (themes).
3. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
