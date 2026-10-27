---
description: Initialize or repair the Assembly Studio multi-agent SDLC workspace - shared rules and memory files, tool-independent procedures, templates and Claude Code/Antigravity entry points. Validates an existing setup without overwriting it. Never implements the IDE. Use only when the user explicitly runs /assembly-init or asks for this phase by name - never start it on your own initiative.
---

Run the Assembly Studio `/assembly-init` procedure. OpenCode entry point. The same procedure is the `assembly-init` skill in Claude Code
and the Antigravity CLI, and a workflow in the Antigravity IDE.

1. Read `docs/sdlc/procedures/assembly-init.md` in full.
2. Follow it exactly, including its session protocol (`docs/sdlc/agent-protocol.md` §3 and §6).

Arguments: $ARGUMENTS
