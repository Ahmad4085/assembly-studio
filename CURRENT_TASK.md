# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## Next: TASK-010 Core abstractions v1

| Field | Value |
|---|---|
| Status | **Ready** (approved, not started) |
| Owner | — (unassigned. The next agent sets itself as owner when starting) |
| Started | — |
| Definition | `docs/05-planning/backlog.md` § TASK-010 (goal, scope, tests, DoD) |
| Procedure | `docs/sdlc/procedures/assembly-implement.md` (invoke with `/assembly-implement TASK-010`) |
| Depends on | TASK-001 (Done ✔) |

**Parallel user action:** TASK-006 (clean test VMs) can start any time.

---

## Last completed: TASK-005, Bundled NASM tooling layout (Done 2026-10-27 12:00 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- `build/fetch-nasm.ps1`: Fetches and extracts pinned NASM 3.02 win64 binary into `tools/nasm/` (git-ignored per ADR-0005).
- Verifies archive SHA-256 (`161d0bfa...`), extracts `nasm.exe`, `ndisasm.exe`, and `LICENSE` (BSD-2-Clause).
- Verifies extracted file hashes and writes deterministic `tools/nasm/SHA256SUMS.txt` and `tools/nasm/SOURCE.txt` (no timestamps).
- `NasmLocator` test helper in `tests/AssemblyStudio.Toolchain.Nasm.Tests` with 6 passing tests: locating nasm, verifying version 3.02, ndisasm locator, BSD-2-Clause license & source metadata, SHA-256 manifest integrity, and actionable failure on missing tool.
- Release build: 0 warnings, 0 errors. Solution tests: 58/58 passed (9 test projects). `build/coverage.ps1`: exit 0.

---

## Earlier: TASK-004, Coverage tooling (Done 2026-10-27 10:48 +0500, Claude Code)
- `coverlet.MTP` 10.1.0 (MIT) pinned in `Directory.Packages.props`, referenced by every test project via `Directory.Build.props`.
- `build/coverage.ps1`: per-project Cobertura, merged per product assembly, `artifacts/coverage/summary.md`; exit 1 on a failed or missing run.
- Skeleton report: 9 Cobertura files from 9 projects, all assemblies "n/a (no coverable lines)".
