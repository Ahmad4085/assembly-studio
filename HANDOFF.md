# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-28 17:28 +0500 by Antigravity (IDE, Gemini 3.8 Flash), after committing TASK-030 and TASK-031 |
| Current task | **None in progress. Milestone 2 in progress. Next eligible: M2 `TASK-032` Interrupt router; M2 `TASK-033` Console device; M2 `TASK-040` NASM runner; M4 `TASK-061` Themes, tokens and contrast test** |
| Last completed | TASK-031 .COM loader and PSP (Done 2026-10-28 16:55 +0500, committed in `c582b25`) |
| Git state | HEAD `c582b25` (TASK-030 and TASK-031 committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-28 16:53 +0500).
- `& ...\AssemblyStudio.Runtime.Tests.exe` → **39/39 passed** in 4.6s (2026-10-28 16:51 +0500).
- `& ...\AssemblyStudio.ArchitectureTests.exe` → **39/39 passed** in 0.53s (2026-10-28 16:52 +0500).
- Core unit test suite executables (Core, Os.Dos, Nasm, Debugging, Content) → **18/18 passed** (2026-10-28 16:38 +0500).
- `& ...\AssemblyStudio.Engine.I8086.Tests.exe -class "*DeterminismTests*" -class "*EnginePerformanceTests*" -class "*IcedDisassemblerTests*"` → **80/80 passed** in 12.1s (2026-10-28 16:39 +0500).

## Changes since the previous handoff
- **TASK-031 .COM loader and PSP** completed:
  - Implemented `ProgramSegmentPrefix` in `src/AssemblyStudio.Runtime/Loading/ProgramSegmentPrefix.cs`:
    - Full 256-byte (0x100) structure initialized at `pspSegment:0000h` per FR-034 and research R-DOS §3:
      - 00h: `CD 20` (INT 20h termination).
      - 02h: Top of memory segment (`0x1800` for 64 KiB block).
      - 05h: CALL far to `PSP:0050h` (`9A 50 00 [psp_lo] [psp_hi]`).
      - 0Ah..15h: Saved INT 22h, 23h, 24h vectors copied from IVT.
      - 16h: Parent PSP segment (`pspSegment`).
      - 18h..2Bh: Job file table (20 bytes: handles 0..4 open, 5..19 unused 0xFF).
      - 2Ch: Environment segment (`0x0000`).
      - 50h: `CD 21 CB` (`INT 21h; RETF`).
      - 5Ch..6Bh: Unopened FCB 1 (zeroed).
      - 6Ch..7Fh: Unopened FCB 2 (zeroed).
      - 80h..FFh: Command tail / DTA (`00 0D` when empty; length byte, characters, CR terminator when provided; max 126 chars).
  - Implemented `ComLoader` and `ComLoadResult` in `src/AssemblyStudio.Runtime/Loading/`:
    - Places PSP at `0x0800`.
    - Loads binary image at `0x0800:0100h`.
    - Enforces maximum allowable .COM image size: 65,278 bytes (`0xFEFE = 0xFFFE - 0x0100`), ensuring the program cannot overwrite the stack return word.
    - Stores word `0000h` at `SS:SP` (`0x0800:FFFEh`), providing the return address for near `RET` to `PSP:0000h` (`INT 20h`) to terminate cleanly (AC-064).
    - Clears memory journal after load so initial baseline state generates zero change highlights (FR-056).
    - Initializes CPU registers: `CS=DS=ES=SS=0x0800`, `IP=0x0100`, `SP=FFFEh`, `AX=BX=CX=DX=SI=DI=BP=0x0000`, `Flags=0x0002` (AC-021, ADR-0004 §2).
  - Authored 9 unit tests in `tests/AssemblyStudio.Runtime.Tests/ComLoaderTests.cs`:
    - AC-021 verified verbatim.
    - Byte-level PSP verification against the research table across all 14 fields.
    - Command tail formatting, CR termination, and 126-char truncation.
    - IVT vector persistence.
    - Near RET to INT 20h termination path.
    - Max image size boundary checks.
    - Custom program segment verification.

## Milestone 2 Status: IN PROGRESS (2/12 tasks done)
- `TASK-030`: Runtime memory, journal and machine — **Done**.
- `TASK-031`: .COM loader and PSP — **Done**.
- Next up in Milestone 2:
  - `TASK-032`: Interrupt router and runtime error policy (`Ready`)
  - `TASK-033`: Console device (`Ready`)
  - `TASK-040`: NASM runner (`Ready`)

## Failed tests and known issues
- `DEF-001`: `MainWindowSmokeTests.View_menu_opens_with_shortcuts_and_reset_layout` in App.Tests can fail in non-interactive/background runners because WPF top-level Menu does not maintain submenu open state without active window focus. Recorded in `docs/07-testing/defects.md`.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-016.
- `AssemblyStudio.Runtime` references only `AssemblyStudio.Core`. No reference to UI or Engine.
- Fixed PSP segment is `0x0800`. Entry point is `0x0100`.
- Initial stack word at `0x0800:FFFEh` is `0x0000`.

## Exact next action
1. Run `/assembly-implement TASK-032` (Interrupt router and runtime error policy), or `/assembly-implement TASK-033` (Console device), or `/assembly-implement TASK-040` (NASM runner), or `/assembly-implement TASK-061` (themes).
2. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
