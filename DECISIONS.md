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
| D-002 | Procedures and guides live in a tool-independent core (`docs/sdlc/`). Tool folders contain only thin wrappers | Proposed (implemented 2026-10-26) | Satisfies the user's SDLC-002 requirement for a tool-independent core. The specific layout awaits user review |
| D-003 | Root shared memory files are the single source of state. `PROJECT_STATE.md` replaces `docs/status/project-status.md`. `WORK_LOG.md` replaces the per-task `docs/06-implementation/task-log.md` | Proposed (implemented 2026-10-26) | Avoids duplicate status and log files ("reuse equivalents instead of duplicates", SDLC-002 brief) |
| D-004 | Initialize Git with a `.gitignore` and one initial commit of the SDLC foundation | **Approved** | User answer 2026-10-26: "Init + initial commit (Recommended)" |
| D-005 | Antigravity integration uses the `.agent/` root: `rules/` (both), `skills/` (CLI 1.4.4), `workflows/` (IDE 1.11.17, which predates skills). `AGENTS.md` holds shared rules | Proposed (implemented 2026-10-26) | CLI conventions verified from the built-in `agy-customizations` docs and a loading test. IDE workflow loading is not verified |

## Architecture decisions (ADRs)
None yet. They are created by `/assembly-architecture` after requirements and research are approved.

## Pending technology preferences (not decisions)
C# · WPF/XAML · MVVM · .NET 10 LTS · AvalonEdit (to evaluate) · xUnit (to evaluate) · NASM (to
evaluate. NASM 3.02 happens to be installed locally, which is not a decision) · execution engine (to
research) · Inno Setup (to evaluate). Source: `docs/00-project-overview/project-overview.md`.
Each becomes a decision only through an approved ADR.
