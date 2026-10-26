# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-26 18:55 +0500 by Claude Code (Opus 5.5), after recording the architecture approval. Git state updated for the SDLC-006 commit |
| Current task | **SDLC-007 UI/UX design: `Ready`, not started, no owner** |
| Last completed | SDLC-006 Architecture (Done. v1.0 + ADR-0001..0009 approved 2026-10-26 18:54 +0500) |
| Git state | At writing: HEAD `40c3185`. **Planned:** all pending SDLC-006 changes (architecture, 9 ADRs, traceability v2.1, OPEN_QUESTIONS v2.3, shared memory, this handoff) go into the commit `SDLC-006: Architecture v1.0 + ADR-0001..0009 (approved)`. Verify with `git log --oneline -1` (expect that message on top of `40c3185`) and `git status --short` (expect only `?? .idea/vcs.xml`). **Untracked, not ours:** `.idea/vcs.xml` |

## Last verified working state
- Requirements v2.0, research v1.0, architecture v1.0 + 9 ADRs approved and consistent (validators ALL OK). POC-001/002 green at their last runs.
- No product code exists.

## Changes since the previous handoff
- Architecture approved. D-014 reference machine. 7 open questions resolved by ADRs.

## Failed tests and known issues
- No failed tests. Open: OQ-007 (license choice, before release), OQ-020 (signing, release), OQ-021 (Ctrl+C routing, **design phase**), OQ-022 (post-MVP DOS functions).

## Decisions and constraints to respect
- D-001 to D-014 and ADR-0001..0009 approved. The UI must consume only immutable snapshots (ADR-0007). AvalonEdit is the editor (ADR-0006).

## Exact next action
User runs `/assembly-design` (SDLC-007). Inputs: requirements v2.0, `docs/03-architecture/architecture.md`, ADR-0006/0007, `docs/sdlc/guides/desktop-ui-quality.md`.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/` and `.mcp.json`.
