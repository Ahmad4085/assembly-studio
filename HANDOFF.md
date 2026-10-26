# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-26 17:22 +0500 by Claude Code (Opus 5.5), after recording the requirements approval. Git state updated for the SDLC-003 commit |
| Current task | **SDLC-004 Research phase: `Ready`, not started, no owner** |
| Last completed | SDLC-003 Requirements phase (Done. Set v1.0 approved 2026-10-26 17:11 +0500) |
| Git state | At writing: HEAD `7ff6f03`. **Planned:** all pending SDLC-003 changes (requirements set, alignment edits, shared memory, this handoff) go into the commit `SDLC-003: Requirements set v1.0 (approved)`. Verify with `git log --oneline -1` (expect that message on top of `7ff6f03`) and `git status --short` (expect only `?? .idea/vcs.xml`). **Untracked, not ours:** `.idea/vcs.xml`. Any other change is someone else's |

## Last verified working state
- Requirements set **v1.0 Approved** (`docs/01-requirements/`). All decisions D-001 to D-010 approved. No ADRs yet.
- No application code. No builds or tests exist.

## Changes since the previous handoff
- Requirements approved. 117 requirements APPROVED, 12 NEEDS_RESEARCH (approved as goals, details after research).
- D-010 approved: `docs/01-requirements/` 7-file set, single traceability matrix, P0–P3 priorities.
- SDLC-004 (Research) defined in `CURRENT_TASK.md` with its priority questions.

## Failed tests and known issues
- No failed tests (none exist). BLK-001: no .NET SDK (blocks phase 6+, not research).
- NFR targets with basis PROPOSED are not yet validated, so don't treat them as measured.
- Research needs web access to primary sources. Anything not checked must be labeled `[Unverified]`.

## Decisions and constraints to respect
- D-006 NASM syntax only. D-007 permissive open source: GPL candidates fail the license gate unless the user changes D-007.
  D-008 text I/O + keyboard DOS subset. D-009 English, localizable. D-010 requirements layout.
- Research recommends. Only ADRs (architecture phase) decide. POC code needs explicit user approval and lives in `poc/`.

## Exact next action
User runs the research phase in either tool:
- Claude Code: `/assembly-research` (or `/assembly-research engine` for one topic)
- Antigravity CLI: `/assembly-research` (workspace skill)
- Antigravity IDE: `/assembly-research` (workflow). Fallback: "Read `docs/sdlc/procedures/assembly-research.md` and follow it."

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/` and `.mcp.json`.
