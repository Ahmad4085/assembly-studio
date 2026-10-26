# /assembly-architecture: Architecture and decisions

> **Session protocol:** run `docs/sdlc/agent-protocol.md` §3 (session start) before step 1 and §6
> (session end) before finishing. **Start this procedure only when the user explicitly asked for it**
> (slash command or a direct request naming it). Never start it on your own initiative.

## 1. Purpose
Turn approved requirements and research into a modular architecture and a set of approved ADRs.
The engine must be replaceable without a UI rewrite.

## 2. Inputs
`Arguments`: optional area to (re)work, or a single ADR topic.

## 3. Reads
Approved requirements and research, `docs/03-architecture/*`, `docs/decisions/*`, `PROJECT_STATE.md`,
`docs/sdlc/guides/dotnet-wpf-engineering.md` (solution structure, MVVM),
`docs/sdlc/guides/assembly-emulator-research.md` (domain layers),
`docs/sdlc/guides/sdlc-documentation.md`, templates `architecture.md` and `adr.md`.

## 4. Workflow
1. **Gate in:** research summary `Approved` (or waiver). Check *Depends on* versions.
2. **Alternatives.** For each structural question, evaluate ≥2 options (e.g. engine in-process vs.
   out-of-process; custom engine vs. library; MVVM toolkit choice; plugin vs. compile-time CPU
   modules). Tie each to NFRs.
3. **Components.** Fill the component table: responsibility, explicit *must not do*, public
   interface (C# interface names), target project. Dependencies point toward abstractions. UI never
   references a concrete engine.
4. **Flows and state.** Edit → assemble → load → run/step/breakpoint → inspect. Engine runs off the
   UI thread. State snapshots are immutable and come from the engine. Define cancellation and
   runaway-program limits.
5. **Cross-cutting.** Error handling, security boundaries (external assembler process, file paths,
   untrusted user programs confined to emulated machine), performance budgets mapped to NFRs,
   offline guarantees, logging, settings, extensibility to x86/x86-64.
6. **Testing strategy** aligned with `cpu-instruction-testing`.
7. **ADRs.** One file per decision in `docs/decisions/ADR-####-<slug>.md` (next free number).
   Minimum expected: target .NET/UI stack, solution structure, execution engine, DOS/BIOS services
   approach, assembler integration, editor component, test framework, packaging approach (may be
   deferred to an explicit "Deferred" ADR status with a deadline phase).
8. Set the architecture document and ADRs to `Awaiting approval`. Ask the gate question.

## 5. Outputs
`docs/03-architecture/architecture.md`, `docs/decisions/ADR-*.md`, `PROJECT_STATE.md` updated.

## 6. Validation
- Every component lists requirements it serves. Every Must FR/NFR is covered by ≥1 component.
- Every ADR cites requirements and research. Licensing impact stated for each dependency.
- No circular dependencies. Engine, runtime and assembler projects have no WPF references.
- Every decision that rests on an `[Unverified]` claim names the POC or check that validates it.

## 7. Approval
Gate question lists the architecture version and each ADR. The user may approve ADRs individually.
Rejected ADRs stay with status `Rejected`.

## 8. Failure handling
A requirement cannot be met by any viable option: propose a requirement change (back to
`/assembly-requirements`) instead of quietly weakening it. Missing research: list it and offer to run
`/assembly-research <topic>`.

## 9. Exit conditions
Architecture `Approved` and all blocking ADRs `Approved` (or explicitly deferred).

## 10. Next permitted phase
`/assembly-design`.
