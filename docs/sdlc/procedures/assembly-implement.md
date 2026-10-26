# /assembly-implement: Implement one approved task

> **Session protocol:** run `docs/sdlc/agent-protocol.md` §3 (session start) before step 1 and §6
> (session end) before finishing. **Start this procedure only when the user explicitly asked for it**
> (slash command or a direct request naming it). Never start it on your own initiative.

## 1. Purpose
Implement a single approved, scoped task with tests, verified by an actual build and test run.

## 2. Inputs
`Arguments`: the task ID (required). If missing, list the eligible tasks (status `Approved`, all
dependencies `Done`) and ask which one. Don't pick one yourself.

## 3. Reads
- `HANDOFF.md`, `CURRENT_TASK.md`, `PROJECT_STATE.md`, `docs/05-planning/backlog.md` (the task block).
- Every requirement, ADR and UI item the task links to, plus the relevant sections of
  `docs/03-architecture/architecture.md` and `docs/04-design/*`.
- Existing code in the areas to be touched (conventions to match).
- `docs/sdlc/guides/dotnet-wpf-engineering.md`. Also `cpu-instruction-testing` for engine/runtime
  tasks and `desktop-ui-quality` for UI tasks.

## 4. Workflow
1. **Eligibility.** The task exists, is `Approved` (no waiver path), dependencies are `Done`, and
   inputs are not `Outdated`. Otherwise stop and explain.
2. **Environment.** `dotnet --list-sdks` shows the SDK required by the ADR. If not, stop: record or
   refresh the blocker and tell the user what to install. Don't install SDKs yourself.
3. **Approach.** State briefly what you'll change, file by file, and which tests prove it. If the task
   turns out to need out-of-scope changes, stop and propose a backlog change instead of expanding scope.
4. Set the task `In progress`.
5. **Implement** within scope, matching conventions. Write tests alongside the code. No fabricated
   execution data. Unsupported behavior raises explicit, typed errors or diagnostics.
6. **Verify.** Run `dotnet build` (affected solution/projects) and `dotnet test` (affected test
   projects, plus the full suite if shared code changed). Capture the commands and summarized output.
7. **Definition of Done.** Tick each item with evidence. Anything unmet → task stays `In progress`
   or `Blocked`, with reasons.
8. **Record.** Update the task status in the backlog and traceability rows, and record new failures
   as `DEF-###` in `docs/07-testing/defects.md`. Then run the session end (agent protocol §6):
   `CURRENT_TASK.md`, a `WORK_LOG.md` entry with the exact build/test commands and results,
   `PROJECT_STATE.md` (latest build/test status), and `HANDOFF.md`.

## 5. Outputs
Source and test files within scope, backlog and traceability updates, shared-memory updates (agent protocol §6).

## 6. Validation
- Build and tests were actually executed in this session. Results are quoted, not paraphrased into "should pass".
- No changes outside the task's declared scope. No new dependency without an approved ADR or research entry.
- Engine/runtime/assembler projects have no WPF references.

## 7. Approval
Task approval is a precondition. Mid-task scope or design changes need user approval before
continuing. Marking `Done` needs no extra approval if the Definition of Done is evidenced.

## 8. Failure handling
Build or test fails and can't be fixed within scope: keep the failure, log it as `Fail` with
output, open a `DEF-###`, set the task `Blocked` or `In progress`. Never weaken or delete a test to
make it pass. Never claim success.

## 9. Exit conditions
Task `Done` with evidence, or `Blocked`/`In progress` with a clear reason.

## 10. Next permitted phase
Report the next eligible task (`/assembly-implement <ID>`). When a milestone's tasks are `Done`,
recommend `/assembly-test`.
