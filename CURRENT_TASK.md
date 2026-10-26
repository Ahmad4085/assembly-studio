# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## SDLC-004: Research phase

| Field | Value |
|---|---|
| Status | **Ready** (not started) |
| Owner | — (unassigned. The next agent sets itself as owner when starting) |
| Started | — |
| Procedure | `docs/sdlc/procedures/assembly-research.md` (invoke with `/assembly-research [topic]`) |
| Depends on | Requirements set v1.0 approved ✔ (2026-10-26 17:11 +0500) |

**Objective:** Gather verified evidence (primary sources, licenses, versions, capabilities) for the decisions the
architecture must make, compare options against the approved requirements, and recommend. Research recommends;
decisions are made as ADRs in `/assembly-architecture`.

**Requirements and inputs:** `docs/01-requirements/` (approved v1.0), especially `OPEN_QUESTIONS.md`. Guidance:
`docs/sdlc/guides/assembly-emulator-research.md`. Template: `docs/templates/research-evaluation.md`.
Priority questions: OQ-001 and OQ-008 (execution engine and its state access, breakpoints, snapshots, determinism),
OQ-002 (NASM redistribution, minimum version, `CPU 8086` behavior), OQ-009 (source-line mapping), OQ-003 (DOS service
semantics), OQ-004 (.COM vs MZ), OQ-005, OQ-010 and OQ-013 (.NET/WPF platform and deployment), OQ-015 and OQ-016
(flag and CPU semantics, reference test suites), OQ-014 (reference machine). All candidates must be license-compatible
with permissive open source (D-007).

**Files in scope:** `docs/02-research/*.md` (new), optional `poc/` **only with explicit user approval per POC**.
Updates to `docs/01-requirements/OPEN_QUESTIONS.md` status columns (minor), and shared memory files. **No product code.**

**Acceptance criteria (procedure exit conditions):**
- Every license, version, platform and capability claim carries an evidence label (`[Verified: source, date]`,
  `[Measured]`, `[Unverified]`, `[Hypothesis]`). No benchmark numbers without `[Measured]`.
- Each recommendation cites the requirement IDs it satisfies and the gaps it leaves.
- `docs/02-research/research-summary.md` links the topic files and lists the unverified items with a resolution plan.
- The summary is `Approved` by an explicit user statement logged in `PROJECT_STATE.md`, or is `Awaiting approval`
  with the gate question asked.

**Validation performed:** none yet.
**Remaining work:** all of it. Needs web access to primary sources. Without it, findings must be labeled `[Unverified]`.

---

## Last completed: SDLC-003, Requirements phase (Done 2026-10-26, Claude Code)
- Requirements set v1.0 **Approved** 2026-10-26 17:11 +0500 ("approve requirements v0.1 and D-010"). D-006 to D-010 approved.
- 96 FR + 33 NFR: 117 APPROVED, 12 NEEDS_RESEARCH. 26 US, 63 AC, 19 OQ.
- Validation: generator checks (IDs, references, coverage, priorities, vague wording) passed for v0.1 and v1.0. A v0.1
  regeneration was byte-identical to the files before approval, and v1.0 differs only in statuses and approval text.
  Repository structural validator passed after the approval edits (ALL OK). Details: `WORK_LOG.md`.
