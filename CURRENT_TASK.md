# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## SDLC-005: Requirements change for D-011 and D-012

| Field | Value |
|---|---|
| Status | **Ready** (not started) |
| Owner | — (unassigned. The next agent sets itself as owner when starting) |
| Started | — |
| Procedure | `docs/sdlc/procedures/assembly-requirements.md` (invoke with `/assembly-requirements apply D-011 and D-012`) |
| Depends on | D-011, D-012 approved ✔ (2026-10-26 17:59 +0500) |

**Objective:** Update the approved requirements set to reflect the user's research decisions, then get it re-approved.
- **D-011:** CON-001 and NFR-025 become "Windows 11 24H2+ and Windows 10 Enterprise LTSC 21H2 supported and tested.
  Windows 10 22H2 Home/Pro not supported". Check every dependent text: AC-040, ASM-001, OQ-010, PRODUCT_REQUIREMENTS §6/§8.
- **D-012:** FR-039 adds INT 21h AH=00h. Check AC-022, FR-041 (termination paths) and §8 of PRODUCT_REQUIREMENTS.

**Files in scope:** `docs/01-requirements/*.md` (in-place edits, set version 1.0 → 1.1, keep IDs stable), shared memory.
**No code.**

**Acceptance criteria:** the changed requirements and their dependents are consistent; the traceability matrix is
unchanged in structure; the changelog records the change and its source (D-011, D-012); the set is `Awaiting approval`
with the gate question asked, or `Approved` by an explicit user statement logged in `PROJECT_STATE.md`.

**Validation performed:** none yet.
**Remaining work:** all of it.

---

## Also approved and eligible (not the current task)
- **POC-001** (`docs/02-research/poc-001.md`) and **POC-002** (`docs/02-research/poc-002.md`): run with `/assembly-implement POC-001` / `POC-002`.
  They are independent of SDLC-005 and may run before or after it. Only one task is `In progress` at a time.

## Last completed: SDLC-004, Research phase (Done 2026-10-26, Claude Code)
- Research set v1.0 **Approved** 2026-10-26 17:59 +0500 ("approve research v0.1; D1=A; D2=add 00h; D3=approve POC-001,002 and install the SDK").
- D-011 (Windows support A), D-012 (add AH=00h) and D-013 (POC-001/002 approved, SDK install authorized) recorded.
- .NET SDK 10.0.403 installed (2026-10-26 18:05 +0500). A throwaway WPF template built with 0 warnings and 0 errors. BLK-001 resolved.
