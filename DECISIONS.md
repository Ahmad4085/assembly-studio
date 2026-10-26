# Decisions Register

> Index of all decisions. Architecture decisions are written in full as ADRs in `docs/decisions/`
> (template `docs/templates/adr.md`) and listed here. Process and tooling decisions are recorded
> here directly. Statuses: **Approved** (explicit user approval, cited) · **Proposed** (not yet
> approved, may already be implemented) · **Superseded** · **Rejected**.
> Never act on a *Proposed* decision as if it were approved when it matters, and never override an
> *Approved* one. Propose a change instead.

## Process and tooling decisions
| ID | Decision | Status | Evidence / approval |
|---|---|---|---|
| D-001 | Claude Code SDLC commands are implemented as project skills (`.claude/skills/<name>/SKILL.md`). Phase commands use `disable-model-invocation: true` | **Approved** | User instruction 2026-10-26 (session 2): "Create … all 11 `/assembly-*` commands as user-invocable skills" |
| D-002 | Procedures and guides live in a tool-independent core (`docs/sdlc/`). Tool folders contain only thin wrappers | **Approved** | User approval 2026-10-26 16:39 +0500: "approve D-002, D-003, D-005". Implemented 2026-10-26 |
| D-003 | Root shared memory files are the single source of state. `PROJECT_STATE.md` replaces `docs/status/project-status.md`. `WORK_LOG.md` replaces the per-task `docs/06-implementation/task-log.md` | **Approved** | User approval 2026-10-26 16:39 +0500: "approve D-002, D-003, D-005". Implemented 2026-10-26 |
| D-004 | Initialize Git with a `.gitignore` and one initial commit of the SDLC foundation | **Approved** | User answer 2026-10-26: "Init + initial commit (Recommended)" |
| D-005 | Antigravity integration uses the `.agent/` root: `rules/` (both), `skills/` (CLI 1.4.4), `workflows/` (IDE 1.11.17, which predates skills). `AGENTS.md` holds shared rules | **Approved** | User approval 2026-10-26 16:39 +0500: "approve D-002, D-003, D-005". CLI conventions verified (built-in `agy-customizations` docs and a loading test). IDE workflow loading is still not verified |
| D-006 | MVP source syntax is NASM. MASM/TASM-style syntax is not supported in the MVP (a hint explains this) | **Approved** | User answer 2026-10-26 between 16:50 and 17:06 +0500 (exact time not recorded) to elicitation questions (SDLC-003): "NASM syntax only" |
| D-007 | Distribution model: permissive open-source release. The specific license (e.g. MIT, Apache-2.0) is still open (OQ-007) | **Approved** | User answer 2026-10-26 between 16:50 and 17:06 +0500 (exact time not recorded) to elicitation questions (SDLC-003): "Open source (permissive)" |
| D-008 | MVP program I/O: text output and keyboard input through a documented DOS service subset. BIOS video/keyboard services are Post-MVP | **Approved** | User answer 2026-10-26 between 16:50 and 17:06 +0500 (exact time not recorded) to elicitation questions (SDLC-003): "Text output + keyboard input" |
| D-009 | MVP UI and tutorials are English only, with all user-facing strings localizable | **Approved** | User answer 2026-10-26 between 16:50 and 17:06 +0500 (exact time not recorded) to elicitation questions (SDLC-003): "English only, localizable" |
| D-010 | The requirements phase outputs a 7-document set in `docs/01-requirements/` (names as requested in the SDLC-003 brief), using the existing folder convention instead of a new `docs/requirements/`. `REQUIREMENTS_TRACEABILITY.md` is the project's single traceability matrix, extended by later phases (replaces the planned `docs/05-planning/traceability.md`). Priorities are P0–P3 with statuses PROPOSED/APPROVED/BLOCKED/NEEDS_RESEARCH (replaces MoSCoW) | **Approved** | User approval 2026-10-26 17:11 +0500: "approve requirements v0.1 and D-010". Implemented 2026-10-26 |
| D-011 | Supported Windows (research decision D1 = A): **Windows 11 24H2+** and **Windows 10 Enterprise LTSC 21H2** are supported and tested. **Windows 10 22H2 Home/Pro is not supported** (may work, untested), because .NET 10 doesn't support it upstream. Changes CON-001 and NFR-025 | **Approved** | User 2026-10-26 17:59 +0500: "approve research v0.1; D1=A; D2=add 00h; D3=approve POC-001,002 and install the SDK". Requirement text updated by SDLC-005 (`/assembly-requirements`) |
| D-012 | Add **INT 21h AH=00h** (DOS-1 terminate, same semantics as INT 20h) to the MVP DOS subset. Changes FR-039 | **Approved** | User 2026-10-26 17:59 +0500: "approve research v0.1; D1=A; D2=add 00h; D3=approve POC-001,002 and install the SDK". Requirement text updated by SDLC-005 |
| D-013 | **POC-001** (custom C# core feasibility) and **POC-002** (listing + map source mapping) approved. **.NET 10 SDK installation authorized** (installed 18:04 +0500: SDK 10.0.403) | **Approved** | User 2026-10-26 17:59 +0500: "approve research v0.1; D1=A; D2=add 00h; D3=approve POC-001,002 and install the SDK" |
| D-014 | **Reference machine** for performance acceptance (OQ-014): Intel Core i5-8265U (4C/8T), 16 GB RAM, Windows 11 (the dev machine) as the minimum reference | **Approved** | User 2026-10-26 18:54 +0500: "approve architecture v0.1 and ADR-0001..0009; reference machine OK" |
| D-015 | **Ctrl+C routing** (OQ-021): Ctrl+C copies, except in the focused console of an active program, where it is delivered as DOS Ctrl-C. Ctrl+Break delivers Ctrl-C from anywhere. Console copy = Ctrl+Shift+C. Pause = Ctrl+Alt+Break | **Approved** | User 2026-10-26 19:03 +0500: "approve" (reply to the gate question naming ui-spec v0.1, design tokens v0.1, the ADR-0007 v1.1 amendment and the Ctrl+C proposal). `docs/04-design/ui-spec.md` §6 |

## Architecture decisions (ADRs)
| ADR | Decision | Status | Source |
|---|---|---|---|
| [ADR-0001](docs/decisions/ADR-0001-platform-dotnet10-wpf.md) | Platform: .NET 10 LTS + WPF on Windows x64 | **Approved** 2026-10-26 18:54 +0500 | SDLC-006, 2026-10-26 |
| [ADR-0002](docs/decisions/ADR-0002-solution-structure.md) | Solution structure and dependency rules | **Approved** 2026-10-26 18:54 +0500 | SDLC-006, 2026-10-26 |
| [ADR-0003](docs/decisions/ADR-0003-execution-engine.md) | Execution engine: custom managed 8086 core | **Approved** 2026-10-26 18:54 +0500 | SDLC-006, 2026-10-26 |
| [ADR-0004](docs/decisions/ADR-0004-runtime-and-dos-services.md) | Runtime environment and DOS services: in-process, own implementation | **Approved** 2026-10-26 18:54 +0500 | SDLC-006, 2026-10-26 |
| [ADR-0005](docs/decisions/ADR-0005-assembler-integration-nasm.md) | Assembler integration: bundled NASM 3.02, listing + map source mapping | **Approved** 2026-10-26 18:54 +0500 | SDLC-006, 2026-10-26 |
| [ADR-0006](docs/decisions/ADR-0006-editor-avalonedit.md) | Code editor: AvalonEdit | **Approved** 2026-10-26 18:54 +0500 | SDLC-006, 2026-10-26 |
| [ADR-0007](docs/decisions/ADR-0007-ui-architecture-mvvm-threading.md) | UI architecture: MVVM, threading, snapshots, settings (v1.1: snapshot content amendment approved 2026-10-26) | **Approved** 2026-10-26 18:54 +0500 | SDLC-006, 2026-10-26 |
| [ADR-0008](docs/decisions/ADR-0008-testing-strategy-and-tools.md) | Test framework, tools and oracle data | **Approved** 2026-10-26 18:54 +0500 | SDLC-006, 2026-10-26 |
| [ADR-0009](docs/decisions/ADR-0009-packaging-and-distribution.md) | Packaging: self-contained + Inno Setup per-user (code signing Deferred) | **Approved** 2026-10-26 18:54 +0500 | SDLC-006, 2026-10-26 |

## Pending technology preferences (not decisions)
C# · WPF/XAML · MVVM · .NET 10 LTS · AvalonEdit (to evaluate) · xUnit (to evaluate) · NASM (to
evaluate. NASM 3.02 happens to be installed locally, which is not a decision) · execution engine (to
research) · Inno Setup (to evaluate). Source: `docs/00-project-overview/project-overview.md`.
Each becomes a decision only through an approved ADR.
