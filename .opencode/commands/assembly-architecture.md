---
description: System architecture for Assembly Studio - component boundaries (UI, editor, assembler integration, CPU engine, runtime, OS services, debugger, visualization), alternatives, ADRs, error handling, security, performance and testing strategy. Ends at a user approval gate. Use only when the user explicitly runs /assembly-architecture or asks for this phase by name - never start it on your own initiative.
---

Run the Assembly Studio `/assembly-architecture` procedure. OpenCode entry point. The same procedure is the `assembly-architecture` skill in Claude Code
and the Antigravity CLI, and a workflow in the Antigravity IDE.

1. Read `docs/sdlc/procedures/assembly-architecture.md` in full.
2. Follow it exactly, including its session protocol (`docs/sdlc/agent-protocol.md` §3 and §6).

Arguments: $ARGUMENTS
