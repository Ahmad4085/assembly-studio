# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-26 by Claude Code (Opus 5.5), end of SDLC-002 |
| Current task | **SDLC-003 Requirements phase: `Ready`, not started, no owner** |
| Last completed | SDLC-002 Multi-agent support (Done) |
| Git state | At writing: repository initialized, no commits yet. **Planned:** this file and all others go into the single initial commit `SDLC-002: Multi-agent SDLC foundation`. Verify with `git log --oneline -1` (expect that message) and `git status --short` (expect empty). Anything else is someone else's change. `.mcp.json` is git-ignored |

## Last verified working state
- SDLC foundation and multi-agent setup in place and structurally validated (details in
  `CURRENT_TASK.md` → "Last completed").
- No application code, no requirements yet. No builds or tests exist, so none have run.

## Changes since the previous handoff
First handoff. SDLC-002 changes:
- Procedures and guides moved to the tool-independent core `docs/sdlc/`.
- `AGENTS.md` holds the shared rules, and `CLAUDE.md` imports it.
- Antigravity entry points added under `.agent/`.
- Root shared memory files created. `PROJECT_STATE.md` replaced `docs/status/project-status.md` (deleted).
- Git initialized.
Full list: `WORK_LOG.md`.

## Failed tests and known issues
- No failed tests (none exist).
- **BLK-001:** no .NET SDK installed. Blocks phase 6+ only.
- Antigravity IDE 1.11.17 loading of `.agent/workflows/` is not verified. In the IDE, check that typing `/`
  shows `assembly-*` workflows. If not, open the procedure file manually (see below).
- Antigravity can't technically block the model from starting a phase skill on its own. The gate is
  enforced by `AGENTS.md` and `.agent/rules/assembly-studio.md`.

## Decisions and constraints to respect
- Approved: D-001 (Claude skills format), D-004 (Git). Proposed, awaiting user review: D-002, D-003, D-005.
  See `DECISIONS.md`. No architecture decisions exist yet. Technology choices are **preferences only**.
- Never self-approve a gate. Run phase procedures only when the user asks.

## Exact next action
User decides whether to accept D-002/D-003/D-005, then runs the requirements phase in either tool:
- Claude Code: `/assembly-requirements`
- Antigravity CLI: `/assembly-requirements` (workspace skill)
- Antigravity IDE: `/assembly-requirements` (workflow). Fallback: "Read
  `docs/sdlc/procedures/assembly-requirements.md` and follow it."
The agent starts with the elicitation questions. It must not draft requirements from assumptions.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/` and `.mcp.json`.
