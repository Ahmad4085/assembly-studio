# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-27 12:05 +0500 by Antigravity (IDE, Gemini 3.8 Flash), after completing TASK-005 |
| Current task | **TASK-010 Core abstractions v1: `Ready`, approved, not started, no owner** |
| Last completed | TASK-005 Bundled NASM tooling layout (Done 2026-10-27 12:00 +0500) |
| Git state | HEAD `23e0fca` (not pushed). Uncommitted working tree changes for TASK-005 (`build/fetch-nasm.ps1`, `tests/AssemblyStudio.Toolchain.Nasm.Tests/NasmLocator*.cs`, `.gitignore`, shared memory). Untracked, not ours: `.idea/vcs.xml`, `.opencode/`, `opencode.jsonc` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → 0 warnings, 0 errors. `dotnet test --solution AssemblyStudio.slnx -c Release` → **58/58 passed** (2026-10-27 12:00 +0500).
- `build/coverage.ps1` → exit 0; summary in `artifacts/coverage/summary.md` (skeleton: no coverable lines yet).
- `build/fetch-nasm.ps1` → exit 0; idempotent and hash-verified (`161d0bfa...`), extracted `nasm.exe`, `ndisasm.exe`, and `LICENSE` in `tools/nasm/`.
- `build/fetch-singlesteptests.ps1` (default, 8086) → 324/324 verified. 8088 manifest verified against upstream.

## Changes since the previous handoff
- TASK-005: `build/fetch-nasm.ps1` (hash verification, atomic download, deterministic `SOURCE.txt` and `SHA256SUMS.txt`), `.gitignore` updated for `tools/`, `NasmLocator` and `NasmLocatorTests` in `tests/AssemblyStudio.Toolchain.Nasm.Tests` (6 tests).

## Failed tests and known issues
- None.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-015.
- Pinned NASM 3.02 per ADR-0005. Missing toolchain binaries must fail with actionable instructions rather than silently falling back to PATH.
- Deterministic metadata in fetch scripts (no runtime timestamps).

## Exact next action
1. Optional: commit TASK-005 pending changes (`git add ...; git commit -m "TASK-005: Bundled NASM tooling layout"`).
2. Begin Milestone M1: user runs `/assembly-implement TASK-010`.
3. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `.opencode/`.
