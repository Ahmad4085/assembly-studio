# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-27 12:46 +0500 by OpenCode (GLM-5.3-Flash Free), after completing TASK-011 |
| Current task | **TASK-012 Data transfer instructions: `Ready`, approved, not started, no owner** |
| Last completed | TASK-011 Engine skeleton: decode, ModR/M, memory access (Done 2026-10-27 12:45 +0500) |
| Git state | HEAD `aa00e41` "docs: shared memory for TASK-010 and TASK-011", **pushed to origin/master** (2026-10-27, time not recorded; range 23e0fca..aa00e41). Working tree clean except untracked, not ours: `.idea/vcs.xml`, `.opencode/`, `opencode.jsonc` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → 0 warnings, 0 errors (2026-10-27 12:45 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **103/103 passed** (9 test projects), including 44 engine hand tests (2026-10-27 12:45 +0500).
- `build/fetch-nasm.ps1` → verified, idempotent (`tools/nasm/` populated). Oracle data: 8086 324/324 + 8088 v2 manifest verified (committed).

## Changes since the previous handoff
- TASK-010: Core abstractions v1 in `src/AssemblyStudio.Core/` (interfaces + immutable models, `MachineSnapshot` shape per ADR-0007 v1.1, XML docs) + 8 model tests.
- TASK-011: `Cpu8086 : ICpu` with pure decode, prefix handling, full ModR/M/EA table, 16-bit wraps, INT imm8 interception contract (router + real IVT vectoring), unsupported/alias-opcode halt with state unchanged; `Decoder` internal seam; `FakeMemory` double; 37 hand tests.
- (Earlier in this session, OpenCode: `/assembly-status`; committed TASK-005 as `a006825` + HANDOFF doc commit `cc128c0` on the user's request.)

## Failed tests and known issues
- None. (Intermediate run: 11 failures — three root causes, all fixed and re-verified; see `WORK_LOG.md` TASK-011.)
- Snapshot list-mutation producer contract documented on `MachineSnapshot`, not compiler-enforced (candidate for TASK-051).

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-015. Core has **no project references**; Engine.I8086 references only Core (Iced arrives with TASK-019).
- Engine API exposes snapshots, not live state (ADR-0002/0003). Decode is pure: halt restores CS:IP, nothing modified (FR-037).
- Undefined flags: deterministic value + reported mask (ADR-0003 §3). Pinned NASM 3.02 per ADR-0005.
- `Cpu8086` is not thread-safe; the debug-session worker thread is the only caller (ADR-0004 §2).

## Exact next action
1. ~~Commit the pending TASK-010 + TASK-011 changes~~ Done: committed (`b2d9e57`, `bafa380`, `aa00e41`) and pushed on the user's request (2026-10-27, time not recorded).
2. User runs `/assembly-implement TASK-012` (data transfer instructions; oracle wiring starts here).
3. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `.opencode/`.
