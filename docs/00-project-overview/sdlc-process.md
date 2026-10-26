# SDLC Process Contract

| Field | Value |
|---|---|
| Document | SDLC process contract |
| Status | Approved (process baseline, established at initialization) |
| Version | 1.2 |
| Last updated | 2026-10-26 |
| Owner | Project lead (user) |

This file is the single source of truth for phases, gates, document statuses and approvals.
Command procedures (`docs/sdlc/procedures/assembly-*.md`) implement this contract. Writing
conventions (headers, IDs, evidence labels, update procedure) live in
`docs/sdlc/guides/sdlc-documentation.md`. Sessions, shared memory files, handoff, Git and recovery
live in `docs/sdlc/agent-protocol.md`. The contract applies equally to every agent tool.

## 1. Phases and gates

| # | Phase | Command | Primary outputs | Gate in (prerequisite) | Gate out |
|---|---|---|---|---|---|
| 0 | Initialization | `/assembly-init` | `AGENTS.md`, tool entry points, this file, overview, shared memory files, templates | — | Workspace validates |
| 1 | Requirements | `/assembly-requirements` | `docs/01-requirements/` requirements set (entry: `PRODUCT_REQUIREMENTS.md`) | Phase 0 complete | **User approval** |
| 2 | Research | `/assembly-research` | `docs/02-research/*.md` | Requirements approved | **User approval** of findings and recommendations |
| 3 | Architecture | `/assembly-architecture` | `docs/03-architecture/architecture.md`, `docs/decisions/ADR-*.md` | Research approved | **User approval** (document and each ADR) |
| 4 | UI/UX design | `/assembly-design` | `docs/04-design/ui-spec.md`, `docs/04-design/design-tokens.md` | Architecture approved | **User approval** |
| 5 | Implementation plan | `/assembly-plan` | `docs/05-planning/backlog.md`, `milestones.md`. Extends `docs/01-requirements/REQUIREMENTS_TRACEABILITY.md` | Design approved | **User approval** of backlog (per milestone allowed) |
| 6 | Implementation | `/assembly-implement` | Code, tests, `WORK_LOG.md` entries | Task status `Approved` | Task `Done` (Definition of Done met, tests executed) |
| 7 | Testing | `/assembly-test` | `docs/07-testing/test-plan.md`, `test-report-*.md` | At least one task `Done` | Report recorded (no approval needed) |
| 8 | QA | `/assembly-qa` | `docs/08-qa/qa-report-*.md` | Test report for the build under QA | **Verdict**: Pass / Pass with conditions / Fail |
| 9 | Release | `/assembly-release` | `docs/09-release/*` | QA verdict not `Fail`, no open Critical defects | **User approval** to package/tag/publish |

Phases 6–8 iterate per milestone. Any phase can be re-entered; re-entry follows §4.

**Proof-of-concept exception:** a `POC-###` item that is in an approved plan, or that the user
explicitly approves during Research or Architecture, may produce throwaway code under `poc/`.
POC code is never promoted to `src/` except through a backlog task.

## 2. Status values
Every gated document carries the header block from the `sdlc-documentation` skill with one of:

- `Draft`: being written. Not usable as a prerequisite.
- `Awaiting approval`: complete, and the gate question has been put to the user.
- `Approved`: the user approved this version. Usable as a prerequisite.
- `Outdated`: an upstream document it depends on changed substantively since approval.
  Re-run its command to revalidate before downstream work relies on it.
- `Superseded`: replaced (mainly ADRs). Keep the file and link to the replacement.

Task status (backlog): `Proposed` → `Approved` → `In progress` → `Done` | `Blocked` | `Dropped`.
Defect status: `Open` → `Fixed (unverified)` → `Verified` | `Won't fix (approved)`.
Test result: `Pass` | `Fail` | `Blocked` | `Not run`. Never `Pass` without an executed run.

## 3. Approval protocol
1. The command finishes the artifact, validates it and sets status `Awaiting approval`.
2. It asks one explicit gate question naming what is approved (document and version), the
   open high-impact questions, and the next phase that approval unlocks.
3. Only an explicit affirmative user reply counts (e.g. "approved", "approve requirements v1.0").
   Silence, "looks good so far", or Claude's own judgment never count.
4. On approval: set status `Approved`, add a row to the Approval log in
   `PROJECT_STATE.md` (date, artifact, version, approver = user, scope/notes), and update
   `DECISIONS.md` for approved ADRs.
5. Partial approval is allowed (e.g. Milestone M1 tasks only). Record exactly what was approved.
6. Change requests in the reply: apply them, bump the version and ask again.

## 4. Changes after approval
- **Minor** (typo, formatting, clarification, broken link, no change in meaning): edit freely,
  add a changelog line. No re-approval.
- **Substantive** (adds, removes or alters a requirement, decision, acceptance criterion, scope
  or task): bump the version, set `Awaiting approval`, re-ask the gate question, and mark every
  document that depends on it `Outdated` in `PROJECT_STATE.md`.
- Downstream commands compare the *Depends on* versions in their header against the current
  upstream versions. A mismatch means `Outdated`: report it, don't proceed silently.

## 5. Bypass rules
- Commands stop and report missing or unapproved prerequisites. The user may explicitly
  authorize proceeding at risk. Record that as a waiver in `PROJECT_STATE.md` (`WVR-###`: scope,
  reason, date). The resulting document's status is `Draft (waiver WVR-###)` until the
  prerequisite is approved and the document is revalidated.
- Open questions with Impact = High do not block a gate automatically, but they must appear in the
  gate question and in the approved document.
- `/assembly-implement` has no waiver path: a task must be `Approved` before it is implemented.

## 6. Evidence rules
- Facts about third-party software (license, API, platform support, maintenance activity) need a
  source (URL or file path) and an access date, or they are labeled `Unverified`.
- Build and test results are recorded only from commands executed in the session, with the
  command line, date, environment and outcome.
- The shared memory files report only what the code, documents and repository show.

## 7. Document map

| Folder | Holds | Written by |
|---|---|---|
| Repository root | `AGENTS.md`, `PROJECT_STATE.md`, `CURRENT_TASK.md`, `HANDOFF.md`, `WORK_LOG.md`, `ROADMAP.md`, `DECISIONS.md` | Every procedure (agent protocol §6) |
| `docs/sdlc/` | Tool-independent procedures, guides, agent protocol | `/assembly-init` (changes need approval) |
| `docs/00-project-overview/` | Overview, this contract | `/assembly-init` |
| `docs/01-requirements/` | Requirements set: product requirements, FR, NFR, user stories, acceptance criteria, traceability matrix (single, extended by later phases), open questions and assumptions | `/assembly-requirements` (matrix: later phases too) |
| `docs/02-research/` | Option evaluations, research summary, POC results | `/assembly-research` |
| `docs/03-architecture/` | Architecture document | `/assembly-architecture` |
| `docs/decisions/` | ADRs (`ADR-####-slug.md`) | `/assembly-architecture` (others may propose) |
| `docs/04-design/` | UI spec, design tokens | `/assembly-design` |
| `docs/05-planning/` | Milestones, backlog, risk register | `/assembly-plan` |
| `docs/06-implementation/` | Implementation notes (only when a task needs them) | `/assembly-implement` |
| `docs/07-testing/` | Test plan, test reports, defect log | `/assembly-test` |
| `docs/08-qa/` | QA reports | `/assembly-qa` |
| `docs/09-release/` | Release checklist and report, notes, third-party notices, install guide | `/assembly-release` |
| `docs/templates/` | Templates for all of the above | `/assembly-init` |

Folders are created when their first real document is written. No empty placeholders.

## Changelog
- 2026-10-26 v1.0: Process baseline established at initialization.
- 2026-10-26 v1.1: Minor. Paths updated for the tool-independent core (`docs/sdlc/`) and root shared
  memory files (`PROJECT_STATE.md` replaces `docs/status/project-status.md`, `WORK_LOG.md` replaces the
  per-task log). Gates and rules unchanged.
- 2026-10-26 v1.2: Minor. Requirements phase output is the `docs/01-requirements/` document set, and
  `REQUIREMENTS_TRACEABILITY.md` is the single traceability matrix (D-010, proposed with the requirements gate).
