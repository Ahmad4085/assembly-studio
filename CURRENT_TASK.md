# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## SDLC-008: Implementation plan

| Field | Value |
|---|---|
| Status | **Ready** (not started) |
| Owner | — (unassigned. The next agent sets itself as owner when starting) |
| Started | — |
| Procedure | `docs/sdlc/procedures/assembly-plan.md` (invoke with `/assembly-plan`) |
| Depends on | Requirements v2.0, research v1.0, architecture v1.1, ADR-0001..0009 (0007 v1.1), ui-spec v1.0, design tokens v1.0, all approved ✔ |

**Objective:** produce `docs/05-planning/backlog.md` (milestones, ordered `TASK-###` with scope, dependencies, test requirements and Definition of Done,
risk register) and extend `docs/01-requirements/REQUIREMENTS_TRACEABILITY.md` with a Tasks column, then obtain approval (per milestone allowed).
**No code.**

**Key inputs:** ADR-0002 (projects and dependency rules), ADR-0008 (MTP, oracle data fetch), the validation items the ADRs assign to "first … task"
(AvalonEdit on net10 + Narrator, Iced on net10, fonts and icons OQ-025, text scaling, theme contrast test, coverlet MTP, NASM fixtures for `<10>`/nested includes/`%rep`),
POC-001/002 code as references (not to be promoted directly).

**Acceptance criteria:** the procedure's validation (`docs/sdlc/procedures/assembly-plan.md` §6).

**Validation performed:** none yet.
**Remaining work:** all of it.

---

## Last completed: SDLC-007, UI/UX design (Done 2026-10-26, Claude Code)
- ui-spec v1.0 + design tokens v1.0 **Approved** 2026-10-26 19:03 +0500 ("approve", given to the gate question naming spec, tokens, ADR-0007 v1.1 and Ctrl+C).
- ADR-0007 → v1.1 (snapshot content, OQ-023). D-015 Ctrl+C routing (OQ-021). Architecture → v1.1 (minor, aligned).
- Validation: 166/166 contrast pairs, design check ALL OK, requirements and structural validators ALL OK.
