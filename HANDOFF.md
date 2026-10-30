# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-30 21:12 +0500 by Claude Code (Opus 5.5), after the testing phase (report 2026-10-30-1) |
| Current task | **None in progress.** Testing phase done (report 2026-10-30-1). Remaining implementation waits for user actions: AC-059 (TASK-061 → editor, M5), TASK-100 (→ TASK-102), TASK-006 (→ TASK-103, TASK-070) |
| Last completed | Testing phase, `docs/07-testing/test-report-2026-10-30-1.md` (2026-10-30 21:12 +0500) |
| Git state | HEAD `c18ffc5` (testing phase committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-30 20:58 +0500).
- Test run 2026-10-30-1 (20:58–21:02): **1388 total, 1387 passed, 1 failed** (TC-040 flood test, DEF-006, timing under load).
- Coverage (20:48–20:55): engine 99.6%, runtime 93.5%; incomplete because of DEF-009.

## Changes since the previous handoff
- **Testing phase 2026-10-30-1** (Claude Code, uncommitted):
  - `docs/07-testing/test-plan.md` (new), `docs/07-testing/test-report-2026-10-30-1.md` (new), `docs/07-testing/defects.md` (DEF-006 updated, DEF-009 new).
  - Traceability v2.4 and acceptance criteria v2.1 test columns.
  - Backlog summary rows TASK-042/052 → Done.
  - New tests: `tests/AssemblyStudio.Debugging.Tests/AcceptanceStepTests.cs`; an FR-001 test in `tests/AssemblyStudio.App.Tests/Documents/OpenFromCommandLineProcessTests.cs`.
  - **TC numbering:** one TC per test class, in the plan's inventory; new classes get the next free number.

## Failed tests and known issues
- `AssemblyStudio.Debugging.Tests.Robustness.HostileProgramTests.A_flood_of_long_strings_reaches_the_budget_quickly` (TC-040): **failed** in test run 2026-10-30-1 (10.12 s, limit 10 s). DEF-006, Open. Passes alone (1.6–1.8 s). Stays failed until a re-run after a fix proves otherwise.
- `DEF-009`: wall-clock tests fail under coverage instrumentation; the coverage run is reported incomplete. Open.
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

- **Durable working notes (TASK-060 to the testing phase):**
  - Tests that launch the real app must pass temporary `--settings`, `--recovery` and `--log` paths, and belong to the non-parallel `AppProcessTests` collection.
  - The WPF test thread sets `App.ComposeOnStartup = false` and sets `ShutdownMode` after `InitializeComponent` (DEF-007).
  - Strings: edit `Shell/Strings.resx`, then run `build/generate-strings.ps1`.
  - Content: files under `src/AssemblyStudio.Content/Content/` plus `catalog.json` entries (samples need `expectedOutput`, and `input` when they read the keyboard; tutorials need `sample`). Text is stored with LF.
  - The instruction reference and the "Unsupported instructions" help page are generated: edit `ReferenceData/instructions.json`, run `ReferenceTests`/`HelpTests` with `ASSEMBLY_STUDIO_WRITE_REFERENCE=1`, rebuild, run the tests again.
  - The help consistency tests (TC-029, TC-043, TC-056) fail when DOS functions, NASM arguments, build codes, limits, start state or menu shortcuts change: update the help with them.
  - Installer: the AppId in `build/installer/AssemblyStudio.iss` must never change. Run ISCC from PowerShell (Git Bash rewrites `/Q` and `/D`). `test-installer.ps1` always uninstalls what it installed.
  - Test cases: one TC per test class (inventory in `docs/07-testing/test-plan.md`); new classes take the next free number.

## Exact next action
1. Done: testing phase committed. Files were:
   - `docs/07-testing/{test-plan.md, test-report-2026-10-30-1.md, defects.md}`
   - `docs/01-requirements/{REQUIREMENTS_TRACEABILITY.md, ACCEPTANCE_CRITERIA.md}`, `docs/05-planning/backlog.md`
   - `tests/AssemblyStudio.Debugging.Tests/AcceptanceStepTests.cs`, `tests/AssemblyStudio.App.Tests/Documents/OpenFromCommandLineProcessTests.cs`
   - shared memory files
2. User decisions: a task to fix DEF-006 and DEF-009 (test-only fixes); `/assembly-qa` for a QA verdict on the current scope; the user actions AC-059, TASK-100, TASK-006.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
