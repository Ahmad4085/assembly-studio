# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-28 18:05 +0500 by Antigravity (IDE, Gemini 3.8 Flash), after completing TASK-033 |
| Current task | **None in progress. Milestone 2 in progress. Next eligible: M2 `TASK-034` Basic DOS services: output and termination (UNBLOCKED); M2 `TASK-040` NASM runner; M4 `TASK-061` Themes, tokens and contrast test** |
| Last completed | TASK-033 Console device (Done 2026-10-28 18:05 +0500) |
| Git state | HEAD `20ff911` (uncommitted changes for TASK-033). Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-28 18:03 +0500).
- `& ...\AssemblyStudio.Runtime.Tests.exe` → **79/79 passed** in 1.10s (2026-10-28 18:03 +0500).
- `& ...\AssemblyStudio.ArchitectureTests.exe` → **39/39 passed** in 1.58s (2026-10-28 18:03 +0500).
- Core unit test suite executables (Core, Os.Dos, Nasm, Debugging, Content) → **18/18 passed** (2026-10-28 18:03 +0500).
- `& ...\AssemblyStudio.Engine.I8086.Tests.exe -class "*DeterminismTests*" -class "*EnginePerformanceTests*" -class "*IcedDisassemblerTests*"` → **80/80 passed** in 12.8s (2026-10-28 18:03 +0500).

## Changes since the previous handoff
- **TASK-033 Console device** completed:
  - Implemented `InputEvent` in `src/AssemblyStudio.Runtime/Console/InputEvent.cs`:
    - Immutable record struct `InputEvent(ulong InstructionCount, byte Value)` stamping input events with CPU execution instruction count (ADR-0004 §5, NFR-016).
  - Implemented `ConsoleDevice` in `src/AssemblyStudio.Runtime/Console/ConsoleDevice.cs`:
    - 1 MiB output buffer (`DefaultMaxCapacity = 1024 * 1024` bytes, NFR-015, AC-045).
    - Circular buffer with O(1) appending and automatic oldest content trimming without heap reallocations.
    - Snapshot delta API: `TakeOutput()` returns pending output string and clears delta (ADR-0007 §2).
    - Full text retrieval: `ToByteArray()` and `GetAllText()` decoding via IBM PC CP437.
    - DOS column counter (ADR-0004 §6, R-DOS §2):
      - CR (0Dh) resets column to 0.
      - BS (08h) decrements column without underflowing.
      - TAB (09h) advances column to next multiple of 8 (`(col + 8) & ~7`).
      - BEL (07h) increments `BelCount` without advancing column.
      - Printable characters (>= 20h) advance column by 1.
      - Control characters (< 20h) do not advance column.
    - Input queue with event recording and deterministic replay (NFR-016, AC-047):
      - Live mode: `EnqueueInput`, `TryReadInput` dequeues and appends stamped `InputEvent` to `RecordedInputs`.
      - Replay mode: `SetReplayInputs` loads recorded events; `TryReadInput` returns bytes only when `CurrentInstructionCount` reaches the stamped instruction count.
    - Reset / Clear: `Clear()` resets output, delta, column, BEL, and live queue, preserving recorded log (FR-063).
  - Authored 22 unit tests in `tests/AssemblyStudio.Runtime.Tests/ConsoleDeviceTests.cs`:
    - Delta draining and full text retrieval.
    - Column counter arithmetic (CR, BS non-underflow, TAB stops 8/16/24, printable, LF/control bytes).
    - BEL character counting and bell ringing.
    - AC-045 cap and trim at arbitrary capacity and under 1 MB flood.
    - Input queue FIFO ordering and empty checks.
    - Input recording with instruction counts.
    - AC-047 deterministic replay verifying step-synchronized byte release.
    - Clear and reset state preservation.

## Milestone 2 Status: IN PROGRESS (4/12 tasks done)
- `TASK-030`: Runtime memory, journal and machine — **Done**.
- `TASK-031`: .COM loader and PSP — **Done**.
- `TASK-032`: Interrupt router and runtime error policy — **Done**.
- `TASK-033`: Console device — **Done**.
- Next up in Milestone 2:
  - `TASK-034`: Basic DOS services: output and termination (`Ready` — **unblocked by TASK-031 + 032 + 033!**)
  - `TASK-040`: NASM runner (`Ready`)
  - `TASK-035`: DOS character input services (Blocked on `TASK-034`)

## Failed tests and known issues
- `DEF-001`: `MainWindowSmokeTests.View_menu_opens_with_shortcuts_and_reset_layout` in App.Tests can fail in non-interactive/background runners because WPF top-level Menu does not maintain submenu open state without active window focus. Recorded in `docs/07-testing/defects.md`.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-016.
- `AssemblyStudio.Runtime` references only `AssemblyStudio.Core`. No reference to UI or Engine.
- Console buffer cap is 1 MB with oldest output trimmed.
- Input events stamped with instruction count for replay determinism.

## Exact next action
1. Optional / when requested: commit TASK-033 changes (`git add ...; git commit -m "TASK-033: Console device"`).
2. Run `/assembly-implement TASK-034` (Basic DOS services: output and termination — unblocked by TASK-033!), or `/assembly-implement TASK-040` (NASM runner), or `/assembly-implement TASK-061` (themes).
3. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
