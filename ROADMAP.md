# Roadmap

> Phases, gates and dependencies come from `docs/00-project-overview/sdlc-process.md` §1 (authoritative).
> This file tracks **verified progress** and the work items that move the project through them.

## Phase progress
| # | Phase | Depends on (gate in) | Gate out | Status | Work item |
|---|---|---|---|---|---|
| 0 | Initialization | — | Workspace validates | **Complete** (2026-10-26) | SDLC-001, SDLC-002 |
| 1 | Requirements | Phase 0 | User approval | **Approved** (2026-10-26 17:11 +0500, set v1.0) | SDLC-003 |
| 2 | Research | Requirements approved | User approval | Ready to start | SDLC-004 |
| 3 | Architecture (+ ADRs) | Research approved | User approval | Not started | — |
| 4 | UI/UX design | Architecture approved | User approval | Not started | — |
| 5 | Implementation plan | Design approved | User approval (per milestone allowed) | Not started | — |
| 6 | Implementation | Task `Approved` | Task `Done` with evidence | Not started (also blocked by BLK-001) | — |
| 7 | Testing | ≥1 task `Done` | Report recorded | Not started | — |
| 8 | QA | Test report for build | Verdict | Not started | — |
| 9 | Release | QA not `Fail`, no open Critical | User approval | Not started | — |

Phases 6–8 iterate per milestone. Any phase can be re-entered (process contract §4).

## SDLC work items
| ID | Title | Status | Notes |
|---|---|---|---|
| SDLC-001 | Establish documentation-driven SDLC foundation | Done | Claude Code, 2026-10-26 |
| SDLC-002 | Multi-agent support (shared memory, tool-independent core, Claude Code + Antigravity) | Done | Claude Code, 2026-10-26 |
| SDLC-003 | Requirements phase (`/assembly-requirements`) | Done | Claude Code. Set v1.0 approved 2026-10-26 17:11 +0500 (D-006 to D-010) |
| SDLC-004 | Research phase (`/assembly-research`) | Ready | Answers the High open questions before architecture |

## Product milestones
Not defined yet. `/assembly-plan` defines them (expected shape in
`docs/sdlc/procedures/assembly-plan.md`). Proposed milestones are not commitments until approved.
