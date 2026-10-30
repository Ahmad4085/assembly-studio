# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-30 20:40 +0500 by Claude Code (Opus 5.5), after completing TASK-101 |
| Current task | **None in progress. Every remaining implementation task waits for a user action: AC-059 (TASK-061 → editor, M5), TASK-100 (→ TASK-102), TASK-006 (→ TASK-103, TASK-070)** |
| Last completed | TASK-101 Self-contained publish and Inno Setup installer (Done 2026-10-30 20:40 +0500) |
| Git state | HEAD `3b6a373` (TASK-101 committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-30 20:33 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1385/1385 passed**, two consecutive runs (2026-10-30 20:36 and 20:38 +0500).
- `build/installer/build-installer.ps1` → installer built; `build/installer/test-installer.ps1` → 21/21 (2026-10-30 20:31 +0500).

## Changes since the previous handoff
- **TASK-101 Installer** (Claude Code, uncommitted):
  - `build/fetch-innosetup.ps1`, `build/installer/{AssemblyStudio.iss, build-installer.ps1, test-installer.ps1}`, `docs/06-implementation/task-101-verification.md`.
  - App: command-line files open at startup (`StartupOptions.Files`).
  - **The AppId in the .iss must never change** (upgrades).
  - Run ISCC from PowerShell, not Git Bash (it rewrites `/Q` and `/D`).
  - Notices/release notes are packaged automatically once they exist at the repo root.
  - TASK-103 can reuse `test-installer.ps1` on the VMs (add the offline/network-disabled condition there).

## Failed tests and known issues
- `DEF-001`: App smoke test can fail in background runners (WPF menu focus). Open.
- `DEF-002`: one full solution run stalled in the engine tests (2026-10-29 10:36). Not reproduced since.
- `DEF-003`: `ComLoader` accepts at most 65,278 bytes while FR-031 and the build allow 65,280. **Needs a user decision.**
- `BLK-002`: TASK-061 waits for the user's manual AC-059 display check.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-016.
- `AssemblyStudio.Runtime`, `AssemblyStudio.Os.Dos`, and `AssemblyStudio.Toolchain.Nasm` reference only `AssemblyStudio.Core` (ADR-0002 graph, enforced by architecture tests).
- Console buffer cap is 1 MB with oldest output trimmed. Input events stamped with instruction count for replay determinism.
- Composition for a runnable machine (as in `DosServicesTests.Rig`):
  1. `memory = new RealModeMemory()`
  2. `dos = new DosServices(psp)`
  3. `router = new InterruptRouter(memory, dos)`
  4. `cpu = new Cpu8086(memory, router)`
  5. `new Machine(cpu, memory, console, router, dos)`, then `ComLoader.Load(machine, image)`
- For TASK-050: a `RuntimeError` halt from a DOS service carries its message in `DosServices.LastError`. The router's `LastHalt` covers only unsupported services and CPU exceptions.

## Exact next action
1. Done: TASK-101 committed. Files were:
   - `build/fetch-innosetup.ps1`, `build/installer/*`, `docs/06-implementation/task-101-verification.md`
   - `src/AssemblyStudio.App/{App.xaml.cs, Shell/StartupOptions.cs}`
   - `tests/AssemblyStudio.App.Tests/{StartupAndScalingTests.cs, Documents/OpenFromCommandLineProcessTests.cs}`
   - `docs/05-planning/backlog.md`, shared memory files
2. User actions now gate all remaining implementation: AC-059 check (BLK-002 → TASK-061 → TASK-064.. and M5), TASK-100 license decision (→ TASK-102), TASK-006 VMs (→ TASK-103, TASK-070).
3. Agent meanwhile: `/assembly-test` (test phase report for M1–M7 so far).

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
