# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-26 18:45 +0500 by Claude Code (Opus 5.5), end of the POC-002 session. Git state updated for the POC-002 commit |
| Current task | **SDLC-006 Architecture phase: `Ready`, not started, no owner** |
| Last completed | POC-002 NASM source mapping (Done 2026-10-26 18:42 +0500, **sufficient with limitations**) |
| Git state | At writing: HEAD `bfd0324`. **Planned:** all pending POC-002 changes (`poc/POC-002-source-mapping/` sources and fixtures, `poc-002.md`, OPEN_QUESTIONS v2.2, shared memory, this handoff) go into the commit `POC-002: NASM source mapping (sufficient with limitations)`. Git-ignored and not committed: `poc/**/bin`, `obj`. Verify with `git log --oneline -1` (expect that message on top of `bfd0324`) and `git status --short` (expect only `?? .idea/vcs.xml`). **Untracked, not ours:** `.idea/vcs.xml` |

## Last verified working state
- POC-002: `dotnet build Poc.Mapping.slnx -c Release` 0/0; `dotnet test --solution Poc.Mapping.slnx -c Release` 24/24 (2026-10-26 18:42 +0500).
- POC-001: 94/94 (178,000 hardware cases) at its last run. Requirements v2.0 (OPEN_QUESTIONS v2.2) and research v1.0 approved. No product code.

## Changes since the previous handoff
- POC-002 done: listing + map mapping verified by three independent checks. Macro/include attribution rules and limitations documented.

## Failed tests and known issues
- No failed tests. No open defects.
- POC-002 limitations to address in architecture: undocumented NASM listing layout (pin the version, keep the fixtures), `<10>`+ nesting,
  nested includes, `%rep` [Unverified].
- `dotnet test` on .NET 10 + xUnit v3 needs `global.json` MTP mode and `--solution`/`--project`.

## Decisions and constraints to respect
- D-001 to D-013 approved. Research recommendations + POC results are inputs. ADRs decide. POC code stays in `poc/`.

## Exact next action
User runs `/assembly-architecture` (SDLC-006). Inputs: `docs/02-research/research-summary.md`, `poc-001.md` §4, `poc-002.md`.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/` and `.mcp.json`.
