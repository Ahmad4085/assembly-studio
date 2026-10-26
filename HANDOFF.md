# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-26 18:21 +0500 by Claude Code (Opus 5.5), after recording the requirements v2.0 approval. Git state updated for the SDLC-005 commit |
| Current task | **POC-001 (custom C# 8086 core feasibility): `Ready`, approved, not started, no owner** |
| Last completed | SDLC-005 Requirements change (Done. Set v2.0 approved 2026-10-26 18:18 +0500) |
| Git state | At writing: HEAD `7deda79`. **Planned:** all pending SDLC-005 changes (requirements v2.0, research *Depends on*, shared memory, this handoff) go into the commit `SDLC-005: Requirements v2.0 (approved)`. Verify with `git log --oneline -1` (expect that message on top of `7deda79`) and `git status --short` (expect only `?? .idea/vcs.xml`). **Untracked, not ours:** `.idea/vcs.xml`. Any other change is someone else's |

## Last verified working state
- Requirements v2.0 approved, research v1.0 approved, both consistent (validators ALL OK). .NET SDK 10.0.403 installed.
- No product code. No product tests. No POC run yet.

## Changes since the previous handoff
- Requirements v2.0 approved (D-011 Windows support, D-012 INT 21h AH=00h, AC-064). Research *Depends on* → v2.0.

## Failed tests and known issues
- No failed tests (none exist). No open blockers.

## Decisions and constraints to respect
- D-001 to D-013 approved. Requirements v2.0 and research v1.0 approved. POC code only in `poc/`, never promoted to `src/` without a task.
- POC-001 constraints (`docs/02-research/poc-001.md`): no GPL code or data; SingleStepTests 8086 (MIT) as the oracle; the test data is downloaded once and its source recorded.

## Exact next action
1. User runs `/assembly-implement POC-001`. The agent follows `docs/02-research/poc-001.md` and records `[Measured]` results there.
2. Then `/assembly-implement POC-002`.
3. Then `/assembly-architecture`.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/` and `.mcp.json`.
