# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## POC-001: Custom C# 8086 core feasibility

| Field | Value |
|---|---|
| Status | **Ready** (approved, not started) |
| Owner | — (unassigned. The next agent sets itself as owner when starting) |
| Started | — |
| Definition | `docs/02-research/poc-001.md` (goal, scope, exit criteria, constraints) |
| Procedure | `docs/sdlc/procedures/assembly-implement.md` (invoke with `/assembly-implement POC-001`) |
| Depends on | D-013 approval ✔. .NET SDK 10.0.403 ✔ |

**Objective:** prove (or disprove) that a managed C# 8086 core executes a representative opcode subset exactly as SingleStepTests
8086 records, and **measure** step latency and throughput against NFR-002 and NFR-003. Throwaway code in `poc/POC-001-cpu-core/`.

**Acceptance criteria:** the exit criteria in `docs/02-research/poc-001.md`: pass rate per opcode, measured latency and throughput,
and a feasibility verdict, recorded there as `[Measured]`.

**Validation performed:** none yet.
**Remaining work:** all of it. POC-002 (`docs/02-research/poc-002.md`) is approved too and runs after this, one task at a time.

---

## Last completed: SDLC-005, Requirements change (Done 2026-10-26, Claude Code)
- Requirements set **v2.0 Approved** 2026-10-26 18:18 +0500 ("approve requirements v2.0"): D-011 and D-012 applied. FR-039, FR-041, NFR-025 → APPROVED. New AC-064.
- Validation: markdown consistency validator ALL OK (96 FR, 33 NFR, 64 AC, 26 US, 129 rows; 118 APPROVED, 11 NEEDS_RESEARCH).
  Research documents revalidated (*Depends on* → v2.0, minor). Repository structural validator ALL OK.
