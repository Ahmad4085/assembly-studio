# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-29 11:53 +0500 by Claude Code (Opus 5.5), after completing TASK-041 |
| Current task | **None in progress. Milestone 2 in progress (TASK-030..036, TASK-040..041 Done). Next eligible: M2 `TASK-042` CPU guard, `TASK-043` source map; M4 `TASK-061` themes** |
| Last completed | TASK-041 Diagnostics, message catalog, MASM hint, size limit (Done 2026-10-29 11:53 +0500) |
| Git state | HEAD `9f364bd` (TASK-041 committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-29 11:48 +0500).
- `dotnet test --project tests/AssemblyStudio.Toolchain.Nasm.Tests -c Release` → **58/58 passed** (2026-10-29 11:49 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **914/914 passed** in 3m 23s (2026-10-29 11:52 +0500).

## Changes since the previous handoff
- **TASK-041 Diagnostics, message catalog, MASM hint, size limit** (Claude Code, uncommitted):
  - Core: `Diagnostic` and `DiagnosticSeverity` (`src/AssemblyStudio.Core/Diagnostic.cs`); `BuildResult.Diagnostics`.
  - `src/AssemblyStudio.Toolchain.Nasm/Diagnostics/`: `NasmDiagnosticParser`, `BuildMessages` + `BuildMessages.resx` (catalog codes ASM001–003, BLD001–008), `MasmHint`. Wired into `NasmAssembler.BuildAsync`.
  - The build message catalog lives in Toolchain.Nasm resources. The runtime and DOS messages (RTE0xx in `RuntimeErrorPolicy`, DOS001/002 in `DosServices`) are still inline English strings in their projects. Moving them into resources (NFR-032) and into the AC-055 review needs a follow-up, likely with TASK-104 or a dedicated task.
- **Earlier today (all committed):** TASK-034/035/036 DOS services, TASK-040 NASM runner.

## Failed tests and known issues
- `DEF-001`: App smoke test can fail in background runners (WPF menu focus). Open.
- `DEF-002`: one full solution run stalled in the engine tests (2026-10-29 10:36). Not reproduced since. If it recurs, capture the running test with `--output Detailed` before stopping it.
- `DEF-003`: `ComLoader` accepts at most 65,278 bytes, while FR-031 and the build allow 65,280 (and the loader's message says 65,280). **Needs a user decision**: loader accepts 65,280, or FR-031 → 65,278.

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
1. Done: TASK-041 committed. Files were:
   - `src/AssemblyStudio.Core/Diagnostic.cs`, `src/AssemblyStudio.Core/BuildResult.cs`
   - `src/AssemblyStudio.Toolchain.Nasm/NasmAssembler.cs`, `src/AssemblyStudio.Toolchain.Nasm/Diagnostics/*`
   - `tests/AssemblyStudio.Toolchain.Nasm.Tests/BuildDiagnosticsTests.cs`, `tests/AssemblyStudio.Toolchain.Nasm.Tests/NasmAssemblerTests.cs`
   - `docs/05-planning/backlog.md`, `docs/07-testing/defects.md`, shared memory files
2. `/assembly-implement TASK-042` (CPU guard: `nasm -E` scan, reuse `ProcessRunner`, report as a build error with a new catalog code), or `TASK-043` (listing + map → `ISourceMap`), or `TASK-061` (themes).
3. User decisions pending: DEF-003 (size limit). TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
