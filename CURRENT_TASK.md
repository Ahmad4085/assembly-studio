# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## POC-002: NASM listing + map source mapping

| Field | Value |
|---|---|
| Status | **Ready** (approved, not started) |
| Owner | — (unassigned. The next agent sets itself as owner when starting) |
| Started | — |
| Definition | `docs/02-research/poc-002.md` (goal, scope, exit criteria) |
| Procedure | `docs/sdlc/procedures/assembly-implement.md` (invoke with `/assembly-implement POC-002`) |
| Depends on | D-013 approval ✔. .NET SDK 10.0.403 ✔. NASM 3.02 installed locally ✔ |

**Objective:** prove that NASM's listing + map give an exact source line ↔ address table for single- and multi-section programs, `times`/`align`,
macros and `%include`; parse diagnostics; propose `cpu`-directive bypass detection. Throwaway code in `poc/POC-002-source-mapping/`.

**Acceptance criteria:** the exit criteria in `docs/02-research/poc-002.md`, recorded there as `[Measured]`.

**Validation performed:** none yet.
**Remaining work:** all of it. Reuse POC-001's setup lessons: `global.json` MTP mode for `dotnet test`, and analyzers with warnings as errors.

---

## Last completed: POC-001, Custom C# 8086 core feasibility (Done 2026-10-26 18:33 +0500, Claude Code). Verdict: feasible
- `dotnet build Poc.Cpu.slnx -c Release` → 0 warnings, 0 errors. `dotnet test --solution Poc.Cpu.slnx -c Release` → 94/94 passed.
- SingleStepTests 8086: 178,000/178,000 cases passed (89 files). The mutation check (PF inverted) fails 31/94 as expected, and the code was restored.
- Step + snapshot mean 0.091 µs. Throughput 47.7–49.4 M instr/s (NFR-003 target ≥ 1 M/s). Full results: `docs/02-research/poc-001.md`.
