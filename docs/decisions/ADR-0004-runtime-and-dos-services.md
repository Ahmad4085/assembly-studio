# ADR-0004: Runtime environment and DOS services — in-process, own implementation

| Field | Value |
|---|---|
| Status | Approved |
| Version | 1.0 |
| Date | 2026-10-26 |
| Requirements addressed | FR-034, FR-038, FR-039, FR-040, FR-041, FR-042, FR-063, FR-064, FR-096, NFR-010, NFR-013, NFR-015, NFR-016 |
| Research | `docs/02-research/dos-services.md` (R-DOS), D-008, D-012. Answers OQ-003, OQ-011, OQ-022 (MVP part) |

## Context
A CPU core provides no DOS. The MVP needs text output, keyboard input and termination (D-008, D-012), with DOS 3.3/4.0 behavior
taken from the MIT-licensed MS-DOS source. User programs are untrusted (FR-096) and must not reach the host (NFR-013).

## Options considered
| Question | Option | Pros | Cons |
|---|---|---|---|
| Where emulation runs (OQ-011) | **In-process** | Lowest latency (POC-001: 0.091 µs per step). Simple debugging. Shared snapshots | An emulator bug that crashes the .NET process takes the IDE down [Hypothesis on likelihood] |
| | Separate process | Crash isolation | IPC for every step and snapshot. More complexity and latency. Duplicated state |
| DOS layer | **Own implementation of the subset** | Exactly the approved scope. Deterministic. Documented deviations | We write it |
| | Spice86 DOS / FreeDOS kernel | Broad coverage | Far beyond scope. License and dependency weight (research) |

## Decision
1. **In-process emulation.** Containment is by construction: the emulated program can only reach the host through services we
   implement, and the MVP implements none that touch files, devices or the network (NFR-013). Engine faults are caught at the debug
   session boundary and become session errors (NFR-010). The engine uses no recursion and no unbounded allocation, so a guest
   program can't trigger a stack overflow or out-of-memory in the host [Hypothesis, verified by fuzz/soak tests, AC-030/AC-045].
2. **Runtime (`AssemblyStudio.Runtime`):**
   - 1 MiB memory with 20-bit wrap and a write journal.
   - The .COM loader puts the PSP at a fixed, documented segment (**0x0800**, an arbitrary deterministic choice), the image at
     PSP:0100h, `CS=DS=ES=SS=PSP`, `IP=0100h`, `SP=FFFEh`, a word 0000h at FFFEh, and `AX=0000h`.
   - **BX, CX, DX, SI, DI, BP are set to 0000h** (a documented deviation from DOS leftovers, for NFR-016). The PSP fields follow the research table.
   - The IVT is initialized so each vector points to a stub address in a reserved area. The router recognizes "default vector"
     versus "program-installed vector".
3. **Interrupt routing (`IInterruptRouter`):**
   - For INT 20h/21h with the default vector, call `IOsServices`.
   - For CPU exceptions (INT 0/1/3/4) with the default vector, a runtime error halts execution (FR-038).
   - A vector the program changed runs normally through the IVT.
   - Unsupported functions halt with "INT xx AH=yy unsupported" (FR-040).
4. **DOS services (`AssemblyStudio.Os.Dos`):** INT 21h **00h, 01h, 02h, 07h, 08h, 09h, 0Ah, 4Ch** and **INT 20h**, with DOS 3.3/4.0
   semantics (AL return values, TAB/CR/BS handling, the 0Ah buffer format, cancel = Esc, BEL when the buffer is full). The research
   deviations apply: no 0Ah template keys in the MVP, Ctrl-C checked on every character in 01/02/08/09/0Ah, never in 07h, ^P
   unsupported, and the default INT 23h terminates with reason "Ctrl-C".
   **06h, 0Bh and 30h are not added** (OQ-022 stays open for post-MVP).
5. **Keyboard input:** when an input function finds the console queue empty, the router returns **`WaitingForInput`**. The CPU leaves
   CS:IP on the INT instruction, so it re-executes when input arrives. Input events are recorded so that replay is deterministic (NFR-016, AC-047).
6. **Console device:** an output buffer capped at 1 MB with the oldest output trimmed (NFR-015), an input queue, and a column
   counter for TAB expansion.
7. **Termination paths** (FR-041, AC-064): INT 20h, AH=00h (both require CS=PSP), AH=4Ch (exit code AL), and RET to PSP:0000. All
   report the termination path and exit code.

## Consequences
- Adding BIOS services (FR-044, P2) means another `IOsServices` implementation. The router design already supports it.
- Ctrl-C delivery from the UI is a design question (OQ-021).

## Validation
- Unit tests per DOS function against the research table. Program-level tests (NASM fixtures → load → run) for AC-022/023/029/031/064.
- Soak/fuzz: random byte images run under a budget never crash the host (NFR-010).

## Changelog
- 2026-10-26 v0.1: Proposed (SDLC-006).
- 2026-10-26 v1.0: **Approved** by the user (2026-10-26 18:54 +0500: "approve architecture v0.1 and ADR-0001..0009; reference machine OK").
