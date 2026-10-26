# Roadmap

> Phases, gates and dependencies come from `docs/00-project-overview/sdlc-process.md` §1 (authoritative).
> This file tracks **verified progress** and the work items that move the project through them.

## Phase progress
| # | Phase | Depends on (gate in) | Gate out | Status | Work item |
|---|---|---|---|---|---|
| 0 | Initialization | — | Workspace validates | **Complete** (2026-10-26) | SDLC-001, SDLC-002 |
| 1 | Requirements | Phase 0 | User approval | **Approved** (v1.0 2026-10-26 17:11 +0500; v2.0 2026-10-26 18:18 +0500) | SDLC-003, SDLC-005 |
| 2 | Research | Requirements approved | User approval | **Approved** (2026-10-26 17:59 +0500, set v1.0) | SDLC-004 |
| 3 | Architecture (+ ADRs) | Research approved | User approval | **Approved** (2026-10-26 18:54 +0500, v1.0 + 9 ADRs) | SDLC-006 |
| 4 | UI/UX design | Architecture approved | User approval | **Approved** (2026-10-26 19:03 +0500, spec + tokens v1.0) | SDLC-007 |
| 5 | Implementation plan | Design approved | User approval (per milestone allowed) | Ready to start | SDLC-008 |
| 6 | Implementation | Task `Approved` | Task `Done` with evidence | Not started (BLK-001 resolved) | — |
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
| SDLC-004 | Research phase (`/assembly-research`) | Done | Claude Code. Set v1.0 approved 2026-10-26 17:59 +0500. D-011 to D-013 |
| SDLC-005 | Requirements change: apply D-011 (Windows support) and D-012 (AH=00h) to CON-001, NFR-025, FR-039 (`/assembly-requirements`) | Done | Claude Code. Set v2.0 approved 2026-10-26 18:18 +0500 |
| POC-001 | Custom C# 8086 core feasibility (`/assembly-implement POC-001`) | Done | Claude Code 2026-10-26 18:33 +0500. **Feasible** (178,000/178,000 cases, ~48 M instr/s) |
| POC-002 | NASM listing + map source mapping (`/assembly-implement POC-002`) | Done | Claude Code 2026-10-26 18:42 +0500. **Sufficient with limitations** (24/24 tests, 3 independent verifiers) |
| SDLC-006 | Architecture phase (`/assembly-architecture`) | Done | Claude Code. Architecture v1.0 + ADR-0001..0009 approved 2026-10-26 18:54 +0500. D-014 |
| SDLC-007 | UI/UX design phase (`/assembly-design`) | Done | Claude Code. Spec + tokens v1.0, ADR-0007 v1.1, D-015 approved 2026-10-26 19:03 +0500 |
| SDLC-008 | Implementation plan (`/assembly-plan`) | Ready | Milestones, backlog, risk register. Extends the traceability matrix with tasks |

## Product milestones
Not defined yet. `/assembly-plan` defines them (expected shape in
`docs/sdlc/procedures/assembly-plan.md`). Proposed milestones are not commitments until approved.
