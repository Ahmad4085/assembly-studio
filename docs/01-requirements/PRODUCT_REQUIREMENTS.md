# Assembly Studio — Product Requirements

| Field | Value |
|---|---|
| Document | Product requirements (entry document of the requirements set) |
| Status | Approved |
| Version | 2.0 |
| Last updated | 2026-10-26 |
| Depends on | `docs/00-project-overview/project-overview.md` v0.3. `DECISIONS.md` D-006 to D-009 |
| Requirements set | Approved as one set together with `PRODUCT_REQUIREMENTS.md` |

## 1. Purpose and document set
This set defines **what** Assembly Studio must do and how well, for the first release line. It
doesn't decide **how**: technology choices are made as ADRs in the architecture phase. The set is
reviewed and approved as one unit. This document carries the gate.

| Document | Contents |
|---|---|
| `PRODUCT_REQUIREMENTS.md` | Goals, personas, scope (MVP / Post-MVP / out of scope), constraints, decisions, definitions, summary |
| `FUNCTIONAL_REQUIREMENTS.md` | `FR-###` by area A–J |
| `NON_FUNCTIONAL_REQUIREMENTS.md` | `NFR-###` with measurable targets and verification |
| `USER_STORIES.md` | `US-###` per persona |
| `ACCEPTANCE_CRITERIA.md` | `AC-###` Given/When/Then, split into engine-independent and engine-dependent |
| `REQUIREMENTS_TRACEABILITY.md` | Goal → story → requirement → acceptance criterion → test case. The project's single traceability matrix, extended by later phases |
| `OPEN_QUESTIONS.md` | `OQ-###` technical unknowns and research questions, plus `ASM-###` assumptions to validate |

## 2. Definitions
**Priority**
- `P0`: required for the first usable release (MVP). Release is blocked if a P0 requirement is not met.
- `P1`: planned for the MVP. Can be deferred only with explicit user approval, recorded in `PROJECT_STATE.md`.
- `P2`: Post-MVP, after feasibility is established.
- `P3`: out of scope until separately approved.

**Requirement status**
- `PROPOSED`: drafted, awaiting user approval.
- `APPROVED`: approved at the requirements gate.
- `NEEDS_RESEARCH`: cannot be fixed until a listed open question is answered. It may still be approved as a *goal*, with its details completed after research.
- `BLOCKED`: cannot proceed because of an unresolved external dependency.

**Target basis (NFRs)**
- `FIRM`: derived from the vision, a constraint or a standard.
- `PROPOSED`: a reasoned starting value that is not yet validated. Each states how it will be validated, and changing it needs approval.

**Evidence:** no test exists yet. Every test case is `NOT CREATED` and every result is `NOT RUN`.

## 3. Product goals
| ID | Goal | Description |
|---|---|---|
| PG-1 | Understanding | Make the execution of 8086 programs observable instruction by instruction: registers, flags, memory and stack. |
| PG-2 | Authoring | Let users write and assemble NASM-syntax 16-bit programs offline, with diagnostics that point to the cause. |
| PG-3 | Controlled execution | Run and debug supported programs inside an emulated environment, under user control. |
| PG-4 | Trust and robustness | Show only real execution state, state limits explicitly, survive faulty programs and tools, and stay accessible. |
| PG-5 | Offline, private, installable | Work without network or account, keep user data local, and install and uninstall cleanly on Windows. |
| PG-6 | Learning content | Provide built-in samples, tutorials and references for beginners, students and instructors. |

## 4. Personas
| ID | Persona | Description | Needs |
|---|---|---|---|
| PER-1 | Beginner | Self-learner with little or no assembly experience; learns alone, often offline. | Understand what instructions do to the CPU; avoid getting stuck on tooling. |
| PER-2 | Student | Completes assembly exercises for a course; writes small programs with input and output. | Write, assemble, run and check exercises reliably; inspect memory and stack. Risk: course material may use MASM/TASM syntax (see D-006, OQ-018). |
| PER-3 | Developer | Experienced programmer writing and debugging supported 16-bit programs. | Efficient editing, breakpoints, step over, execution history, control over the toolchain. |
| PER-4 | Instructor | Demonstrates instruction execution in class, often on a projector. | Readable, repeatable, trustworthy demonstrations without internet. |

## 5. Decisions that shape these requirements (user-approved, `DECISIONS.md`)
| ID | Decision |
|---|---|
| D-006 | MVP source syntax is **NASM**. MASM/TASM-style syntax is not supported (a hint explains this, FR-021) |
| D-007 | Distribution model is **permissive open source**. The specific license is open (OQ-007) |
| D-008 | MVP program I/O is **text output plus keyboard input** through a documented DOS service subset. BIOS video and keyboard services are Post-MVP |
| D-009 | MVP UI and content are **English only, with all strings localizable** |
| D-011 | Supported Windows: **Windows 11 24H2+ and Windows 10 Enterprise LTSC 21H2**. Windows 10 22H2 Home/Pro is not supported (.NET 10 upstream support, research) |
| D-012 | The MVP DOS subset **adds INT 21h AH=00h** (DOS-1 terminate) |

## 6. Constraints
| ID | Constraint | Source |
|---|---|---|
| CON-001 | Runs on Windows 11 24H2 or later and Windows 10 Enterprise LTSC 21H2 (x64). Windows 10 22H2 Home/Pro is not supported. | Product vision, D-011 |
| CON-002 | Core functionality works offline with no account or cloud service. | Product vision, AGENTS.md |
| CON-003 | No paid dependencies and no mandatory online services. | AGENTS.md |
| CON-004 | Released as permissive open source; every distributed dependency must be license-compatible. | D-007 |
| CON-005 | Source syntax is NASM's. | D-006 |
| CON-006 | UI and content in English for the MVP, with all strings localizable. | D-009 |
| CON-007 | MVP program I/O: text output and keyboard input through a documented DOS service subset. | D-008 |

**Technology preferences (not constraints):** C#, WPF/XAML, MVVM, .NET 10 LTS, an editor component
(AvalonEdit to evaluate), xUnit, NASM, Inno Setup. Each becomes binding only through an approved
ADR, after verifying compatibility, licensing and tooling (no .NET SDK is installed yet, BLK-001).

## 7. Verified facts, decisions, proposals and unknowns
| Kind | Items |
|---|---|
| **Verified (environment, observed 2026-10-26)** | Windows 11 Pro 10.0.26200 [Measured: `ver`]. NASM 3.02 installed locally [Measured: `nasm -v`]. No .NET SDK installed, runtime 8.0.8 only [Measured: `dotnet --list-sdks`]. No third-party capability, license or performance claim has been verified yet |
| **Decided by the user** | D-006 syntax, D-007 distribution model, D-008 MVP I/O class, D-009 language |
| **Approved (this set, v1.0)** | All FR/NFR, every numeric threshold marked PROPOSED, .COM-only MVP format, the DOS function list in FR-039, initial register values in FR-034 |
| **Unresolved** | `OPEN_QUESTIONS.md`: engine (OQ-001, OQ-008), assembler bundling and CPU restriction (OQ-002), source mapping (OQ-009), isolation (OQ-011), deployment (OQ-013) and others |

## 8. Supported-scope statement (MVP, proposed)
- **CPU:** 8086 real mode, documented instruction set without 8087 (final list FR-036 after research). 1 MiB address space. No 80186+, protected mode, 32-bit or 64-bit.
- **Assembler and syntax:** NASM syntax, 16-bit flat binary output (`-f bin`, `org 100h`), single source file.
- **Executable format:** .COM only (≤ 65,280 bytes). MZ .EXE is Post-MVP (FR-043).
- **Platform:** Windows 11 24H2+ and Windows 10 Enterprise LTSC 21H2, x64 (D-011). Windows 10 22H2 Home/Pro is not supported.
- **Operating-system services:** text output, keyboard input and termination (INT 21h 00h/01h/02h/07h/08h/09h/0Ah/4Ch, INT 20h; D-008, D-012), DOS 3.3/4.0 semantics with documented deviations. Anything else halts with an explicit message (FR-040).
- **Execution:** always emulated, never native. Deterministic, budgeted and pausable.
- **Not supported in the MVP:** DOS file, device and memory services, TSRs, BIOS video/keyboard (INT 10h/16h), graphics, sound, mouse, MASM/TASM syntax, arbitrary existing DOS programs.

Assembling (NASM), executing (CPU engine), loading (loader/runtime) and OS services (DOS subset) are
separate requirements and components. No requirement assumes one of them provides another.

## 9. Release scope (proposed)
### MVP: required for the first usable release (P0)
A minimal, reliable offline workflow: write, assemble and run NASM-syntax .COM programs within the supported scope, with diagnostics that point to the cause, keyboard I/O, stepping, and register, flag, memory and stack inspection.

FR-001, FR-002, FR-003, FR-004, FR-005, FR-008, FR-011, FR-012, FR-013, FR-015, FR-018, FR-023, FR-024, FR-025, FR-026, FR-027, FR-028, FR-029, FR-030, FR-031, FR-033, FR-034, FR-035, FR-036, FR-037, FR-038, FR-039, FR-040, FR-041, FR-042, FR-045, FR-046, FR-048, FR-049, FR-051, FR-053, FR-054, FR-055, FR-056, FR-063, FR-064, FR-065, FR-067, FR-068, FR-072, FR-075, FR-076, FR-079, FR-080, FR-081, FR-083, FR-085, FR-086, FR-087, FR-090, FR-091, FR-092, FR-095, FR-096, NFR-002, NFR-004, NFR-008, NFR-009, NFR-010, NFR-013, NFR-014, NFR-015, NFR-016, NFR-017, NFR-018, NFR-019, NFR-020, NFR-022, NFR-025, NFR-027, NFR-029, NFR-030

### MVP: planned, deferrable with approval (P1)
Breakpoints, step over, execution history, tutorials and reference, replace, tabs, preferences, accessibility depth, crash recovery, performance targets.

FR-006, FR-007, FR-014, FR-016, FR-017, FR-019, FR-020, FR-021, FR-022, FR-032, FR-047, FR-050, FR-052, FR-057, FR-058, FR-066, FR-069, FR-070, FR-071, FR-077, FR-078, FR-082, FR-084, FR-088, FR-093, FR-094, NFR-001, NFR-003, NFR-005, NFR-006, NFR-007, NFR-011, NFR-012, NFR-021, NFR-023, NFR-024, NFR-026, NFR-031, NFR-032, NFR-033

### Post-MVP (P2)
Reverse stepping, run to cursor, editing state, MZ .EXE, BIOS services, multi-file programs, checked exercises, presentation mode, external-change detection, footprint target.

FR-009, FR-010, FR-043, FR-044, FR-059, FR-060, FR-061, FR-062, FR-073, FR-074, NFR-028

### Out of scope until approved (P3 and explicit exclusions)
FR-089

- A replacement for professional reverse-engineering or debugging suites
- A complete x86/x64 instruction set, 80186+ instructions, protected/32-bit/64-bit modes, the 8087 FPU
- Guaranteed compatibility with arbitrary DOS applications
- DOS file, device and memory-management services, TSRs, graphics, sound, mouse
- MASM/TASM syntax (D-006)
- Cloud accounts, mandatory online services, telemetry
- Mobile and web versions
- Automatic updates (FR-089)
- Debugging native host processes
- Any feature without a validated technical implementation plan

## 10. Trade-offs for review
| Trade-off | Proposed choice | Consequence |
|---|---|---|
| Course compatibility vs scope | NASM only (D-006) | Students with MASM/TASM material get a hint, not compatibility (OQ-018) |
| Executable formats | .COM only in the MVP | Simple loader and no linker needed. Larger programs are excluded (ASM-002) |
| OS services | Text I/O DOS subset (D-008) | Covers typical exercises. Screen-oriented programs wait for P2 BIOS services |
| Breakpoints and step over | P1, not P0 | The MVP is usable with step and run alone. Both are cheap if the engine single-steps, so expected in the MVP |
| Reverse stepping | P2, needs research | No promise until state restoration is proven (OQ-017) |
| Bundled assembler | Bundle if license permits | Zero-configuration first run (FR-079). Otherwise users install NASM themselves |
| Performance numbers | PROPOSED until measured | Not release criteria until validated or approved as-is (AC-056) |

## 11. Summary counts
| Set | P0 | P1 | P2 | P3 | Total |
|---|---|---|---|---|---|
| Functional | 59 | 26 | 10 | 1 | 96 |
| Non-functional | 18 | 14 | 1 | 0 | 33 |

| Set | PROPOSED | NEEDS_RESEARCH | BLOCKED | APPROVED |
|---|---|---|---|---|
| Functional | 0 | 10 | 0 | 86 |
| Non-functional | 0 | 1 | 0 | 32 |

User stories: 26. Acceptance criteria: 64 (35 engine-independent, 29 engine-dependent). Open questions: 22 (16 open, partially answered or researched pending an ADR). Assumptions: 6.

## 12. Approval record
Approved by the user on 2026-10-26 17:11 +0500 ("approve requirements v0.1 and D-010"), logged in `PROJECT_STATE.md`.
- Requirements with status `PROPOSED` are now `APPROVED`. `NEEDS_RESEARCH` items are approved as goals, and their
  details are completed after research closes the linked open questions.
- Numeric targets whose basis is `PROPOSED` remain starting values. They become release criteria once validated or
  explicitly approved as-is (AC-056).
- Further changes follow the process contract §4: minor edits update the changelog, and substantive changes need a
  new version and re-approval.

**v2.0 (SDLC-005): approved 2026-10-26 18:18 +0500** ("approve requirements v2.0"). Applies D-011 and D-012. FR-039, FR-041 and NFR-025 are APPROVED.

## Changelog
- 2026-10-26 v0.1: Initial draft (SDLC-003, Claude Code).
- 2026-10-26 v1.0: Approved by the user (2026-10-26 17:11 +0500: "approve requirements v0.1 and D-010"). Requirement statuses PROPOSED → APPROVED. NEEDS_RESEARCH unchanged. Content otherwise identical to v0.1.
- 2026-10-26 v2.0: **Substantive change (SDLC-005)** applying user-approved decisions D-011 (supported Windows: CON-001, NFR-025, AC-040, ASM-001, OQ-010) and D-012 (INT 21h AH=00h: FR-039, FR-041, new AC-064, OQ-022). Changed requirements are `PROPOSED` until re-approval. Set status `Awaiting approval`.
- 2026-10-26 v2.0: **Approved** by the user (2026-10-26 18:18 +0500: "approve requirements v2.0"). FR-039, FR-041, NFR-025 → APPROVED.
