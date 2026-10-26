# /assembly-qa: Quality assurance

> **Session protocol:** run `docs/sdlc/agent-protocol.md` §3 (session start) before step 1 and §6
> (session end) before finishing. **Start this procedure only when the user explicitly asked for it**
> (slash command or a direct request naming it). Never start it on your own initiative.

## 1. Purpose
Answer "does the product do what the approved requirements say, well enough to ship this scope?"
with evidence, and issue a verdict.

## 2. Inputs
`Arguments`: scope (milestone or release candidate). Default: everything `Done`.

## 3. Reads
Approved requirements, UI spec, traceability, latest test report and defects, task log, status
file, `docs/sdlc/guides/desktop-ui-quality.md` (checklist), `docs/templates/qa-report.md`.

## 4. Workflow
1. **Gate in:** a test report exists for the current build and it isn't older than the last code
   change. Otherwise ask to run `/assembly-test` first.
2. **Requirement verification.** For each in-scope FR/NFR, check every acceptance criterion
   against evidence: an executed test, or a manual check performed now (state the steps and what
   was observed). If the app can't be launched or driven in this environment, mark the item
   `Not verified` and list the manual steps for the user.
3. **Quality checks** (desktop-ui-quality checklist): UI functionality, usability, accessibility
   (keyboard, UI Automation names, contrast, high contrast, scaling), offline behavior (no network
   calls on core paths, check code and run with network disabled where possible), reliability
   (runaway programs, faults, large files), Windows 10/11 and DPI, **visualization integrity** (displayed
   values equal engine state, no mock data in production code paths, search for fake/sample/demo
   data sources), and documented known limitations.
4. **Defects.** Log new findings as `DEF-###` with severity.
5. **Verdict.** `Fail` if any Must acceptance criterion fails or any Critical defect is open.
   `Pass with conditions` lists each condition. `Pass` only with full evidence.
6. Write `docs/08-qa/qa-report-YYYY-MM-DD-<n>.md`. Update traceability QA status and `PROJECT_STATE.md`.

## 5. Outputs
QA report, defect updates, traceability and status updates.

## 6. Validation
`Not verified` is never counted as verified. The verdict rule is applied mechanically. Every
"Verified" has a concrete evidence reference.

## 7. Approval
The verdict is evidence-based, not approved. The user may accept specific High/Medium defects as
release conditions. Record each acceptance in the status approval log. Critical defects cannot be waived for release.

## 8. Failure handling
Missing evidence → `Not verified`, with the reason and steps for the user. Environment can't run
the UI → state it and fall back to view-model and integration evidence plus a manual checklist.

## 9. Exit conditions
QA report with verdict recorded.

## 10. Next permitted phase
`Fail` → fixes via `/assembly-plan` (new tasks) and `/assembly-implement`. `Pass` or
`Pass with conditions` → `/assembly-release`.
