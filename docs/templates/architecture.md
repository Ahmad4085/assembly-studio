# Assembly Studio — System Architecture

| Field | Value |
|---|---|
| Document | System architecture |
| Status | Draft |
| Version | 0.1 |
| Last updated | YYYY-MM-DD |
| Depends on | requirements.md v<x>, docs/02-research/* v<x> |

## 1. Context and goals
Quality attributes that drive the design (from NFRs) and key constraints.

## 2. Component overview
Diagram (Mermaid or ASCII) of components and dependencies. The dependency direction must point
**toward abstractions**: UI → application services → engine/runtime interfaces.

| Component | Responsibility | Must NOT do | Public interface | Project/assembly | Requirements |
|---|---|---|---|---|---|
| Shell / UI (WPF) | | Execute or decode instructions | | | |
| Editor | | | | | |
| Assembler integration | | Execute code | | | |
| CPU execution engine | | Know about UI or DOS | | | |
| Runtime environment (memory, loader, devices) | | | | | |
| OS services (DOS/BIOS) | | | | | |
| Debugger / session control | | | | | |
| Visualization | | Fabricate or cache stale state as current | | | |
| Content (tutorials, samples) | | | | | |

## 3. Key flows
Edit → assemble → load → run/step → breakpoint → inspect → output. Sequence per flow, including
which component owns each state transition.

## 4. Execution model and state
Threading (engine off the UI thread), cancellation, run/pause/step semantics, state snapshots,
execution history storage, determinism.

## 5. Data and file formats
Projects, settings, listings/symbol maps, execution traces, user content locations.

## 6. Error handling
Assembler errors, CPU faults/unsupported opcodes, unsupported interrupts, runaway programs
(time/step limits), I/O errors, crash reporting (offline).

## 7. Security boundaries
External processes (assembler) invocation, path handling, untrusted user programs (sandboxed in
the emulator; no host access except via defined services), file system access, no network.

## 8. Performance requirements and budgets
Mapped to NFR IDs (e.g. step latency, instructions/second, UI frame budget, startup time).

## 9. Extensibility path
How additional CPUs (x86, x86-64), assemblers and OS services plug in without UI rewrites.

## 10. Testing strategy
Unit (instruction semantics), integration (assembler ↔ loader ↔ engine), golden/reference tests,
UI automation scope, what is manual.

## 11. Decisions
| ADR | Title | Status |
|---|---|---|
| ADR-0001 | | |

## 12. Risks and open questions
| ID | Description | Impact | Mitigation / POC |
|---|---|---|---|

## Changelog
- YYYY-MM-DD v0.1: Initial draft.
