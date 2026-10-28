# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-28 17:53 +0500 by Antigravity (IDE, Gemini 3.8 Flash), after completing TASK-032 |
| Current task | **None in progress. Milestone 2 in progress. Next eligible: M2 `TASK-033` Console device; M2 `TASK-040` NASM runner; M4 `TASK-061` Themes, tokens and contrast test** |
| Last completed | TASK-032 Interrupt router and runtime error policy (Done 2026-10-28 17:53 +0500) |
| Git state | HEAD `7a61c36` (TASK-032 committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-28 17:49 +0500).
- `& ...\AssemblyStudio.Runtime.Tests.exe` → **57/57 passed** in 0.72s (2026-10-28 17:49 +0500).
- `& ...\AssemblyStudio.ArchitectureTests.exe` → **39/39 passed** in 1.47s (2026-10-28 17:49 +0500).
- Core unit test suite executables (Core, Os.Dos, Nasm, Debugging, Content) → **18/18 passed** (2026-10-28 17:49 +0500).
- `& ...\AssemblyStudio.Engine.I8086.Tests.exe -class "*DeterminismTests*" -class "*EnginePerformanceTests*" -class "*IcedDisassemblerTests*"` → **80/80 passed** in 14.7s (2026-10-28 17:50 +0500).

## Changes since the previous handoff
- **TASK-032 Interrupt router and runtime error policy** completed:
  - Implemented `IvtStubs` in `src/AssemblyStudio.Runtime/Interrupts/IvtStubs.cs`:
    - Reserved stub segment `0xF000` (physical `0xF0000`, ADR-0004 §2–3).
    - Allocates 4-byte stubs for all 256 vectors at `0xF000:(vector * 4)`, containing an `IRET` opcode (`0xCF`).
    - Initializes IVT at `0000:0000h..03FFh` pointing each vector to its default reserved stub.
    - `IsDefaultVector(memory, vector)` accurately detects default versus program-installed vectors.
  - Implemented `RuntimeHaltInfo` and `RuntimeErrorPolicy` in `src/AssemblyStudio.Runtime/Interrupts/`:
    - Conforms to the What/Where/Next contract (FR-038, FR-040, FR-066, NFR-024, AC-029, AC-031, AC-055).
    - CPU exceptions (INT 0..4): `RTE001` Divide error, `RTE002` Single-step trap, `RTE003` Non-maskable interrupt, `RTE004` Breakpoint interrupt, `RTE005` Overflow trap.
    - Unsupported services: `RTE021` for INT 21h functions naming AH, `RTE020` for general unsupported interrupts.
  - Implemented `InterruptRouter` in `src/AssemblyStudio.Runtime/Interrupts/InterruptRouter.cs`:
    - Implements `IInterruptRouter`.
    - Program-installed vectors return `null` so CPU vectors normally through IVT (push Flags/CS/IP, clear IF/TF, ADR-0004 §3).
    - CPU exceptions with default vector halt with `HaltReason.RuntimeError` and populate `LastHalt` (FR-038).
    - OS services delegation (`IOsServices.HandleService`), passthrough of `Executed`, `WaitingForInput`, and `UnsupportedService`.
    - Unsupported interrupts/functions halt with `HaltReason.UnsupportedService` and populate `LastHalt` (FR-040, NFR-013).
  - Wired auto-attachment in `src/AssemblyStudio.Runtime/Machine.cs` (`router.Machine = this;`).
  - Authored 18 unit tests in `tests/AssemblyStudio.Runtime.Tests/InterruptRouterTests.cs`:
    - IVT stub initialization and IRET opcodes across all 256 vectors.
    - Program-installed vector detection.
    - AC-031 divide error halt at CS:IP with what/where/next runtime error message.
    - AC-029 unsupported INT 21h function 3Dh halt naming function 3Dh with what/where/next, no host file access.
    - CPU exceptions (INT 1, 2, 3, 4) with stable codes RTE002..RTE005.
    - Unsupported software interrupts (INT 10h, 13h, 16h, 2Fh) with RTE020.
    - Program-installed vector for INT 21h and divide error vectors through IVT and executes guest handler.
    - OS services delegation (Executed, WaitingForInput, UnsupportedService, fallback).

## Milestone 2 Status: IN PROGRESS (3/12 tasks done)
- `TASK-030`: Runtime memory, journal and machine — **Done**.
- `TASK-031`: .COM loader and PSP — **Done**.
- `TASK-032`: Interrupt router and runtime error policy — **Done**.
- Next up in Milestone 2:
  - `TASK-033`: Console device (`Ready`) — completing this unblocks `TASK-034` (DOS output and termination).
  - `TASK-040`: NASM runner (`Ready`)
  - `TASK-034`: DOS output and termination services (Blocked on `TASK-033`)

## Failed tests and known issues
- `DEF-001`: `MainWindowSmokeTests.View_menu_opens_with_shortcuts_and_reset_layout` in App.Tests can fail in non-interactive/background runners because WPF top-level Menu does not maintain submenu open state without active window focus. Recorded in `docs/07-testing/defects.md`.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-016.
- `AssemblyStudio.Runtime` references only `AssemblyStudio.Core`. No reference to UI or Engine.
- Fixed PSP segment is `0x0800`. Entry point is `0x0100`.
- Reserved stub segment is `0xF000`. Stubs contain `0xCF` (`IRET`).

## Exact next action
1. Optional / when requested: commit TASK-032 changes (`git add ...; git commit -m "TASK-032: Interrupt router and runtime error policy"`).
2. Run `/assembly-implement TASK-033` (Console device, unblocking TASK-034), or `/assembly-implement TASK-040` (NASM runner), or `/assembly-implement TASK-061` (themes).
3. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
