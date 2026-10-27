# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` A6).

| Field | Value |
|---|---|
| Written | 2026-10-27 14:59 +0500 by OpenCode (GLM-5.3-Flash Free), after completing TASK-013 |
| Current task | **TASK-014 Logic, shift and rotate instructions: `Ready`, approved, not started, no owner** |
| Last completed | TASK-013 Arithmetic instructions (Done 2026-10-27 14:59 +0500) |
| Git state | HEAD `ccd2595` "TASK-013: Arithmetic instructions (ADD/ADC/SUB/SBB/CMP, INC/DEC, NEG) with exact 8086 flags" + docs commit `fa25969` "docs: shared memory for TASK-013", **not yet pushed** (push only on user request). Untracked, not ours: `.idea/vcs.xml`, `.opencode/`, `opencode.jsonc` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` -> 0 warnings, 0 errors (2026-10-27 14:59 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` -> **308/308 passed** (2026-10-27 14:59 +0500); engine test project 249/249 - **all 155 in-scope SingleStepTests oracle files pass 100%** (71 TASK-012 data transfer + 84 TASK-013 arithmetic).

## Changes since the previous handoff
- TASK-013: ADD/ADC/SUB/SBB/CMP in all forms (rm/r bases 00/10/18/28/38 arms 0-3, acc-imm arms 4/5, immediate groups 80/82/81/83 with reg-field operation select and 83 sign-extended imm8), INC/DEC r16 (40-4F) and rm (FE/FF arms 0/1) leaving CF untouched, NEG (F6/F7 arm 3). All flags exact per the Intel 8086 manual; OF is width-aware (bit 7 for 8-bit ops).
- FF group restructured: `ExecFfGroup` dispatches arms 0/1 (INC/DEC rm16) and arm 6 (PUSH, TASK-012); FE group handler `ExecFeGroup`; `ExecPushRm16` removed. `SetFlag` helper added in `Cpu8086.Arithmetic.cs`.
- Oracle harness extended to the 84 TASK-013 files; AC-025 hand test added verbatim; 20 hand tests in new `ArithmeticTests.cs`.
- Two oracle-exposed bugs fixed during implementation: CMP acc-imm wrote the discarded result into AL/AX; 8-bit INC/DEC flag math used an unmasked 16-bit intermediate.

## Failed tests and known issues
- None failing. Notes: F6/F7 arms other than /3 halt unsupported until TASK-014; FF arms 2/3/4/5/7 halt until their tasks; 80.1/80.4/80.6 (OR/AND/XOR by immediate) are implemented and oracle-verified in TASK-013 scope (their rm/r families are TASK-014).

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-015. Core has **no project references**; Engine.I8086 references only Core (Iced arrives with TASK-019).
- Engine API exposes snapshots, not live state (ADR-0002/0003). Decode is pure: halt restores CS:IP, nothing modified (FR-037).
- Undefined flags: deterministic value + reported mask (ADR-0003 A3) - the oracle harness masks via metadata.json. Pinned NASM 3.02 per ADR-0005.
- `Cpu8086` is not thread-safe; the debug-session worker thread is the only caller (ADR-0004 A2).
- 8-bit register encoding: index 0-3 -> AL,CL,DL,BL; 4-7 -> AH,CH,DH,BH. Sreg field aliases `reg&3`.
- TASK-013 scope note: OR/AND/XOR arms exist only via the 80-83 groups; the rm/r logic families (08-0D, 20-25, 30-35, 84/85, A8/A9, F6/F7 /0 /2) are TASK-014.

## Exact next action
1. Optional: commit the pending TASK-013 changes (user asks or approves per protocol A8).
2. User runs `/assembly-implement TASK-014` (logic, shift and rotate instructions - extend the oracle file list with the TASK-014 files and add a `Cpu8086.Logic.cs` partial; the `Arith` core's OR/AND/XOR arms and `SetFlag`/`IncDecFlags` helpers are already in place).
3. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `.opencode/`.
