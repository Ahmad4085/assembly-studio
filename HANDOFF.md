# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-26 18:07 +0500 by Claude Code (Opus 5.5), after recording the research approval. Git state updated for the SDLC-004 commit |
| Current task | **SDLC-005 requirements change (D-011, D-012): `Ready`, not started, no owner** |
| Last completed | SDLC-004 Research phase (Done. Set v1.0 approved 2026-10-26 17:59 +0500) |
| Git state | At writing: HEAD `86784bb`. **Planned:** all pending SDLC-004 changes (research set, POC definitions, OPEN_QUESTIONS v1.1, implement procedure note, shared memory, this handoff) go into the commit `SDLC-004: Research v1.0 (approved), D-011..D-013`. Verify with `git log --oneline -1` (expect that message on top of `86784bb`) and `git status --short` (expect only `?? .idea/vcs.xml`). **Untracked, not ours:** `.idea/vcs.xml`. Any other change is someone else's |

## Last verified working state
- Requirements v1.0 approved (CON-001, NFR-025, FR-039 Outdated until SDLC-005). Research v1.0 approved.
- **.NET SDK 10.0.403 installed and working** (a WPF template build passed). No product code. No product tests.

## Changes since the previous handoff
- Research approved. Decisions D-011 (Windows support A), D-012 (AH=00h), D-013 (POCs + SDK).
- SDK installed, BLK-001 resolved. POC-001/002 defined (`docs/02-research/poc-00x.md`), approved, not started.

## Failed tests and known issues
- No failed tests (none exist).
- Requirements CON-001, NFR-025 and FR-039 contradict D-011/D-012 until SDLC-005. Follow the decisions, not the old text.

## Decisions and constraints to respect
- D-001 to D-013 approved. Research recommendations are inputs. ADRs decide.
- POC code goes in `poc/` only and is never promoted to `src/` without a backlog task.

## Exact next action
1. User runs `/assembly-requirements apply D-011 and D-012` (SDLC-005). The agent edits in place (set v1.1) and asks for re-approval.
2. User runs `/assembly-implement POC-001` and `/assembly-implement POC-002` (in either order; one task at a time).
3. Then `/assembly-architecture`.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/` and `.mcp.json`.
