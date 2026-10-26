# /assembly-research: Technical research

> **Session protocol:** run `docs/sdlc/agent-protocol.md` §3 (session start) before step 1 and §6
> (session end) before finishing. **Start this procedure only when the user explicitly asked for it**
> (slash command or a direct request naming it). Never start it on your own initiative.

## 1. Purpose
Gather verified evidence for the decisions architecture must make. Research **recommends**.
Decisions are made as ADRs in `/assembly-architecture`.

## 2. Inputs
`Arguments`: topic(s). Default `all`, in this order: platform (.NET/WPF/Windows support) →
execution engine → DOS/BIOS services → assembler → editor component → testing → packaging.

## 3. Reads
Approved `docs/01-requirements/` set (especially `OPEN_QUESTIONS.md`), `PROJECT_STATE.md`, existing `docs/02-research/*`,
`docs/sdlc/guides/assembly-emulator-research.md` (domain guidance and candidate list),
`docs/sdlc/guides/sdlc-documentation.md`, `docs/templates/research-evaluation.md`.

## 4. Workflow
1. **Gate in:** requirements `Approved` (or waiver). If outdated vs. its *Depends on*, report it.
2. **Derive criteria** from FR/NFR/CON and OQ IDs (license gate, offline gate, Windows/.NET gate,
   architecture coverage, debug hooks, determinism, integration effort, maintenance).
3. **Collect evidence** per option from primary sources: official docs, repository README/LICENSE,
   release pages, package registries. Use web access when available. Record URL and access date.
   If sources can't be reached, label the item `[Unverified]`. Never fill gaps from memory as fact.
4. **Licensing.** Record the exact license (SPDX) and its distribution obligations for each option,
   and check compatibility with the product license decision (or flag OQ-007 as blocking).
5. **Compare** in a matrix and recommend. List rejected options with reasons.
6. **Propose POCs** for high-risk unknowns (e.g. "engine can single-step and expose flags after
   each instruction", "NASM listing maps addresses to source lines"). A POC runs only with
   explicit user approval, under `poc/`, with results recorded as `[Measured]`.
7. Write `docs/02-research/research-summary.md` linking topic files, recommendations and open items.
   Set `Awaiting approval` and ask the gate question.

## 5. Outputs
`docs/02-research/<topic>.md` per topic, `docs/02-research/research-summary.md`, optional
`docs/02-research/poc-<id>.md`. `PROJECT_STATE.md` updated.

## 6. Validation
- Every license, version, platform-support and capability claim carries an evidence label.
- No benchmark numbers without `[Measured]`.
- Each recommendation cites the requirement IDs it satisfies and the gaps it leaves
  (e.g. "engine X provides CPU only; DOS INT 21h must be implemented by us").
- `[Unverified]` items are listed in the summary with a resolution plan.

## 7. Approval
Gate question: "Approve research findings and recommendations (list), with unverified items (list)?"
Approval accepts the recommendations as input to architecture. It does not approve ADRs.

## 8. Failure handling
No network: research from local sources only, mark everything else `[Unverified]`, tell the user
which claims need verification. Conflicting sources: record both and prefer the primary source.

## 9. Exit conditions
Summary `Approved`, or `Awaiting approval` with the gate question asked.

## 10. Next permitted phase
`/assembly-architecture`.
