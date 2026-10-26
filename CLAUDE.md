# Assembly Studio — Claude Code Instructions

@AGENTS.md

## Claude Code specifics
- The shared rules above (imported from `AGENTS.md`) apply in full. This file adds only
  Claude Code invocation details.
- SDLC commands are project skills in `.claude/skills/`. Each is a thin wrapper that runs the
  tool-independent procedure in `docs/sdlc/procedures/<name>.md`. Never put procedure logic
  in a wrapper. Edit the procedure instead.
- The 11 phase commands (`/assembly-*`) have `disable-model-invocation: true`: only the user can
  start them. The 5 guides (`sdlc-documentation`, `assembly-emulator-research`,
  `cpu-instruction-testing`, `dotnet-wpf-engineering`, `desktop-ui-quality`) load automatically when relevant.
- Record yourself as `Claude Code (<model>)` in `WORK_LOG.md` and as the owner in `CURRENT_TASK.md`.
- Before ending a substantial task, or when the user says they're switching to another agent,
  complete the session end in `docs/sdlc/agent-protocol.md` §6, including `HANDOFF.md`.
