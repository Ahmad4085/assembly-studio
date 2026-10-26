# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-26 19:18 +0500 by Claude Code (Opus 5.5), after recording the backlog approval. Git state updated for the SDLC-008 commit |
| Current task | **TASK-001 Solution skeleton: `Ready`, approved, not started, no owner** |
| Last completed | SDLC-008 Implementation plan (Done. Backlog v1.0 approved 2026-10-26 19:17 +0500) |
| Git state | At writing: HEAD `41d05b1`. **Planned:** all pending SDLC-008 changes (`docs/05-planning/backlog.md`, traceability v2.3, shared memory, this handoff) go into the commit `SDLC-008: Implementation backlog v1.0 (approved)`. Verify with `git log --oneline -1` (expect that message on top of `41d05b1`) and `git status --short` (expect only `?? .idea/vcs.xml`). **Untracked, not ours:** `.idea/vcs.xml` |

## Last verified working state
- All SDLC documents approved through the plan: requirements v2.0, research v1.0, architecture v1.1, ADR-0001..0009 (0007 v1.1), ui-spec v1.0, tokens v1.0, backlog v1.0.
- No product code exists yet (`src/` absent). POC-001/002 green at their last runs.

## Changes since the previous handoff
- Backlog approved: 62 tasks `Approved`. Implementation phase opens with M0.

## Failed tests and known issues
- No failed tests. Open: OQ-007 (license → TASK-100), OQ-020 (signing, release), OQ-025 (fonts/icons → TASK-070), OQ-022/024 (post-MVP).

## Decisions and constraints to respect
- D-001 to D-015, ADR-0001..0009 (0007 v1.1), ui-spec, tokens and backlog approved.
- Implement one approved task at a time with `/assembly-implement <TASK-ID>`. Product code goes in `src/` and `tests/`. POC code is reference only.
- Never use destructive Git commands without explicit user instruction.

## Exact next action
1. User runs `/assembly-implement TASK-001` (backlog § TASK-001).
2. In parallel (user action): TASK-006, provision clean Windows 11 24H2 x64 and Windows 10 Enterprise LTSC 21H2 x64 VMs and record them in
   `docs/06-implementation/environments.md`.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/` and `.mcp.json`.
