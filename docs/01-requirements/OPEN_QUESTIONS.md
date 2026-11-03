# Open Questions and Assumptions

| Field | Value |
|---|---|
| Document | Open questions and assumptions |
| Status | Approved |
| Version | 2.5 |
| Last updated | 2026-10-26 |
| Depends on | `docs/00-project-overview/project-overview.md` v0.3. `DECISIONS.md` D-006 to D-009 |
| Requirements set | Approved as one set together with `PRODUCT_REQUIREMENTS.md` |

These questions must not be answered by guessing. Each names the phase that resolves it. Questions
marked High must be answered (or explicitly accepted as open) before the architecture is approved.
OQ-001 to OQ-007 were carried over from `docs/00-project-overview/project-overview.md`.

## Open questions
| ID | Question | Impact | Resolved in | Status | Related |
|---|---|---|---|---|---|
| OQ-001 | Which 8086 execution engine fits the requirements: an existing emulator library, a custom C# 8086 core, or a hybrid? | High | Research → ADR | Resolved 2026-10-26 by ADR-0003 (custom managed 8086 core) | FR-033, FR-036 |
| OQ-002 | NASM integration: is redistribution of NASM binaries permitted and what are the obligations (believed BSD-2-Clause, unverified)? Minimum version? Does `CPU 8086` reject all later instructions? What listing/map output is available? | High | Research | Answered 2026-10-26 (research): BSD-2-Clause, redistribution allowed with notice. Minimum 3.02. `cpu 8086` works but user source can override it (R-ASM) | FR-023, FR-024, FR-027, FR-028 |
| OQ-003 | Exact DOS service list and semantics for the MVP text I/O class (D-008): echo behavior, Ctrl-C/Ctrl-Break handling, buffered-input editing, initial PSP contents. | Medium | Research | Answered 2026-10-26 (research): DOS 3.3/4.0 semantics with documented deviations (`dos-services.md`) | FR-034, FR-039, FR-064 |
| OQ-004 | Executable formats: confirm .COM-only for MVP. Can NASM produce MZ .EXE without a separate linker, and at what cost? | Medium | Research | Answered 2026-10-26 (research): .COM confirmed for MVP. MZ via `exebin.mac` without a linker (P2) | FR-026, FR-034, FR-043 |
| OQ-005 | Target .NET version (10 LTS preferred) and WPF support on the supported Windows versions. SDK availability (BLK-001). | Medium | Research → ADR | Resolved 2026-10-26 by ADR-0001 (.NET 10 LTS + WPF) | FR-086, NFR-025 |
| OQ-006 | Initialize a Git repository? | Low | User | Resolved 2026-10-26 (D-004) | — |
| OQ-007 | Product distribution model and license. | High | User | Partially resolved: permissive open source (D-007). Specific license (e.g. MIT or Apache-2.0) open, needed before first release | NFR-030 |
| OQ-008 | Can the selected engine expose all registers, flags, memory and per-instruction state, support breakpoints and execution control, and snapshot/restore state? Is it deterministic? | High | Research / POC | Partially answered 2026-10-26 (POC-001): full register/flag/memory access, single-step, INT interception hook and deterministic traces shown. Breakpoints/snapshots designed in the architecture | FR-046 to FR-062, NFR-016 |
| OQ-009 | How are addresses mapped to source lines (NASM listing or map files), including macros and `times`? How reliable is it? | Medium | Research / POC | Answered 2026-10-26 by POC-002: listing + map is sufficient with limitations (macros → invoking line, includes, times/align verified). Limitations listed in `docs/02-research/poc-002.md` | FR-028, FR-052, FR-053 |
| OQ-010 | Minimum supported Windows 10/11 builds for the chosen .NET/WPF. x64 only, or also ARM64? | Medium | Research | Resolved 2026-10-26 by D-011: Windows 11 24H2+ and Windows 10 Enterprise LTSC 21H2 (x64) supported. Arm64 optional build, not required | NFR-025 |
| OQ-011 | Safest way to execute untrusted programs: is in-process emulation enough, or is a separate process needed for fault isolation? | Medium | Architecture | Resolved 2026-10-26 by ADR-0004 (in-process, containment by construction) | NFR-010, NFR-013, FR-096 |
| OQ-012 | Which P0/P1 requirements fit the first release? Which P1 items may be deferred? | Medium | Plan (with user) | Open | All P1 |
| OQ-013 | Deployment model: self-contained vs framework-dependent .NET. Installer technology (e.g. Inno Setup). Per-user vs per-machine install, and admin rights. | Medium | Research → ADR | Resolved 2026-10-26 by ADR-0009 (self-contained, Inno Setup per-user) | FR-083, FR-086, NFR-027 |
| OQ-014 | Reference machine specification for performance targets. | Medium | Research | Resolved 2026-10-26 by D-014 (i5-8265U, 16 GB, Windows 11 as minimum reference) | NFR-001 to NFR-007 |
| OQ-015 | Undefined flags: emulate a specific 8086's actual results, or mark them undefined in the UI? | Medium | Research → ADR | Resolved 2026-10-26 by ADR-0003 (deterministic value + undefined mask shown in UI) | FR-048, NFR-017 |
| OQ-016 | Exact CPU semantics target (8086 vs 8088, behaviors that changed in later CPUs such as shift-count masking and `PUSH SP`) and the source of expected values (documentation, reference test suites and their licenses). | Medium | Research | Resolved 2026-10-26 by ADR-0003 (documented 8086, SingleStepTests 8086 oracle) | FR-027, FR-036, NFR-017 |
| OQ-017 | Feasibility and cost of reverse stepping (snapshots or recording). | Low | Research (Post-MVP) | Researched 2026-10-26: snapshot support to be designed into the core (P2) | FR-060 |
| OQ-018 | Is a NASM-syntax hint enough for students whose courses use MASM/TASM syntax, or is a MASM-compatible mode needed later? | Low | Post-MVP review | Open | FR-021, D-006 |
| OQ-019 | Development tooling: .NET SDK not installed (BLK-001). Which IDE (Rider is present) and CI setup? | Low | Before phase 6 | Open: .NET SDK still not installed. Now also blocks POCs (`research-summary.md` D3) | — |
| OQ-020 | Code signing for the installer and executables (Windows SmartScreen warnings for an unsigned open-source app); free options? | Medium | Architecture / release | Open (raised by research 2026-10-26) | FR-083, NFR-027 |
| OQ-021 | Keyboard conflict: Ctrl+C is 'copy' in the IDE but Ctrl-C/Ctrl-Break in a DOS program. How are they routed (console focus only)? | Low | Design | Resolved 2026-10-26 by `docs/04-design/ui-spec.md` §6 (D-015) | FR-064, FR-091 |
| OQ-022 | Add DOS functions beyond D-008: AH=00h (DOS-1 terminate), 06h (direct console I/O), 0Bh (input status), 30h (version)? | Medium | Architecture (needs a requirements change) | Partially resolved: AH=00h added (D-012). 06h, 0Bh, 30h not in MVP per ADR-0004 (2026-10-26). Open for post-MVP | FR-039, D-008 |
| OQ-023 | `MachineSnapshot` lacks current-instruction decode, memory and stack windows, and history entries needed by UI-007/008/009/013 | Medium | Design (ADR-0007 amendment) | Resolved 2026-10-26 by ADR-0007 v1.1 amendment | FR-053, FR-054, FR-055, FR-058 |
| OQ-024 | Floating and re-dockable panels (needs a docking library and an ADR) | Low | Post-MVP | Open | UI-001 |
| OQ-025 | Are Cascadia Mono and Segoe Fluent Icons present on Windows 10 Enterprise LTSC 21H2? | Low | First shell task | Open (fallbacks: Consolas, Segoe MDL2 Assets) | NFR-025 |
| OQ-026 | After a failed build, should Run be enabled? AC-013 (approved) says "Run is not enabled"; the ui-spec §4 state matrix (Build failed column) lists "Build, Run (rebuilds)". | Low | Next design or requirements review | Open. TASK-080 follows AC-013: Run is disabled after a failed build until the text changes, then Run rebuilds. Proposed: align the ui-spec matrix with AC-013 | AC-013, UI-002 |
| OQ-027 | NFR-005 asks for a cold start ≤ 3 s; measured after a Windows restart: 7.75 s, 4.3 s of it before the application's code runs (the self-contained .NET runtime and WPF read from disk; warm starts 1.9 s). Change the deployment (framework-dependent needs the .NET Desktop Runtime installed, against ADR-0009), revise NFR-005 (e.g. ≤ 3 s warm, a separate limit for the first start after a restart), or accept a known limitation? | High | Before release (QA) | Open. Evidence: TASK-108, DEF-023, `docs/06-implementation/task-108-verification.md` | NFR-005, AC-056, ADR-0009 |

## Assumptions to validate
| ID | Assumption | Risk if false | Validation |
|---|---|---|---|
| ASM-001 | Users run Windows 11 24H2+ or Windows 10 Enterprise LTSC 21H2 on x64 hardware (D-011). | Windows 10 22H2 Home/Pro users are unsupported and need to upgrade. ARM64-only users need the optional Arm64 build. | D-011 (decided). Monitor user feedback |
| ASM-002 | Programs are small educational programs: one source file, .COM image <= 65,280 bytes. | Larger multi-file programs are out of MVP scope (FR-010, FR-043). | Review with sample set during research |
| ASM-003 | A redistributable assembler that accepts NASM syntax and emits 8086 flat binaries exists (NASM is believed to be BSD-2-Clause, unverified). | If bundling is not allowed, users must install the assembler themselves (FR-023). | OQ-002 |
| ASM-004 | An 8086 execution engine with full state access exists, or a custom engine is feasible within the MVP. | MVP debugging scope or schedule changes. | OQ-001, OQ-008 |
| ASM-005 | One program executing at a time is sufficient. | Multi-session debugging needs redesign. | User review |
| ASM-006 | Users accept NASM syntax (D-006). | Students using MASM/TASM material need a migration path. | OQ-018 |

## Changelog
- 2026-10-26 v0.1: Initial draft (SDLC-003, Claude Code).
- 2026-10-26 v1.0: Approved by the user (2026-10-26 17:11 +0500: "approve requirements v0.1 and D-010"). Requirement statuses PROPOSED → APPROVED. NEEDS_RESEARCH unchanged. Content otherwise identical to v0.1.
- 2026-10-26 v1.1: Minor (SDLC-004 research). Status column updated for 13 questions, OQ-020 to OQ-022 added. No requirement changed. Document stays Approved.
- 2026-10-26 v2.0: **Substantive change (SDLC-005)** applying user-approved decisions D-011 (supported Windows: CON-001, NFR-025, AC-040, ASM-001, OQ-010) and D-012 (INT 21h AH=00h: FR-039, FR-041, new AC-064, OQ-022). Changed requirements are `PROPOSED` until re-approval. Set status `Awaiting approval`.
- 2026-10-26 v2.0: **Approved** by the user (2026-10-26 18:18 +0500: "approve requirements v2.0"). FR-039, FR-041, NFR-025 → APPROVED.
- 2026-10-26 v2.1: Minor (POC-001 results). Status updated for OQ-001, OQ-008, OQ-014. No requirement changed. Stays Approved.
- 2026-10-26 v2.2: Minor (POC-002 results). OQ-009 answered. No requirement changed. Stays Approved.
- 2026-10-26 v2.3: Minor. OQ-001, 005, 011, 013, 014, 015, 016 resolved by approved ADRs / D-014. OQ-022 updated. No requirement changed.
- 2026-10-26 v2.4: Minor (SDLC-007). OQ-021 resolution proposed. OQ-023 to OQ-025 added. No requirement changed.
- 2026-10-26 v2.5: Minor. OQ-021 and OQ-023 resolved (design approval). No requirement changed.
- 2026-11-01: OQ-026 added (TASK-080 found AC-013 and the ui-spec §4 matrix disagree on Run after a failed build). No answer recorded.
- 2026-11-03: OQ-027 added (TASK-108: the cold start after a restart took 7.75 s; NFR-005's 3 s target). No answer recorded.
