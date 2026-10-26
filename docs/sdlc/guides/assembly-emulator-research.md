# Assembler, emulator and runtime research guidance

## 1. Keep the layers separate
| Layer | Question it answers | Typical evidence needed |
|---|---|---|
| Assembler | Can it turn our supported syntax into bytes for the target, with listings/symbols for source mapping? | Output formats (e.g. flat `.COM`, MZ `.EXE`), `BITS 16`/`CPU 8086` support, listing/map/debug output, exit codes and error format, license for bundling |
| Decoder/disassembler | Can we show instructions at addresses? | Supported modes/ISAs, .NET availability |
| CPU engine | Can it execute instruction-by-instruction with full state access? | Supported CPU models and modes, single-step, register/flag/memory read-write, hooks on interrupts/ports/memory, determinism, embeddability from C#, performance |
| Runtime environment | Memory map, loader (.COM at PSP:0100h, MZ relocation), I/O ports, timer | Who provides each piece. Usually us |
| OS/BIOS services | INT 21h/10h/16h/1Ah behavior | Usually **not** provided by a CPU emulator. Full-system emulators provide them but are rarely embeddable |
| Debugger/visualizer | Breakpoints, history, state snapshots | Needs engine hooks. History needs state diffs or snapshots |

A component that covers one layer does not cover the next. State exactly which layers each option covers.

## 2. Candidate list (starting point, all `[Unverified]` until checked)
Verify existence, current license, maintenance and capabilities from primary sources before use.
- **Assemblers:** NASM, YASM, FASM, JWasm/UASM (MASM-compatible). MASM/TASM are proprietary.
  Treat as not redistributable unless proven otherwise.
- **Execution engines:** Unicorn Engine (CPU-only, QEMU-derived, check license and .NET bindings),
  custom managed 8086 core in C#, existing open-source C# 8086/x86 emulators (assess quality and tests),
  full-system emulators (DOSBox family, 86Box/PCem) which are typically GPL and not designed as
  embeddable libraries.
- **Decoding/encoding in .NET:** Iced (x86/x64 decoder/disassembler/encoder), Capstone (disassembler) and its bindings.
- **Reference test data:** published per-instruction CPU test suites for 8088/8086 (e.g. single-step
  JSON test collections). Check the license and the hardware model they were captured from.

## 3. Evaluation questions per option
1. Exact license (SPDX) and obligations. Compatible with the product license (OQ-007)? Static vs dynamic linking or separate-process implications?
2. Last release and commit activity, issue responsiveness, bus factor.
3. Which CPU models/modes are actually supported and tested (8086 vs 386+ behaviors differ,
   e.g. flag results, `PUSH SP`, shift count masking, undefined flags)?
4. Can we single-step, read and write all registers, flags and memory, and hook interrupts, I/O ports and invalid opcodes?
5. Determinism: same input → same state trace?
6. Integration from C#/.NET on Windows x64: managed, P/Invoke, separate process? Native binaries to ship?
7. Offline: no runtime downloads or telemetry.
8. Path to 32/64-bit later without replacing the UI.

## 4. Recording rules
- Use `docs/templates/research-evaluation.md`. Evidence labels per `sdlc-documentation`.
- Distinguish "the docs say it supports X" `[Verified: docs]` from "we ran X" `[Measured]`.
- Capabilities that matter for a decision but are only documented become POC candidates.
- Never present a library as covering DOS services, debugging or 8086-exact semantics without evidence.
