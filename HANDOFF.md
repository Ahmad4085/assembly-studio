# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-29 12:42 +0500 by Antigravity (IDE, Gemini 3.8 Flash), after completing TASK-042 |
| Current task | **None in progress. Milestone 2 in progress (TASK-030..036, TASK-040..042 Done; 10/12). Next eligible: M2 `TASK-043` source map; M4 `TASK-061` themes** |
| Last completed | TASK-042 CPU-directive guard (Done 2026-10-29 12:35 +0500, committed in `71d32fe`) |
| Git state | HEAD `71d32fe` (TASK-042 committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-29 12:17 +0500).
- `dotnet test --project tests/AssemblyStudio.Toolchain.Nasm.Tests -c Release` → **79/79 passed** (2026-10-29 12:18 +0500).
- `dotnet test --project tests/AssemblyStudio.ArchitectureTests -c Release` → **39/39 passed** (2026-10-29 12:18 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **934/935 passed** in 3m 12s (2026-10-29 12:33 +0500; only pre-existing DEF-001 in App.Tests).

## Changes since the previous handoff
- **TASK-042 CPU-directive guard** (Antigravity (IDE, Gemini 3.8 Flash), uncommitted):
  - `src/AssemblyStudio.Toolchain.Nasm/Diagnostics/CpuDirectiveGuard.cs`:
    - Scans preprocessed NASM output (`-Xgnu -E`, FR-027, ADR-0005 §3) for CPU directives requesting a processor level other than 8086.
    - Parses `%line <line>[+<step>] [<file>]` to accurately track source files and line numbers across empty lines, multi-line constructs, macros, and includes.
    - Rejects non-8086 levels (`186`, `286`, `386`, `486`, `586`, `686`, `PENTIUM`, `x64`, etc.) and allows `8086`. Avoids false-positives on string literals.
    - Reports against `documentPath` for the main file (or `null` if untitled) and relative/full paths for included files.
  - `src/AssemblyStudio.Toolchain.Nasm/Diagnostics/BuildMessages.cs` and `BuildMessages.resx`:
    - Added stable code `BLD009` (`UnsupportedCpu`, severity `DiagnosticSeverity.Error`).
    - What: *"The CPU directive '{0}' is not supported. Assembly Studio targets the 8086 processor."*
    - Next: *"Remove the CPU directive or change it to 8086, then build again."*
  - `src/AssemblyStudio.Toolchain.Nasm/NasmAssembler.cs`:
    - In `BuildAsync`, executes `nasm -Xgnu -E <source>` before binary assembly.
    - If non-8086 CPU directives are detected, halts the build immediately with `BuildOutcome.Failed`, empty binary output, and `BLD009` diagnostics.
    - Honors timeout (`BLD002`), crash (`BLD003`), and cancellation (`BLD005`).
  - Tests:
    - 8 new unit tests in `tests/AssemblyStudio.Toolchain.Nasm.Tests/CpuDirectiveGuardTests.cs`.
    - 5 new integration tests in `tests/AssemblyStudio.Toolchain.Nasm.Tests/BuildDiagnosticsTests.cs` covering AC-018 instruction rejection (`ASM001`), `cpu 386` bypass rejection (`BLD009`), hidden-in-macro bypass rejection (`BLD009`), include file bypass rejection (`BLD009`), and explicit `cpu 8086` build success.
    - Toolchain test suite expanded from 58 to 79 tests (all passing).

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
1. Commit TASK-042 when requested by user. Files modified/added:
   - `src/AssemblyStudio.Toolchain.Nasm/Diagnostics/BuildMessages.cs`
   - `src/AssemblyStudio.Toolchain.Nasm/Diagnostics/BuildMessages.resx`
   - `src/AssemblyStudio.Toolchain.Nasm/Diagnostics/CpuDirectiveGuard.cs`
   - `src/AssemblyStudio.Toolchain.Nasm/NasmAssembler.cs`
   - `tests/AssemblyStudio.Toolchain.Nasm.Tests/CpuDirectiveGuardTests.cs`
   - `tests/AssemblyStudio.Toolchain.Nasm.Tests/BuildDiagnosticsTests.cs`
   - `docs/05-planning/backlog.md`, `CURRENT_TASK.md`, `PROJECT_STATE.md`, `ROADMAP.md`, `WORK_LOG.md`, `HANDOFF.md`
2. `/assembly-implement TASK-043` (listing + map → `ISourceMap`), or `/assembly-implement TASK-061` (themes).
3. User decisions pending: DEF-003 (size limit). TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
