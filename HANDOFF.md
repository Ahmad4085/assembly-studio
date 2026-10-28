# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-28 16:13 +0500 by Claude Code (Opus 5.5), after fixing the TASK-020 timing test and committing TASK-020 (TASK-020 itself by Antigravity (IDE, Gemini 3.8 Flash), 16:00) |
| Current task | **None in progress. Milestone 1 Complete. Next eligible: M2 `TASK-030` Runtime memory, journal and machine; M4 `TASK-061` Themes, tokens and contrast test; recommended: `/assembly-test`** |
| Last completed | TASK-020 Engine conformance and performance gate (Done 2026-10-28 16:00 +0500) |
| Git state | HEAD `ce3f6ea` (TASK-020 committed, including the timing-test fix). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-28 15:58 +0500).
- `& ...\AssemblyStudio.Engine.I8086.Tests.exe -class "*DeterminismTests*" -class "*EnginePerformanceTests*" -class "*EngineConformanceReportTests*"` → **22/22 passed** in 48.5s (2026-10-28 15:58 +0500).
- `& ...\AssemblyStudio.Engine.I8086.Tests.exe` → **611/611 passed** in 134.7s (283 SingleStepTests oracle files at 100% + 328 unit/hand tests).
- `dotnet test tests/AssemblyStudio.ArchitectureTests/AssemblyStudio.ArchitectureTests.csproj -c Release` → **39/39 passed** in 3.5s (2026-10-28 15:53 +0500).
- `dotnet run --project build/bench/EngineBench.csproj -c Release` → Mean step+snapshot latency **0.178 µs**, throughput **6.42–7.29 M/s** (2026-10-28 15:48 +0500).
- Solution tests: **709/710 passed** (1 pre-existing DEF-001 in App.Tests; 2026-10-28 15:58 +0500).
- **2026-10-28 16:12 +0500 (Claude Code):**
  - Fix: the throughput test could fail under CPU contention in full runs. Both timing tests now run in the non-parallel `TimingSensitive` collection; throughput is the best of 5 runs, asserted at ≥ 1.0 M instructions/s.
  - `dotnet test --solution AssemblyStudio.slnx -c Release` → **710/710 passed**, two consecutive runs. DEF-001's test passed in all three of my runs; DEF-001 left open.

## Changes since the previous handoff
- **TASK-020 Engine conformance and performance gate** completed (Milestone 1 finalized):
  - Verified full 8086 hardware oracle suite: all 283 SingleStepTests oracle files covering all in-scope 8086 instructions pass at **100.0%** (~566,000 cases).
  - Verified secondary 8088 oracle dataset across 17 opcode files covering all instruction categories (17,000 cases at 100%).
  - Verified complete 256 opcode table classification: 210 Supported, 7 Prefix, 39 Unsupported. All 39 unsupported opcodes (including 186/286/386 extensions, coprocessor ESC, port I/O, SALC) halt with `UnsupportedInstruction` with byte-identical pre-step state preserved (FR-037).
  - Built standalone benchmark tool in `build/bench/` and measured on reference machine (D-014: Intel Core i5-8265U):
    - **Step + Register Snapshot Latency (NFR-002):** Mean **0.178 µs**, p50 **0.200 µs**, p95 **0.300 µs**, p99 **0.300 µs**, Max 474.7 µs (OS jitter). Budget: <= 100 ms (100,000 µs); engine consumption is **0.0002%** of budget.
    - **Worst-case Snapshot Latency:** Mean **91.9 µs** per full 1 MiB memory copy.
    - **Free-Run Throughput (NFR-003):** **6.42 – 7.29 M instructions/sec** across 3 runs of 50M instructions (Target: >= 1.00 M/s; **6.4x – 7.3x headroom**).
  - Verified deterministic execution over long traces (NFR-016, AC-047) in `DeterminismTests.cs`: multi-instruction routines produce 100% bit-for-bit identical CPU states, flags, instruction IPs, and memory traces across runs.
  - Authored formal engine conformance report `TR-2026-10-28-01` in `docs/07-testing/engine-conformance-report.md` (FR-036, AC-058).

## Milestone 1 Status: COMPLETE
- All 11 tasks in Milestone 1 (TASK-010 through TASK-020) are **Done**:
  - `TASK-010`: Core abstractions v1 (`ICpu`, `CpuState`, `IMemory`, `MachineSnapshot`, etc.)
  - `TASK-011`: Engine skeleton (pure decode, prefix handling, ModR/M, segment wraps)
  - `TASK-012`: Data transfer instructions (MOV, XCHG, LEA, LDS, LES, PUSH, POP, etc.)
  - `TASK-013`: Arithmetic instructions (ADD, ADC, SUB, SBB, CMP, INC, DEC, NEG)
  - `TASK-014`: Logic, shift and rotate instructions (AND, OR, XOR, TEST, NOT, shifts/rotates)
  - `TASK-015`: Multiply, divide, BCD and conversions (MUL, DIV, BCD, CORD microcode flags)
  - `TASK-016`: Control transfer and interrupts (Jumps, calls, returns, INT, IRET, IVT)
  - `TASK-017`: String instructions and REP prefixes (MOVS, CMPS, STOS, LODS, SCAS)
  - `TASK-018`: Flag ops, misc and full opcode table (CLC, STC, HLT, WAIT, OpcodeTable)
  - `TASK-019`: Disassembler adapter (Iced 1.21.0, 8086 filter, NASM format)
  - `TASK-020`: Engine conformance and performance gate (report TR-2026-10-28-01)

## Failed tests and known issues
- `DEF-001`: `MainWindowSmokeTests.View_menu_opens_with_shortcuts_and_reset_layout` fails in non-interactive/background runners because WPF top-level Menu does not maintain submenu open state without active window focus. Recorded in `docs/07-testing/defects.md`.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-016.
- Core has no project references; Engine.I8086 references only Core.
- Every unsupported opcode halts with `UnsupportedInstruction` and pre-step state preserved (FR-037).
- Disassembler returns `null` on invalid or non-8086 instructions.

## Exact next action
1. Optional / when requested: commit TASK-020 changes (`git add ...; git commit -m "TASK-020: Engine conformance and performance gate"`).
2. Recommend running `/assembly-test` for formal Milestone 1 test report verification, OR proceed directly to `/assembly-implement TASK-030` (start Milestone 2: Runtime memory, journal and machine).
3. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
