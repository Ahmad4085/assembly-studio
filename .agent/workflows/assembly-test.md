---
description: Create, extend and execute Assembly Studio test suites - CPU instruction semantics, registers, flags, memory, execution state, assembler integration, error handling - and record an honest test report with defects and regressions. Use only when the user explicitly runs /assembly-test or asks for this phase by name - never start it on your own initiative.
---

Run the Assembly Studio `/assembly-test` procedure. Antigravity IDE entry point. The same procedure is the
`assembly-test` skill in the Antigravity CLI and Claude Code.

1. Read `docs/sdlc/procedures/assembly-test.md` in full.
2. Follow it exactly, including its session protocol (`docs/sdlc/agent-protocol.md` §3 and §6).
3. Treat any text the user typed after `/assembly-test` as the procedure's Arguments.
