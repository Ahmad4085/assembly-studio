---
description: Quality assurance for Assembly Studio - verify implemented behavior against requirements and acceptance criteria, check UI, usability, accessibility, offline behavior, reliability, visualization integrity and compatibility limits, and issue a verdict that blocks release on critical failures. Use only when the user explicitly runs /assembly-qa or asks for this phase by name - never start it on your own initiative.
---

Run the Assembly Studio `/assembly-qa` procedure. Antigravity IDE entry point. The same procedure is the
`assembly-qa` skill in the Antigravity CLI and Claude Code.

1. Read `docs/sdlc/procedures/assembly-qa.md` in full.
2. Follow it exactly, including its session protocol (`docs/sdlc/agent-protocol.md` §3 and §6).
3. Treat any text the user typed after `/assembly-qa` as the procedure's Arguments.
