---
trigger: always_on
---

# Assembly Studio — Antigravity rules

`AGENTS.md` at the repository root contains the shared rules for every agent. Read it at the start of
every conversation if it isn't already in your context. These rules add Antigravity specifics only.

1. **Before any edit:** follow the session start in `docs/sdlc/agent-protocol.md` §3. Read
   `HANDOFF.md`, `CURRENT_TASK.md` and `PROJECT_STATE.md`, run `git status --short` and
   `git log --oneline -5`, and identify the active task. Never overwrite or revert changes you didn't make.
2. **SDLC commands:** `/assembly-*` are workspace skills (CLI, `.agent/skills/`) or workflows (IDE,
   `.agent/workflows/`). Each one runs the tool-independent procedure in `docs/sdlc/procedures/`.
   Start a phase procedure only when the user explicitly runs it or names it. Never start one on
   your own initiative, even if it looks like the logical next step: suggest it instead.
3. **No equivalent of Claude Code commands:** don't try to run `.claude/skills/` files or Claude
   slash-command syntax. Use the files under `docs/sdlc/` and `.agent/`.
4. **Same IDs and documents as every other agent:** requirements, ADRs, task IDs and acceptance
   criteria come from `docs/` and the shared memory files. Never create parallel numbering, plans or
   task lists in Antigravity artifacts. Antigravity task lists and implementation plans are
   working notes only, and `CURRENT_TASK.md` remains authoritative.
5. **Approvals:** never treat your own artifact review or plan acceptance as an SDLC gate approval.
   Gate approvals are explicit user statements recorded in `PROJECT_STATE.md`.
6. **After meaningful work:** complete the session end in `docs/sdlc/agent-protocol.md` §6. Record
   yourself as `Antigravity (<CLI or IDE>, <model>)` in `WORK_LOG.md` and `CURRENT_TASK.md`, and rewrite
   `HANDOFF.md` before the conversation ends or the user switches tools.
7. **Evidence:** report only builds, tests and commits you actually ran and saw succeed. Use the
   system clock for timestamps.
