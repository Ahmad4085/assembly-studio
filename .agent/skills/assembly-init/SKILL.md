---
name: assembly-init
description: Initialize or repair the Assembly Studio multi-agent SDLC workspace - shared rules and memory files, tool-independent procedures, templates and Claude Code/Antigravity entry points. Validates an existing setup without overwriting it. Never implements the IDE. Use only when the user explicitly runs /assembly-init or asks for this phase by name - never start it on your own initiative.
---

Run the Assembly Studio `/assembly-init` procedure.

1. Read `docs/sdlc/procedures/assembly-init.md` in full.
2. Follow it exactly, including its session protocol.
3. Treat any text the user typed after `/assembly-init` as the procedure's Arguments.
