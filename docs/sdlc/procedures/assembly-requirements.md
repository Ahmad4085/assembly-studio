# /assembly-requirements: Product requirements

> **Session protocol:** run `docs/sdlc/agent-protocol.md` §3 (session start) before step 1 and §6
> (session end) before finishing. **Start this procedure only when the user explicitly asked for it**
> (slash command or a direct request naming it). Never start it on your own initiative.

## 1. Purpose
Produce and maintain the `docs/01-requirements/` requirements set (entry: `PRODUCT_REQUIREMENTS.md`): a testable, traceable definition of
what Assembly Studio must do. Requirements say *what* and *how well*, never *how*. No technology
choices except true constraints.

## 2. Inputs
- `Arguments`: optional focus (e.g. "debugger", "tutorials") or a change request.
- User answers to clarifying questions.

## 3. Reads
`docs/00-project-overview/project-overview.md`, `docs/00-project-overview/sdlc-process.md`,
`PROJECT_STATE.md`, existing `docs/01-requirements/*`,
`docs/sdlc/guides/sdlc-documentation.md`, `docs/templates/requirements.md`.

## 4. Workflow
1. **Gate in:** confirm phase 0 is complete in `PROJECT_STATE.md`. Otherwise stop and suggest `/assembly-init`.
2. **Mode:** new (no file) → create from template; existing → idempotent update (sdlc-documentation §4).
3. **Elicit.** Ask the user a short batch (≤ 8) of the highest-value questions. Always cover:
   target users and skill levels; v1 CPU/assembler/executable format scope; required DOS/BIOS
   services; debugging depth (step, step-over, reverse/history?); tutorials/samples scope;
   product license and distribution model; accessibility expectations; performance expectations.
   Carry OQ-001…OQ-007 from the overview. Resolve or keep each one.
4. **Draft** product goals (PG), personas (PER), user stories (US), functional requirements (FR) with
   priority P0–P3 and status (PROPOSED / NEEDS_RESEARCH / BLOCKED, never APPROVED before the gate),
   Given/When/Then acceptance criteria (AC, split engine-independent vs engine-dependent), NFRs with
   measurable or explicitly PROPOSED thresholds and a verification method
   (performance, reliability, usability, accessibility, offline, security, compatibility,
   maintainability, licensing, installability), constraints, supported-scope statement including an
   explicit **not supported** list, assumptions, open questions.
5. **Domain sanity checks.** Requirements must not conflate assembling with executing, must not
   assume DOS services come from the CPU engine, and must not promise compatibility beyond v1 scope.
   Visualization requirements must state that displayed state comes from the engine.
6. **Self-review** against §6, then set `Awaiting approval` and ask the gate question.

## 5. Outputs
`docs/01-requirements/`: `PRODUCT_REQUIREMENTS.md` (goals, personas, scope, constraints, definitions,
summary, gate), `FUNCTIONAL_REQUIREMENTS.md`, `NON_FUNCTIONAL_REQUIREMENTS.md`, `USER_STORIES.md`,
`ACCEPTANCE_CRITERIA.md`, `REQUIREMENTS_TRACEABILITY.md`, `OPEN_QUESTIONS.md` (questions and
assumptions). The set shares one version and status. Overview OQ table annotated with where each
question moved. Shared memory updated (agent protocol §6).

## 6. Validation
- Every FR has a priority and status. Every NFR has a threshold (FIRM or PROPOSED, with a validation
  plan) and a verification method.
- IDs unique and sequential, no reuse. Every P0–P2 FR belongs to ≥1 user story. Every P0/P1 FR and
  NFR has ≥1 acceptance criterion. Every AC and US reference resolves.
- No vague wording ("fast", "easy", "fully compatible") without a measurable definition.
- The traceability matrix covers every FR and NFR, and test cases are `NOT CREATED` / `NOT RUN` until they exist.
- No implementation decisions disguised as requirements (e.g. "use AvalonEdit") unless recorded
  as a constraint with source.
- High-impact unknowns are OQ rows, not guesses.

## 7. Approval
Gate question: "Approve requirements v<x> (N FR, M NFR) with open questions <list>?" Only an
explicit yes sets `Approved` (version 1.0 on first approval) and logs it. Partial approval is
recorded as such.

## 8. Failure handling
User unavailable or unsure: leave `Draft`, record OQs, report what blocks approval. Contradictory
inputs: record both, raise an OQ, don't pick one silently.

## 9. Exit conditions
Document `Approved`, or `Awaiting approval` with the gate question asked.

## 10. Next permitted phase
`/assembly-research` (only after approval or a recorded waiver).
