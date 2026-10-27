# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## Rework: review fixes for TASK-001/002/003 (explicit user request)

| Field | Value |
|---|---|
| Status | **In progress**: code fixes done and committed, one item open (8088 v2 manifest) |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-26 22:48 +0500 |
| Request | User: "yes, fix them and log it" (after Claude Code's review of Antigravity's TASK-001..003) |

**Scope:** (1) TASK-003 fetch script: fail on download errors, no partial files, verify against committed SHA-256 manifests,
pin and fully fetch 8088 v2, stable `SOURCE.txt`. (2) TASK-002: add a `.csproj`-level rule test (unused forbidden references).
(3) TASK-001: line-ending consistency (`.editorconfig` vs repo LF, add `.gitattributes`).

**Done and verified:**
- `build/fetch-singlesteptests.ps1` rewritten: verifies every file (cached or downloaded) against a committed SHA-256 manifest; downloads to `.part`
  and moves only after the hash matches; any failure → exit 1; stray files reported; deterministic `SOURCE.txt`; 8088 v2 pinned to commit
  `aea84484abc79d09639d855b7b0ab32bc9e4dbeb`; refuses a set whose manifest is missing. `build/singlesteptests-8086-v1-files.txt` replaced by
  `build/singlesteptests-8086-v1.sha256` (324 files, all matched GitHub's git blob SHA-1 at the pinned commit).
- `tests/AssemblyStudio.ArchitectureTests/ProjectFileRuleTests.cs`: 16 `.csproj` rule tests. Negative checks in a scratch copy: unused forbidden
  ProjectReference, `UseWPF` in Runtime, unlisted src project → each fails 1 test; clean → pass.
- `.editorconfig` → `end_of_line = lf`; `.gitattributes` added (`* text=auto eol=lf`, CRLF for .cmd/.bat, binaries).
- ADR-0008 v1.1 (validation results recorded). Release build 0 warnings; 53/53 tests (2026-10-27 10:15 +0500).

**Open:** 8088 v2 manifest `build/singlesteptests-8088-v2.sha256` not created yet. First download stopped after 142/324 verified files
(connection reset by GitHub); resumed with retries. Until the manifest exists, `-Include8088` stops with a clear error. The default run is unaffected.

---

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
