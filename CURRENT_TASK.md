# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## SDLC-006: Architecture phase

| Field | Value |
|---|---|
| Status | **Ready** (not started) |
| Owner | — (unassigned. The next agent sets itself as owner when starting) |
| Started | — |
| Procedure | `docs/sdlc/procedures/assembly-architecture.md` (invoke with `/assembly-architecture`) |
| Depends on | Research v1.0 approved ✔. Requirements v2.0 approved ✔. POC-001 ✔ (feasible). POC-002 ✔ (sufficient with limitations) |

**Objective:** produce `docs/03-architecture/architecture.md` and the ADRs in `docs/decisions/` (at least: .NET/UI stack, solution structure,
execution engine, DOS services approach, assembler integration and source mapping, editor component, test framework (incl. MTP mode),
packaging), then obtain user approval.

**Key inputs:** `docs/02-research/research-summary.md` (R-*), `poc-001.md` §4 and `poc-002.md` (results and limitations), D-001 to D-013,
open questions marked "ADR pending" (OQ-001, OQ-005, OQ-011, OQ-013, OQ-015, OQ-020, OQ-022).

**Acceptance criteria:** the procedure's validation (`docs/sdlc/procedures/assembly-architecture.md` §6) and exit conditions
(architecture + blocking ADRs `Approved` or explicitly deferred).

**Validation performed:** none yet.
**Remaining work:** all of it.

---

## Last completed: POC-002, NASM listing + map source mapping (Done 2026-10-26 18:42 +0500, Claude Code). Verdict: sufficient with limitations
- `dotnet build Poc.Mapping.slnx -c Release` → 0 warnings, 0 errors. `dotnet test --solution Poc.Mapping.slnx -c Release` → 24/24 passed.
- Three independent verifiers (image bytes, map symbols, ndisasm boundaries) pass on 5 fixtures. The mutation check fails 20/24 as expected (restored).
- Macros map to the invoking line, includes to their file, `times`/`align` full length. `cpu` bypass detected via `nasm -E`. Details: `docs/02-research/poc-002.md`.
