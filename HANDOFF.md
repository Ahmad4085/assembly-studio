# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-27 10:48 +0500 by Claude Code (Opus 5.5), after completing TASK-004 |
| Current task | **TASK-005 Bundled NASM tooling layout: `Ready`, approved, not started, no owner** |
| Last completed | TASK-004 Coverage tooling (Done 2026-10-27 10:48 +0500) |
| Git state | At writing: HEAD `e299d3c` (not pushed). TASK-004 and `build/singlesteptests-8088-v2.sha256` are committed right after this file as `TASK-004: Coverage tooling (coverlet.MTP), 8088 v2 manifest` (on the user's request, not pushed). **Untracked, not ours:** `.idea/vcs.xml` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → 0 warnings, 0 errors. `dotnet test --solution AssemblyStudio.slnx -c Release` → **53/53 passed** (2026-10-27 10:48 +0500).
- `build/coverage.ps1` → exit 0; summary in `artifacts/coverage/summary.md` (skeleton: no coverable lines yet).
- `build/fetch-singlesteptests.ps1` (default, 8086) → 324/324 verified. 8088 manifest verified against upstream, script run with `-Include8088` not yet done.

## Changes since the previous handoff
- TASK-004: `coverlet.MTP` 10.1.0 (MIT) in all test projects, `build/coverage.ps1`. ADR-0008 v1.2 (coverage verified, 8088 manifest).

## Failed tests and known issues
- No failed tests. coverlet reports 0-line assemblies as rate 1; the summary shows "n/a" for them. Use the summary, not the raw `line-rate`.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-015.
- Coverage collects only with `--coverlet`. Threshold enforcement is not planned in M0.
- Oracle files are verified only against committed manifests. A new src project must be added to `AllowedProductDependencies`.

## Exact next action
1. User runs `/assembly-implement TASK-005`.
2. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/` and `.mcp.json`.
