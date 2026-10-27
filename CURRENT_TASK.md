# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## TASK-005: Bundled NASM tooling layout

| Field | Value |
|---|---|
| Status | **Ready** (approved, not started) |
| Owner | — (unassigned. The next agent sets itself as owner when starting) |
| Started | — |
| Definition | `docs/05-planning/backlog.md` § TASK-005 (goal, scope, tests, DoD) |
| Procedure | `docs/sdlc/procedures/assembly-implement.md` (invoke with `/assembly-implement TASK-005`) |
| Depends on | TASK-001 (Done ✔). ADR-0005 approved ✔ |

**Validation performed:** none yet.
**Remaining work:** all of it. **Parallel user action:** TASK-006 (clean test VMs) can start any time.

**Leftover from the TASK-001..003 rework (Claude Code):** `build/singlesteptests-8088-v2.sha256` is written (324/324 matched upstream git
blobs) but uncommitted. The `-Include8088` double run was declined by the user during the TASK-004 session (time not recorded) and has not been run on it.

---

## Last completed: TASK-004, Coverage tooling (Done 2026-10-27 10:48 +0500, Claude Code)
- `coverlet.MTP` 10.1.0 (MIT) pinned in `Directory.Packages.props`, referenced by every test project via `Directory.Build.props`.
- `build/coverage.ps1`: per-project Cobertura, merged per product assembly, `artifacts/coverage/summary.md`; exit 1 on a failed or missing run.
- Skeleton report: 9 Cobertura files from 9 projects, all assemblies "n/a (no coverable lines)". Scratch probe: 2/5 + 3/5 → merged 4/5 (80.0%), as predicted.
  Failing test → exit 1, summary marked Incomplete. ADR-0008 v1.2 records the result.

## Earlier: rework of TASK-001..003 (Claude Code, committed `e299d3c`)
- Verified oracle fetch with committed SHA-256 manifests, `.csproj` architecture rules, LF line endings. See `WORK_LOG.md`.
