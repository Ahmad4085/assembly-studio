---
description: Release preparation for Assembly Studio - verify prerequisites, build and test the release, prepare Windows packaging and install docs, check dependency licenses and distribution obligations, produce release notes and checklist. Never marks ready without passing checks; never publishes without approval. Use only when the user explicitly runs /assembly-release or asks for this phase by name - never start it on your own initiative.
---

Run the Assembly Studio `/assembly-release` procedure. Antigravity IDE entry point. The same procedure is the
`assembly-release` skill in the Antigravity CLI and Claude Code.

1. Read `docs/sdlc/procedures/assembly-release.md` in full.
2. Follow it exactly, including its session protocol (`docs/sdlc/agent-protocol.md` §3 and §6).
3. Treat any text the user typed after `/assembly-release` as the procedure's Arguments.
