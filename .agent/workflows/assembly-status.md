---
description: Read-only Assembly Studio SDLC dashboard - completed phases, approvals, outdated documents, outstanding tasks, failed tests and open defects, blockers and the next permitted command, derived from the actual documents and repository. Use only when the user explicitly runs /assembly-status or asks for this phase by name - never start it on your own initiative.
---

Run the Assembly Studio `/assembly-status` procedure. Antigravity IDE entry point. The same procedure is the
`assembly-status` skill in the Antigravity CLI and Claude Code.

1. Read `docs/sdlc/procedures/assembly-status.md` in full.
2. Follow it exactly, including its session protocol (`docs/sdlc/agent-protocol.md` §3 and §6).
3. Treat any text the user typed after `/assembly-status` as the procedure's Arguments.
