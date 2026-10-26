# SDLC documentation conventions

The process rules (phases, gates, approval) are in `docs/00-project-overview/sdlc-process.md`.
This guide covers **how to write and update** the documents.

## 1. Header block (every gated document)
Start every document created from `docs/templates/` with:

```markdown
| Field | Value |
|---|---|
| Document | <name> |
| Status | Draft / Awaiting approval / Approved / Outdated / Superseded |
| Version | <major.minor> |
| Last updated | YYYY-MM-DD |
| Depends on | <path> v<version>, ... (or "—") |
```

Versioning: `0.x` while draft. `1.0` at first approval. Minor bump (`1.1`) for minor changes
and major bump (`2.0`) for substantive changes (see the process contract §4).
Every document ends with a `## Changelog` section: `- YYYY-MM-DD v<version>: <what changed>`.

## 2. Stable IDs
| Prefix | Meaning | Format |
|---|---|---|
| FR / NFR | Functional / non-functional requirement | `FR-001` |
| PG / PER | Product goal / persona | `PG-1`, `PER-1` |
| US | User story | `US-001` |
| AC | Acceptance criterion (Given/When/Then) | `AC-001` |
| ASM / OQ / CON | Assumption / open question / constraint | `ASM-001` |
| RES | Research option evaluated | `RES-001` |
| ADR | Architecture decision record | `ADR-0001` |
| UI | UI specification item (screen, panel, component, state) | `UI-001` |
| M | Milestone | `M1` |
| TASK / POC | Backlog task / proof-of-concept | `TASK-001` |
| TC | Test case or test group | `TC-001` |
| DEF / RISK / WVR / BLK | Defect / risk / waiver / blocker | `DEF-001` |

Rules: assign the next free number by scanning the existing document(s). Never renumber, reuse or
silently delete. Retire with `~~FR-012~~ Retired YYYY-MM-DD: <reason>` and keep the row.

## 3. Evidence labels
Mark every non-obvious claim about third-party software, standards or behavior:
- `[Verified: <URL or path>, accessed YYYY-MM-DD]`: you read the source this session.
- `[Measured: <command>, YYYY-MM-DD]`: you executed it this session.
- `[Unverified]`: believed but not checked. It must not drive an approved decision alone.
- `[Hypothesis]`: a proposal to be tested (usually by a POC).
Never invent license terms, API capabilities, version numbers, benchmarks or maintenance activity.

## 4. Idempotent update procedure
When a command runs and its output document already exists:
1. Read it fully. Note its status and version.
2. Compare with current inputs (upstream versions in *Depends on*).
3. Edit only the sections that need changing. Preserve user-written content, IDs and history.
4. Classify the change: none, minor or substantive (process §4), and set version and status accordingly.
5. Add a changelog line. If nothing changed, say so and leave the file untouched.
Never recreate a document from its template when it already exists.

## 5. Writing style
- Testable statements: "shall" for requirements; measurable thresholds for NFRs.
- One idea per row or bullet. Tables for registries (requirements, tasks, tests).
- Separate *facts*, *decisions*, *assumptions* and *open questions*. Never blend them in prose.
- Link with relative paths and IDs, e.g. `FR-004` → `../01-requirements/FUNCTIONAL_REQUIREMENTS.md`.
- No placeholder text in approved documents ("TBD" only inside an OQ row).

## 6. Traceability
`docs/01-requirements/REQUIREMENTS_TRACEABILITY.md` is the single matrix: `PG` → `US` → `FR/NFR` → `AC` →
`ADR` → `UI` → `TASK` → `TC` → latest result → QA status. Columns are added by the phase that creates them.
Each command that creates or changes IDs updates the rows it touches (create the file from the
template if it is missing and phase ≥ 5). Commands earlier than planning record their links inside
their own documents (e.g. ADR "Requirements addressed").

## 7. Shared memory update (end of every command)
Follow `docs/sdlc/agent-protocol.md` §6: `CURRENT_TASK.md`, `WORK_LOG.md` (append), `PROJECT_STATE.md`
(phase, approvals only on explicit approval, waivers, outdated documents, blockers, failed tests,
build/test status, next action), `ROADMAP.md`/`DECISIONS.md` when they change, and `HANDOFF.md`.
Record only what the code, documents and repository show.
