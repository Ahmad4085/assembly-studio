# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` A6).

| Field | Value |
|---|---|
| Written | 2026-10-27 16:12 +0500 by OpenCode (GLM-5.3-Flash Free), after completing TASK-014 |
| Current task | **TASK-015 Multiply, divide, BCD and conversions: `Approved`, not started, no owner** |
| Last completed | TASK-014 Logic, shift and rotate instructions (Done 2026-10-27 16:12 +0500) |
| Git state | HEAD `33a234f` "docs: shared memory for TASK-014", directly below the code commit `84077c5` "TASK-014: Logic, shift and rotate instructions with oracle-verified 8086 flag semantics". **TASK-013 + TASK-014 commits not yet pushed** (push only on user request). Untracked, not ours: `.idea/vcs.xml`, `.opencode/`, `opencode.jsonc` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` -> 0 warnings, 0 errors (2026-10-27 16:12 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` -> **389/389 passed** (2026-10-27 16:12 +0500); engine test project 330/330 - **all 213 in-scope SingleStepTests oracle files pass 100%** (71 TASK-012 data transfer + 84 TASK-013 arithmetic + 58 TASK-014 logic/shift/rotate) + 107 hand/other tests.

## Changes since the previous handoff
- TASK-014 in new `Cpu8086.Logic.cs`: AND/OR/XOR rm/r families (bases 08/20/30 - reuse the TASK-013 `Arith` core, `opcode>>3` maps onto `ArithKind`) and acc-imm (0C/24/34); TEST (84/85, A8/A9, F6/F7 arm 0 - AND flags, result discarded, AF untouched); NOT (F6/F7 arm 2, no flags); shifts/rotates D0-D3 all /reg via `ExecShiftGroup`.
- 8086 count semantics: count NOT masked, count 0 = full no-op (not even flags), OF only for count 1 (oracle-verified: ROL/RCL/SHL = MSB(result)^CF; ROR = bit7^bit6 of result; RCR = MSB(orig)^MSB(result); SHR = MSB(orig); SAR = 0), count>1 leaves OF untouched, rotates never touch SF/ZF/PF/AF.
- **Undocumented D0-D3 /6 = SETMO** (the backlog's "/6 handling decision"): operand becomes all ones (0xFF/0xFFFF), no-op at CL count 0, flags = real chip's raw values (SF=1, ZF=0, PF=1, CF=OF=AF=0; metadata 0xF72A reports them undefined).
- `ExecNeg` refactored to `ExecNegCore(m, cursor, wide)`; new `ExecF6F7Group` dispatches arms 0/2/3. The undocumented F6/F7 /1 TEST alias and MUL/DIV arms 4-7 halt unsupported (FR-037; /1 not in approved scope).
- Oracle harness extended by 58 TASK-014 files - all pass on the first run; 23 hand tests in new `LogicShiftRotateTests.cs` (3 of my hand-computed expectations were wrong initially - XOR bytes, AND imm value, RCR direction - engine was right in all three).

## Failed tests and known issues
- None failing. Notes: F6/F7 arms 1/4/5/6/7 halt until TASK-015 (multiply/divide); FF arms 2/3/4/5/7 halt until their tasks; D0-D3 /6 SETMO intentionally executes as the undocumented real-chip behavior, not a halt.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-015. Core has **no project references**; Engine.I8086 references only Core (Iced arrives with TASK-019).
- Engine API exposes snapshots, not live state (ADR-0002/0003). Decode is pure: halt restores CS:IP, nothing modified (FR-037).
- Undefined flags: deterministic value + reported mask (ADR-0003 A3) - the oracle harness masks via metadata.json (per-opcode `flags-mask`, reg-subkey when present). Pinned NASM 3.02 per ADR-0005.
- `Cpu8086` is not thread-safe; the debug-session worker thread is the only caller (ADR-0004 A2).
- 8-bit register encoding: index 0-3 -> AL,CL,DL,BL; 4-7 -> AH,CH,DH,BH. Sreg field aliases `reg&3`.
- TASK-014 quirks still in force (from TASK-012/013): PUSH SP pushes decremented SP; POP SP loads popped value; POPF `(popped|0xF002)&~0x0028`.
- Hand-test helper pattern: `NewCpu` needs TWO overloads (`NewCpu(params byte[] code)` and `NewCpu(Core.CpuState state, params byte[] code)`) - a single optional+params signature fails overload resolution (CS1503/CS0019).

## Exact next action
1. Optional: push the TASK-013 + TASK-014 commits to origin (user asks; push only on user request).
2. User runs `/assembly-implement TASK-015` (multiply, divide, BCD and conversions - F6/F7 arms 4-7, AAS/AAD/AAM/DAA/DAS etc. per the backlog scope; the `ExecF6F7Group` dispatcher is the seam for arms 4-7).
3. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `.opencode/`.
