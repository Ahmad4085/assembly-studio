# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-26 18:34 +0500 by Claude Code (Opus 5.5), end of the POC-001 session. Git state updated for the POC-001 commit |
| Current task | **POC-002 (NASM listing + map source mapping): `Ready`, approved, not started, no owner** |
| Last completed | POC-001 custom C# 8086 core (Done 2026-10-26 18:33 +0500, **feasible**) |
| Git state | At writing: HEAD `6252368`. **Planned:** all pending POC-001 changes (`poc/POC-001-cpu-core/` sources, `poc-001.md`, OPEN_QUESTIONS v2.1, shared memory, this handoff) go into the commit `POC-001: Custom C# 8086 core feasibility (feasible)`. Git-ignored and not committed: `poc/**/bin`, `obj`, `testdata` (re-fetch with `fetch-testdata.ps1`). Verify with `git log --oneline -1` (expect that message on top of `6252368`) and `git status --short` (expect only `?? .idea/vcs.xml`). **Untracked, not ours:** `.idea/vcs.xml` |

## Last verified working state
- POC-001: `dotnet build Poc.Cpu.slnx -c Release` 0 warnings/0 errors; `dotnet test --solution Poc.Cpu.slnx -c Release` 94/94 passed (2026-10-26 18:33 +0500).
- Requirements v2.0 (OPEN_QUESTIONS v2.1, minor) and research v1.0 approved. No product code exists.

## Changes since the previous handoff
- POC-001 done: 178,000/178,000 hardware-recorded cases pass, ~48 M instr/s. Results and architecture inputs in `docs/02-research/poc-001.md`.

## Failed tests and known issues
- No failed tests. No open defects.
- **.NET 10 SDK + xUnit v3:** `dotnet test` needs MTP mode via `global.json` (`"test": {"runner": "Microsoft.Testing.Platform"}`) and `--solution`/`--project` arguments.
- When running Bash on Windows, pass timestamps from bash `date`. Python `os.popen('date')` runs the Windows `date` command (incident on SDLC-005).

## Decisions and constraints to respect
- D-001 to D-013 approved. POC code stays in `poc/`, never promoted to `src/` without a backlog task.

## Exact next action
1. User runs `/assembly-implement POC-002` (definition: `docs/02-research/poc-002.md`; NASM 3.02 is on PATH).
2. Then `/assembly-architecture`.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/` and `.mcp.json`.
