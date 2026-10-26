# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## TASK-004: Coverage tooling (coverlet with MTP)

| Field | Value |
|---|---|
| Status | **Ready** (approved, not started) |
| Owner | — (unassigned. The next agent sets itself as owner when starting) |
| Started | — |
| Definition | `docs/05-planning/backlog.md` § TASK-004 (goal, scope, tests, DoD) |
| Procedure | `docs/sdlc/procedures/assembly-implement.md` (invoke with `/assembly-implement TASK-004`) |
| Depends on | TASK-001 (Done ✔). ADR-0008 approved ✔ |

**Objective:** Verify and enable line coverage under Microsoft.Testing.Platform.

**Acceptance criteria:** Identify and verify the coverlet MTP package and its license (ADR-0008 unverified item). `build/coverage.ps1` producing a report. Record finding in ADR-0008 validation section. Coverage report generated for skeleton tests. Standard DoD (backlog §4).

**Validation performed:** none yet.
**Remaining work:** all of it. **Parallel user action:** TASK-006 (clean Windows 11 24H2 and Windows 10 Enterprise LTSC 21H2 VMs) can start any time.

---

## Last completed: TASK-003, SingleStepTests oracle data fetch (Done 2026-10-26, Antigravity)
- Created `build/fetch-singlesteptests.ps1` and `build/singlesteptests-8086-v1-files.txt`.
- Fetched and verified all 324 SingleStepTests 8086 v1 hardware oracle capture files (commit `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`) into `testdata/singlesteptests/8086/` (git-ignored).
- Generated `SHA256SUMS.txt` and `SOURCE.txt` (recording MIT license, commit, source URL, file count).
- Added `-Include8088` switch for secondary 8088 v2 oracle data.
- Added `testdata/` and `tools/nasm/` to `.gitignore`.
- Created `SingleStepOracleHelper.cs` and `SingleStepOracleHelperTests.cs` in `tests/AssemblyStudio.Engine.I8086.Tests/Oracle/`.
- Verified idempotence (hash match: `900D6BB7...`), verified missing data produces clear actionable failure instructions, and all 37 solution tests pass.
