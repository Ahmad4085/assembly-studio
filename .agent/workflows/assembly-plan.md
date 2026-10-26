---
description: Implementation planning for Assembly Studio - convert approved requirements, architecture, ADRs and UI spec into milestones, an ordered backlog of scoped tasks and POCs with dependencies, tests and Definition of Done, plus traceability and risk register. Plans only. Use only when the user explicitly runs /assembly-plan or asks for this phase by name - never start it on your own initiative.
---

Run the Assembly Studio `/assembly-plan` procedure. Antigravity IDE entry point. The same procedure is the
`assembly-plan` skill in the Antigravity CLI and Claude Code.

1. Read `docs/sdlc/procedures/assembly-plan.md` in full.
2. Follow it exactly, including its session protocol (`docs/sdlc/agent-protocol.md` §3 and §6).
3. Treat any text the user typed after `/assembly-plan` as the procedure's Arguments.
