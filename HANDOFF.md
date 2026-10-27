# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-27 10:15 +0500 by Claude Code (Opus 5.5), after the review and rework of TASK-001..003 |
| Current task | **Rework of TASK-001..003: code done and committed. Open: 8088 v2 manifest (Claude Code, download in progress)**. Then TASK-004 `Ready` |
| Last completed | TASK-003 (Antigravity), reworked by Claude Code (see `WORK_LOG.md`) |
| Git state | At writing: HEAD `6aa04c3`; the rework is committed right after this file as `TASK-002/003 rework: verified oracle fetch, project-file rules, LF` (not pushed). **Untracked, not ours:** `.idea/vcs.xml` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → 0 warnings, 0 errors. `dotnet test --solution AssemblyStudio.slnx -c Release` → **53/53 passed** (2026-10-27 10:15 +0500).
- `build/fetch-singlesteptests.ps1` (default, 8086) → exit 0, 324/324 files verified against `build/singlesteptests-8086-v1.sha256`.

## Changes since the previous handoff
- Review of Antigravity's TASK-001..003: all reported results reproduced. Fixes: the fetch script fails loudly and verifies against committed
  SHA-256 manifests; 8088 v2 pinned (`aea84484…`); `.csproj` architecture rules (unused forbidden references, UseWPF, unlisted projects);
  LF line endings enforced (`.editorconfig`, `.gitattributes`). ADR-0008 v1.1 (validation results).

## Failed tests and known issues
- No failed tests. `-Include8088` stops with a clear error until `build/singlesteptests-8088-v2.sha256` exists.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.1), D-001..D-015.
- Oracle files are verified only against the committed manifests. Never regenerate a manifest from unverified downloads.
- Architecture rules exist twice (compiled references + `.csproj`). A new src project must be added to `AllowedProductDependencies`.

## Exact next action
1. Claude Code: when the 8088 download finishes, commit `build/singlesteptests-8088-v2.sha256` (all 324 files must match upstream git blobs) and run `-Include8088` twice.
2. User runs `/assembly-implement TASK-004`. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/` and `.mcp.json`.
