# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` A6).

| Field | Value |
|---|---|
| Written | 2026-10-28 10:25 +0500 by Claude Code (Opus 5.5), after completing TASK-015 (taken over from OpenCode on the user's request) |
| Current task | **TASK-016 Control transfer and interrupts: `Approved`, not started, no owner** (TASK-060 WPF shell also eligible) |
| Last completed | TASK-015 Multiply, divide, BCD and conversions (Done 2026-10-28 10:25 +0500) |
| Git state | TASK-015 committed as `37e7422` (on the user's request, 2026-10-28), after `ddc5fe5` (SDLC-009); this HANDOFF update is a follow-up docs commit. **Not pushed** (origin/master at `357251e`). Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` (OpenCode's analysis scripts) |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` -> 0 warnings, 0 errors (2026-10-28, this session).
- `dotnet test --solution AssemblyStudio.slnx -c Release` -> **425/425 passed** (2026-10-28, this session); **229 SingleStepTests oracle files at 100%** (71 TASK-012 + 84 TASK-013 + 58 TASK-014 + 16 TASK-015).

## Changes since the previous handoff
- SDLC-009 (OpenCode support, D-016) committed as `ddc5fe5`. `.opencode/` and `opencode.jsonc` are now tracked project files.
- TASK-015 in new `Cpu8086.MulDiv.cs`: MUL/IMUL/DIV/IDIV (F6/F7 /4-/7), AAA/AAS/DAA/DAS, AAM/AAD, CBW/CWD.
- Division follows the 8086 CORD microcode loop (`Cord`), so the FLAGS pushed by a divide error match the real chip. Divide error (quotient overflow, divisor 0, AAM 0): `IInterruptRouter.Intercept(0, state-at-the-instruction)`; with no handler, real vectoring that pushes the *next* instruction's IP (8086).
- Oracle-verified 8086 quirks: IDIV rejects -128/-32768; REP prefix negates the IDIV quotient; AAA/AAS adjust AL and AH separately; DAA/DAS high-digit threshold is 9Fh when AF was set on entry; DAS low-digit borrow does not set CF.
- INT vectoring factored into `VectorInterrupt(vector, flagsToPush, returnIp)`; TASK-016's INT 3/INTO/IRET should reuse it.
- TASK-015 steps report undefined-flag masks (`StepResult.UndefinedFlags`); TASK-012..014 still report `None`.

## Failed tests and known issues
- None failing. F6/F7 /1 (undocumented TEST alias) and FF arms 2/3/4/5/7 halt until their tasks. IMUL with a REP prefix is not negated (no oracle evidence; the oracle has no such cases).

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-016. Core has **no project references**; Engine.I8086 references only Core.
- Engine API exposes snapshots, not live state (ADR-0002/0003). Decode is pure: halt restores CS:IP, nothing modified (FR-037).
- Undefined flags: deterministic value + reported mask (ADR-0003 A3); the oracle harness masks via metadata.json.
- `Cpu8086` is not thread-safe (ADR-0004 A2). Runtime error policy for INT 0 with the default vector belongs to TASK-032.
- Hand-test helper pattern: `NewCpu` needs two overloads (see existing test files).

## Exact next action
1. Optional: push `ddc5fe5`, `37e7422` and the HANDOFF commit (push only on user request).
2. User runs `/assembly-implement TASK-016` (control transfer and interrupts), or `/assembly-implement TASK-060` for the first visible WPF window.
3. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/` and `.mcp.json`.
