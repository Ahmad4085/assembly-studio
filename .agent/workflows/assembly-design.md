---
description: UI/UX specification for Assembly Studio - premium desktop IDE layout, editor behavior, panels, interaction states, themes, design tokens, accessibility and component conventions. Produces implementable specs only; builds no UI. Use only when the user explicitly runs /assembly-design or asks for this phase by name - never start it on your own initiative.
---

Run the Assembly Studio `/assembly-design` procedure. Antigravity IDE entry point. The same procedure is the
`assembly-design` skill in the Antigravity CLI and Claude Code.

1. Read `docs/sdlc/procedures/assembly-design.md` in full.
2. Follow it exactly, including its session protocol (`docs/sdlc/agent-protocol.md` §3 and §6).
3. Treat any text the user typed after `/assembly-design` as the procedure's Arguments.
