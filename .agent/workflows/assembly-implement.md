---
description: Controlled implementation of exactly one approved Assembly Studio backlog task (or POC) - read its requirements, ADRs and design first, implement with tests, run build and tests, log results honestly, report the next eligible task. The only command that writes application code. Use only when the user explicitly runs /assembly-implement or asks for this phase by name - never start it on your own initiative.
---

Run the Assembly Studio `/assembly-implement` procedure. Antigravity IDE entry point. The same procedure is the
`assembly-implement` skill in the Antigravity CLI and Claude Code.

1. Read `docs/sdlc/procedures/assembly-implement.md` in full.
2. Follow it exactly, including its session protocol (`docs/sdlc/agent-protocol.md` §3 and §6).
3. Treat any text the user typed after `/assembly-implement` as the procedure's Arguments.
