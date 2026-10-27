# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## Next: TASK-012 Data transfer instructions

| Field | Value |
|---|---|
| Status | **Ready** (approved; TASK-011 Done) |
| Owner | — (unassigned. The next agent sets itself as owner when starting) |
| Started | — |
| Definition | `docs/05-planning/backlog.md` § TASK-012 (goal, scope, tests, DoD) |
| Procedure | `docs/sdlc/procedures/assembly-implement.md` (invoke with `/assembly-implement TASK-012`) |
| Depends on | TASK-011 (Done ✔) |

**Parallel user action:** TASK-006 (clean test VMs) can start any time.

---

## Last completed: TASK-011, Engine skeleton: decode, ModR/M, memory access (Done 2026-10-27 12:45 +0500, OpenCode (GLM-5.3-Flash Free))
- `src/AssemblyStudio.Engine.I8086/`: `Cpu8086 : ICpu` — pure decode (state untouched until a decision), opcode dispatch with only `CD nn` (INT imm8) in this task as the interception-contract carrier; every other opcode (including aliases `0F`, `60–6F`, `C0/C1/C8/C9`, `D6`) halts `Halted(UnsupportedInstruction)` with CS:IP restored to the instruction start and full state byte-identical (FR-037, AC-028 engine part).
- `Decoder` (internal, pure): prefix parsing (segment overrides `26/2E/36/3E` last-wins, `F2/F3` REP capture, `F0` LOCK ignored), full 8086 ModR/M effective-address table with default segments (rm 2/3 and rm 6 mod≠0 → SS, else DS), disp8 sign-extension, disp16, `mod 00 rm 110` direct displacement, 16-bit EA and IP fetch wrap.
- Interrupt contract tested three ways: handled result returned as-is; `WaitingForInput` pass-through (CS:IP stays on the INT); `null`/no router → real vectoring (push FLAGS/CS/return-IP with 16-bit SP wrap, IF/TF cleared, CS:IP loaded from the IVT).
- 37 deterministic hand tests in `tests/AssemblyStudio.Engine.I8086.Tests` (full-state assertions, raw bytes, no assembler); `FakeMemory` double implementing the documented IMemory semantics (1 MiB, 20-bit wrap, word wrap within segment).
- No new dependencies (Iced deferred to TASK-019). No implementations of instruction families (TASK-012+).
- Release build: 0 warnings, 0 errors. Solution tests: 103/103 passed (9 test projects).

---

## Last completed: TASK-010, Core abstractions v1 (Done 2026-10-27 12:21 +0500, OpenCode (GLM-5.3-Flash Free))
- `src/AssemblyStudio.Core/`: `FlagBit`, `CpuState` (record struct, 14 ushort fields), `StepOutcome`/`HaltReason`/`StepResult`, `IMemory` (+ `MemoryChange` write-journal drain), `IInterruptRouter` (`StepResult? Intercept`, null = vector through IVT), `ICpu` (`State`/`SetState`/`StepInstruction`), `IDisassembler`, `IConsoleDevice`, `IMachine`, `IOsServices` (`HandleService(vector, state, IMachine)`), `SessionState`/`CurrentInstruction`/`MemoryWindow`/`StackWindow`/`MachineSnapshot` (ADR-0007 v1.1 fields, types only). XML docs on every public abstraction, citing ADR/FR.
- No implementations added (out of scope): `IAssembler`/`ISourceMap` (TASK-040/043) and `IDebugSession` (TASK-050) intentionally not defined here.
- 8 model tests in `tests/AssemblyStudio.Core.Tests`: CpuState equality/hash/with-expressions/value-copy, MachineSnapshot immutability (CPU advancing after publish leaves the snapshot unchanged), v1.1 field shape.
- Renamed `ICpu.Step()` → `StepInstruction()` (CA1716 reserved keyword, warnings-as-errors). No suppressions added.
- Release build: 0 warnings, 0 errors. Solution tests: 66/66 passed (9 test projects).

---

## Last completed: TASK-005, Bundled NASM tooling layout (Done 2026-10-27 12:00 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- `build/fetch-nasm.ps1`: Fetches and extracts pinned NASM 3.02 win64 binary into `tools/nasm/` (git-ignored per ADR-0005).
- Verifies archive SHA-256 (`161d0bfa...`), extracts `nasm.exe`, `ndisasm.exe`, and `LICENSE` (BSD-2-Clause).
- Verifies extracted file hashes and writes deterministic `tools/nasm/SHA256SUMS.txt` and `tools/nasm/SOURCE.txt` (no timestamps).
- `NasmLocator` test helper in `tests/AssemblyStudio.Toolchain.Nasm.Tests` with 6 passing tests: locating nasm, verifying version 3.02, ndisasm locator, BSD-2-Clause license & source metadata, SHA-256 manifest integrity, and actionable failure on missing tool.
- Release build: 0 warnings, 0 errors. Solution tests: 58/58 passed (9 test projects). `build/coverage.ps1`: exit 0.

---

## Earlier: TASK-004, Coverage tooling (Done 2026-10-27 10:48 +0500, Claude Code)
- `coverlet.MTP` 10.1.0 (MIT) pinned in `Directory.Packages.props`, referenced by every test project via `Directory.Build.props`.
- `build/coverage.ps1`: per-project Cobertura, merged per product assembly, `artifacts/coverage/summary.md`; exit 1 on a failed or missing run.
- Skeleton report: 9 Cobertura files from 9 projects, all assemblies "n/a (no coverable lines)".
