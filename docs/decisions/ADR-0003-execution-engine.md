# ADR-0003: Execution engine — custom managed 8086 core

| Field | Value |
|---|---|
| Status | Approved |
| Version | 1.0 |
| Date | 2026-10-26 |
| Requirements addressed | FR-033, FR-036, FR-037, FR-038, FR-046, FR-048, FR-049, FR-053, FR-056, FR-058, FR-060 (enabler), NFR-002, NFR-003, NFR-013, NFR-016, NFR-017, NFR-019 |
| Research | `docs/02-research/execution-engine.md` (R-ENG), `poc-001.md` (POC-001: feasible). Answers OQ-001, OQ-008 (engine part), OQ-015, OQ-016 |

## Context
Research rejected Unicorn (GPL-2.0, oldest model 486) and the DOSBox family (GPL, no hooks). It kept Spice86.Core only as a heavy
fallback. POC-001 showed a managed C# core passing **178,000/178,000** SingleStepTests 8086 cases for its subset, with
step + snapshot at 0.091 µs and about 48 M instructions/s [Measured].

## Options considered
| Option | Pros | Cons | License impact |
|---|---|---|---|
| **Custom C# core** | Exact 8086 semantics proven against hardware data. Hooks, determinism and snapshots designed in. No native binaries | We write and maintain about 300 opcode forms | Our code (permissive). Test data: SingleStepTests (MIT), test-only |
| Spice86.Core | DOS layer included, Apache-2.0 | 25 dependencies (LGPL Mt32emu, ASP.NET, Avalonia). In-process step API unknown | LGPL dependency review needed |
| Unicorn | Mature | GPL-2.0, no 8086 model | Fails the license gate (D-007) |

## Decision
1. Build **`AssemblyStudio.Engine.I8086`**, a managed implementation of `ICpu` covering the **documented 8086 instruction set**
   (FR-036), validated per opcode against **SingleStepTests 8086** (Intel P80C86A-2) with the 8088 set as a secondary oracle.
2. **Interrupt interception:** before vectoring, `INT n`, CPU exceptions and the like call `IInterruptRouter` (ADR-0004). The router
   either handles the call or lets the CPU vector through the IVT with real 8086 semantics (push FLAGS/CS/IP, clear IF/TF).
3. **Undefined flags (OQ-015):** the engine computes a **deterministic** value and reports, per step, the mask of flags left
   undefined by the last instruction. The UI shows those flags as "undefined" (FR-048). Tests compare with the SingleStepTests
   `flags-mask`. Emulating silicon-exact undefined values is **not** a goal for the MVP.
4. **Undocumented and alias opcodes** (e.g. 60–6F aliases, `C0/C1/C8/C9`, `D6 SALC`, `0F POP CS`): **halt with "unsupported
   instruction"** (FR-037), documented as a deviation (FR-045). `HLT` ends the session as "halted". `WAIT` is a no-op (no 8087).
   `LOCK` is ignored. `C6`/`C7` with reg /1–/7 behave as /0 (measured in POC-001).
5. **State and snapshots:** `CpuState` is an immutable record struct. The memory owned by the runtime keeps a **per-step write
   journal** (address, old value) so it can report changed bytes (FR-056), keep the execution history (FR-058) and later support
   reverse stepping (FR-060, P2), without full 1 MiB copies (161 µs each, measured).
6. **Disassembly** for the current-instruction view (FR-053) and the history (FR-058) uses **Iced** (MIT, NASM formatter, 16-bit).
   Its `CpuidFeature` tags flag non-8086 encodings.
7. **Process model:** in-process (see ADR-0004 for the isolation rationale).

## Consequences
- Test data (≈ 39 MB for the subset, more for all opcodes) is fetched by a pinned script and git-ignored (ADR-0008).
- Instruction coverage grows in planned increments. Each opcode family is "done" only when its SingleStepTests file passes 100%.
- Public mutable fields from the POC are not carried over. The product API exposes snapshots, not live arrays (ADR-0002).

## Validation
- Per-opcode SingleStepTests pass rate 100%, plus the hand-written spec tests (`docs/sdlc/guides/cpu-instruction-testing.md`) (NFR-017, AC-058).
- A benchmark in CI-like conditions confirms NFR-002/003 on the reference machine (AC-056).
- **Unverified:** Iced 1.21.0 on .NET 10 and whether `INTEL8086_ONLY` covers POP CS. Validated by the first disassembler task test.

## Changelog
- 2026-10-26 v0.1: Proposed (SDLC-006).
- 2026-10-26 v1.0: **Approved** by the user (2026-10-26 18:54 +0500: "approve architecture v0.1 and ADR-0001..0009; reference machine OK").
