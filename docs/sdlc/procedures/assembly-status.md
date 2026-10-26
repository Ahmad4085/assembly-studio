# /assembly-status: Project status

> **Session protocol:** run `docs/sdlc/agent-protocol.md` §3 steps 1–2 (read-only). Run §6 only
> with `--refresh`. May be run whenever the user asks for status.

## 1. Purpose
Give an accurate snapshot of where the project stands and what is permitted next. It is derived
from the documents and repository, never invented.

## 2. Inputs
`Arguments`: `--refresh` = also correct `PROJECT_STATE.md` when it disagrees with
the documents. Default: read-only.

## 3. Reads
`HANDOFF.md`, `CURRENT_TASK.md`, `PROJECT_STATE.md`, `ROADMAP.md`, `DECISIONS.md`, latest `WORK_LOG.md` entries; header blocks (status, version, *Depends on*) of every document under
`docs/`; `docs/decisions/*`; backlog task statuses; latest test report, defects and QA report;
`git status` / `git log -5 --oneline` if a repository; `dotnet --list-sdks`.

## 4. Workflow
1. Collect each document's status and version. Recompute *Outdated* by comparing *Depends on*
   versions with current upstream versions.
2. Collect approvals (approval log), waivers, task counts by status, failing tests, open defects
   by severity, blockers (including environment), open High-impact OQs.
3. **Consistency check.** Report every disagreement between the shared memory files and the documents
   or repository (e.g. state says "Approved" but the document header says "Draft". `CURRENT_TASK.md`
   says `In progress` with no matching `WORK_LOG.md`/`HANDOFF.md` entry. `HANDOFF.md` Git state differs
   from `git status`). Code, tests and documents are the source of truth (agent protocol §1).
4. Determine the next permitted command from the process contract's gate table.
5. If `--refresh`: correct the factual fields in `PROJECT_STATE.md` and `ROADMAP.md`, append a
   `WORK_LOG.md` entry describing the corrections.

## 5. Outputs
Chat summary: phase table, approvals, outdated docs, tasks, failing tests/defects, blockers, open
High OQs, inconsistencies, next command. `PROJECT_STATE.md` updated only with `--refresh`.

## 6. Validation
Every number in the summary is traceable to a file. Unknown → "unknown (reason)", not an estimate.

## 7. Approval
None. This command never approves or changes gated documents.

## 8. Failure handling
Missing `PROJECT_STATE.md` → report it and suggest `/assembly-init`. Unparseable header → list the file as
inconsistent.

## 9. Exit conditions
Summary delivered.

## 10. Next permitted phase
Whatever the gate table permits, stated as an exact command.
