# /assembly-plan: Implementation plan and backlog

> **Session protocol:** run `docs/sdlc/agent-protocol.md` §3 (session start) before step 1 and §6
> (session end) before finishing. **Start this procedure only when the user explicitly asked for it**
> (slash command or a direct request naming it). Never start it on your own initiative.

## 1. Purpose
Produce an ordered, approvable backlog that `/assembly-implement` can execute task by task.
Never writes code.

## 2. Inputs
`Arguments`: optional milestone to (re)plan.

## 3. Reads
Approved requirements, research summary, architecture, ADRs, UI spec and tokens; `PROJECT_STATE.md`;
existing `docs/05-planning/*`; `docs/sdlc/guides/sdlc-documentation.md`; templates
`backlog.md`, `implementation-task.md`, `traceability.md`.

## 4. Workflow
1. **Gate in:** UI spec `Approved` (or waiver). Check every *Depends on* version and report outdated inputs.
2. **Milestones.** Define user-visible milestones. Suggested shape, adjust to requirements:
   M0 solution skeleton + CI-able build + test harness; M1 engine core (decode/execute subset,
   registers, flags) headless with tests; M2 assembler integration + loader; M3 shell UI + editor;
   M4 debugger (step, breakpoints, history) + visualization; M5 DOS services subset + output;
   M6 tutorials/samples; M7 packaging.
3. **Risk first.** Put POCs for high-risk unknowns before the tasks that depend on them.
4. **Tasks.** Each task uses the implementation-task template: goal, in/out scope, requirements,
   ADR/UI links, dependencies, expected files, test requirements (TC IDs), Definition of Done.
   Size: completable and verifiable in one session (roughly ≤ 1 day of work). Split larger ones.
5. **Environment tasks.** Include prerequisites explicitly (e.g. "install .NET SDK per ADR"). The
   user performs them, so mark them as user actions.
6. **Traceability.** Create or update `docs/05-planning/traceability.md` and list coverage gaps.
7. **Risk register** in the backlog.
8. All new tasks `Proposed`. Set backlog `Awaiting approval`. Ask the gate question (whole backlog or
   per milestone).

## 5. Outputs
`docs/05-planning/backlog.md` (milestones, tasks, risks), `docs/05-planning/milestones.md` (only if
the backlog becomes too long, otherwise keep milestones inside the backlog),
`docs/05-planning/traceability.md`, `PROJECT_STATE.md` updated.

## 6. Validation
- Every Must FR/NFR maps to ≥1 task, or is listed as a gap with a reason.
- Every task has ≥1 requirement, test requirements and a Definition of Done.
- Dependency graph is acyclic. No task depends on a later milestone.
- Tasks relying on `[Unverified]` claims depend on the POC that verifies them.

## 7. Approval
On approval, approved tasks move `Proposed` → `Approved`. Record exactly which IDs were approved.
Only `Approved` tasks are eligible for implementation.

## 8. Failure handling
Requirements that can't be planned without an architecture decision: stop and name the missing
ADR. Don't decide it inside the plan.

## 9. Exit conditions
Backlog (or ≥1 milestone) `Approved`.

## 10. Next permitted phase
`/assembly-implement <first eligible TASK-ID>`.
