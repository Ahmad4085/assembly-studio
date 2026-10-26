# /assembly-test: Testing

> **Session protocol:** run `docs/sdlc/agent-protocol.md` §3 (session start) before step 1 and §6
> (session end) before finishing. **Start this procedure only when the user explicitly asked for it**
> (slash command or a direct request naming it). Never start it on your own initiative.

## 1. Purpose
Prove behavior with deterministic, executed tests. Produce a test report that reflects exactly what ran.

## 2. Inputs
`Arguments`: scope (milestone, component, test IDs) or `rerun DEF-###` to verify a fix.
Default: everything implemented so far.

## 3. Reads
`PROJECT_STATE.md`, backlog (tasks `Done`), traceability, `docs/07-testing/*` (plan, previous reports,
defects), requirements and architecture testing strategy, `docs/sdlc/guides/cpu-instruction-testing.md`,
templates `test-plan.md` and `test-report.md`.

## 4. Workflow
1. **Gate in:** ≥1 task `Done`. Required SDK installed (else blocker, stop).
2. **Plan.** Create or update `docs/07-testing/test-plan.md`: inventory TC IDs by level, mapped to
   FR/NFR/TASK. Identify untested `Done` tasks and Must requirements.
3. **Fill gaps.** Add missing deterministic tests for implemented behavior only, following
   `cpu-instruction-testing`. New tests for unimplemented features are marked `Skip` with a reason
   and never counted as passed. Adding tests here is in scope. Changing production code is not
   (that goes through `/assembly-implement`).
4. **Execute** the suite: `dotnet test` with a results logger (e.g. `--logger trx`). Record the
   exact commands, environment (`dotnet --version`, OS, assembler version) and build identity.
5. **Analyze.** Compare with the previous report: new failures = regressions. For each failure,
   open or update a `DEF-###` in `docs/07-testing/defects.md`. For `rerun DEF-###`: a passing
   re-run moves the defect `Fixed (unverified)` → `Verified`.
6. **Report** `docs/07-testing/test-report-YYYY-MM-DD-<n>.md`. Update traceability "Latest
   result" and `PROJECT_STATE.md` (failed tests listed until verified).

## 5. Outputs
Test code in `tests/`, `test-plan.md`, new test report, `defects.md`, traceability and status updates.

## 6. Validation
- Counts in the report equal the runner's output.
- Every failure has a defect. No `Pass` for tests that didn't run.
- Tests are deterministic: no wall-clock, randomness without fixed seed, network or machine-specific paths.

## 7. Approval
None to run tests. Deleting or skipping an existing test needs user approval and a recorded reason.

## 8. Failure handling
Build fails: report `Blocked` with build output, run nothing else, open a defect. Flaky test:
record it as a defect. Don't retry until green.

## 9. Exit conditions
Report written with actual results.

## 10. Next permitted phase
Failures in scope → `/assembly-implement` for the fix task (backlog may need a new approved task).
Otherwise → `/assembly-qa`.
