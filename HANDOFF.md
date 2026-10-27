# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-27 13:56 +0500 by OpenCode (GLM-5.3-Flash Free), after completing TASK-012 |
| Current task | **TASK-013 Arithmetic instructions: `Ready`, approved, not started, no owner** |
| Last completed | TASK-012 Data transfer instructions (Done 2026-10-27 13:55 +0500) |
| Git state | HEAD `fa9dcc5` "docs: update HANDOFF.md after pushing TASK-010/011", **pushed to origin/master**. Uncommitted, ours: TASK-012 source/tests (`src/AssemblyStudio.Engine.I8086/Cpu8086.DataTransfer.cs`, `Cpu8086.cs`, `tests/.../SingleStepOracleTests.cs`, `DataTransferTests.cs`) + shared memory + backlog. Untracked, not ours: `.idea/vcs.xml`, `.opencode/`, `opencode.jsonc` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → 0 warnings, 0 errors (2026-10-27 13:55 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **204/204 passed** (2026-10-27 13:55 +0500); engine test project 146/146 — **all 71 TASK-012 SingleStepTests oracle files pass 100%**.
- NASM tooling and oracle data (323 8086 files + metadata) fetched/verified in M0; committed.

## Changes since the previous handoff
- TASK-012: all data-transfer opcodes implemented in `Cpu8086.DataTransfer.cs` (partial class) with oracle-derived 8086 quirks; dispatch rewritten as a switch expression (`StepResult?`); SingleStepTests oracle harness added to the test project (71 files, 100%); 30 hand tests (PUSH SP, POP SP, segment moves, LAHF/SAHF, PUSHF/POPF reserved bits, XLAT, LEA/LDS/LES, moffs, C6/C7, XCHG, word wrap).
- (Earlier in this session, OpenCode: `/assembly-status`, TASK-010, TASK-011, committed and pushed `23e0fca..fa9dcc5`.)

## Failed tests and known issues
- None failing. Notes: mod=11 forms of 8D/C4/C5 halt as undefined (no oracle cases); other FF forms halt until their tasks.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-015. Core has **no project references**; Engine.I8086 references only Core (Iced arrives with TASK-019).
- Engine API exposes snapshots, not live state (ADR-0002/0003). Decode is pure: halt restores CS:IP, nothing modified (FR-037).
- Undefined flags: deterministic value + reported mask (ADR-0003 §3) — the oracle harness masks via metadata.json. Pinned NASM 3.02 per ADR-0005.
- `Cpu8086` is not thread-safe; the debug-session worker thread is the only caller (ADR-0004 §2).
- 8-bit register encoding: index 0–3 → AL,CL,DL,BL; 4–7 → AH,CH,DH,BH. Sreg field aliases `reg&3`.

## Exact next action
1. Optional: commit the pending TASK-012 changes (user asks or approves per protocol §8).
2. User runs `/assembly-implement TASK-013` (arithmetic instructions — oracle harness already in place; extend the file list and add a `Cpu8086.Arithmetic.cs` partial).
3. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `.opencode/`.
