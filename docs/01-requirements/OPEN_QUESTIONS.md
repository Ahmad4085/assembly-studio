# Open Questions and Assumptions

| Field | Value |
|---|---|
| Document | Open questions and assumptions |
| Status | Approved |
| Version | 1.1 |
| Last updated | 2026-10-26 |
| Depends on | `docs/00-project-overview/project-overview.md` v0.3. `DECISIONS.md` D-006 to D-009 |
| Requirements set | Approved as one set together with `PRODUCT_REQUIREMENTS.md` |

These questions must not be answered by guessing. Each names the phase that resolves it. Questions
marked High must be answered (or explicitly accepted as open) before the architecture is approved.
OQ-001 to OQ-007 were carried over from `docs/00-project-overview/project-overview.md`.

## Open questions
| ID | Question | Impact | Resolved in | Status | Related |
|---|---|---|---|---|---|
| OQ-001 | Which 8086 execution engine fits the requirements: an existing emulator library, a custom C# 8086 core, or a hybrid? | High | Research → ADR | Researched 2026-10-26: custom C# core recommended (R-ENG, `docs/02-research/execution-engine.md`). ADR pending. POC-001 proposed | FR-033, FR-036 |
| OQ-002 | NASM integration: is redistribution of NASM binaries permitted and what are the obligations (believed BSD-2-Clause, unverified)? Minimum version? Does `CPU 8086` reject all later instructions? What listing/map output is available? | High | Research | Answered Researched 2026-10-26: BSD-2-Clause, redistribution allowed with notice. Minimum 3.02. `cpu 8086` works but user source can override it (R-ASM) | FR-023, FR-024, FR-027, FR-028 |
| OQ-003 | Exact DOS service list and semantics for the MVP text I/O class (D-008): echo behavior, Ctrl-C/Ctrl-Break handling, buffered-input editing, initial PSP contents. | Medium | Research | Answered Researched 2026-10-26: DOS 3.3/4.0 semantics with documented deviations (`dos-services.md`) | FR-034, FR-039, FR-064 |
| OQ-004 | Executable formats: confirm .COM-only for MVP. Can NASM produce MZ .EXE without a separate linker, and at what cost? | Medium | Research | Answered Researched 2026-10-26: .COM confirmed for MVP. MZ via `exebin.mac` without a linker (P2) | FR-026, FR-034, FR-043 |
| OQ-005 | Target .NET version (10 LTS preferred) and WPF support on the supported Windows versions. SDK availability (BLK-001). | Medium | Research → ADR | Researched 2026-10-26: .NET 10 LTS recommended (support to 2028-11-14). **Windows 10 conflict → decision D1** (`research-summary.md`) | FR-086, NFR-025 |
| OQ-006 | Initialize a Git repository? | Low | User | Resolved 2026-10-26 (D-004) | — |
| OQ-007 | Product distribution model and license. | High | User | Partially resolved: permissive open source (D-007). Specific license (e.g. MIT or Apache-2.0) open, needed before first release | NFR-030 |
| OQ-008 | Can the selected engine expose all registers, flags, memory and per-instruction state, support breakpoints and execution control, and snapshot/restore state? Is it deterministic? | High | Research / POC | Researched 2026-10-26: designed into a custom core (R-ENG). Feasibility via POC-001 | FR-046 to FR-062, NFR-016 |
| OQ-009 | How are addresses mapped to source lines (NASM listing or map files), including macros and `times`? How reliable is it? | Medium | Research / POC | Partially answered Researched 2026-10-26: listing + map mapping measured. Macros/`times`/`%include` → POC-002 | FR-028, FR-052, FR-053 |
| OQ-010 | Minimum supported Windows 10/11 builds for the chosen .NET/WPF. x64 only, or also ARM64? | Medium | Research | Answered Researched 2026-10-26: .NET 10 supports Win11 24H2+ and Win10 Enterprise LTSC only. x64 + Arm64. **Decision D1 pending** | NFR-025 |
| OQ-011 | Safest way to execute untrusted programs: is in-process emulation enough, or is a separate process needed for fault isolation? | Medium | Architecture | Open | NFR-010, NFR-013, FR-096 |
| OQ-012 | Which P0/P1 requirements fit the first release? Which P1 items may be deferred? | Medium | Plan (with user) | Open | All P1 |
| OQ-013 | Deployment model: self-contained vs framework-dependent .NET. Installer technology (e.g. Inno Setup). Per-user vs per-machine install, and admin rights. | Medium | Research → ADR | Researched 2026-10-26: self-contained, folder layout, Inno Setup per-user (R-PLAT, R-PKG). Size/time via POC-004 | FR-083, FR-086, NFR-027 |
| OQ-014 | Reference machine specification for performance targets. | Medium | Research | Open | NFR-001 to NFR-007 |
| OQ-015 | Undefined flags: emulate a specific 8086's actual results, or mark them undefined in the UI? | Medium | Research → ADR | Researched 2026-10-26: SingleStepTests `flags-mask` gives per-opcode undefined flags. Policy → ADR | FR-048, NFR-017 |
| OQ-016 | Exact CPU semantics target (8086 vs 8088, behaviors that changed in later CPUs such as shift-count masking and `PUSH SP`) and the source of expected values (documentation, reference test suites and their licenses). | Medium | Research | Answered Researched 2026-10-26: SingleStepTests 8086 (Intel P80C86A-2, MIT) + 8088 as oracle | FR-027, FR-036, NFR-017 |
| OQ-017 | Feasibility and cost of reverse stepping (snapshots or recording). | Low | Research (Post-MVP) | Researched 2026-10-26: snapshot support to be designed into the core (P2) | FR-060 |
| OQ-018 | Is a NASM-syntax hint enough for students whose courses use MASM/TASM syntax, or is a MASM-compatible mode needed later? | Low | Post-MVP review | Open | FR-021, D-006 |
| OQ-019 | Development tooling: .NET SDK not installed (BLK-001). Which IDE (Rider is present) and CI setup? | Low | Before phase 6 | Open: .NET SDK still not installed. Now also blocks POCs (`research-summary.md` D3) | — |
| OQ-020 | Code signing for the installer and executables (Windows SmartScreen warnings for an unsigned open-source app); free options? | Medium | Architecture / release | Open (raised by research 2026-10-26) | FR-083, NFR-027 |
| OQ-021 | Keyboard conflict: Ctrl+C is 'copy' in the IDE but Ctrl-C/Ctrl-Break in a DOS program. How are they routed (console focus only)? | Low | Design | Open (raised by research 2026-10-26) | FR-064, FR-091 |
| OQ-022 | Add DOS functions beyond D-008: AH=00h (DOS-1 terminate), 06h (direct console I/O), 0Bh (input status), 30h (version)? | Medium | Architecture (needs a requirements change) | Open (raised by research 2026-10-26) | FR-039, D-008 |

## Assumptions to validate
| ID | Assumption | Risk if false | Validation |
|---|---|---|---|
| ASM-001 | Users run Windows 10 or 11 on x64 hardware. | ARM64-only users can't use the MVP. | OQ-010 |
| ASM-002 | Programs are small educational programs: one source file, .COM image <= 65,280 bytes. | Larger multi-file programs are out of MVP scope (FR-010, FR-043). | Review with sample set during research |
| ASM-003 | A redistributable assembler that accepts NASM syntax and emits 8086 flat binaries exists (NASM is believed to be BSD-2-Clause, unverified). | If bundling is not allowed, users must install the assembler themselves (FR-023). | OQ-002 |
| ASM-004 | An 8086 execution engine with full state access exists, or a custom engine is feasible within the MVP. | MVP debugging scope or schedule changes. | OQ-001, OQ-008 |
| ASM-005 | One program executing at a time is sufficient. | Multi-session debugging needs redesign. | User review |
| ASM-006 | Users accept NASM syntax (D-006). | Students using MASM/TASM material need a migration path. | OQ-018 |

## Changelog
- 2026-10-26 v0.1: Initial draft (SDLC-003, Claude Code).
- 2026-10-26 v1.0: Approved by the user (2026-10-26 17:11 +0500: "approve requirements v0.1 and D-010"). Requirement statuses PROPOSED → APPROVED. NEEDS_RESEARCH unchanged. Content otherwise identical to v0.1.
- 2026-10-26 v1.1: Minor (SDLC-004 research). Status column updated for 13 questions, OQ-020 to OQ-022 added. No requirement changed. Document stays Approved.
