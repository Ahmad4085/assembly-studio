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

## Architecture decisions (ADRs)
None yet. They are created by `/assembly-architecture` after requirements and research are approved.

## Pending technology preferences (not decisions)
C# · WPF/XAML · MVVM · .NET 10 LTS · AvalonEdit (to evaluate) · xUnit (to evaluate) · NASM (to
evaluate. NASM 3.02 happens to be installed locally, which is not a decision) · execution engine (to
research) · Inno Setup (to evaluate). Source: `docs/00-project-overview/project-overview.md`.
Each becomes a decision only through an approved ADR.
