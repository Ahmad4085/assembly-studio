# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-30 12:57 +0500 by Claude Code (Opus 5.5), after completing TASK-067 |
| Current task | **None in progress. M4: TASK-060, 062, 063, 067 Done; TASK-061 Blocked (BLK-002), so TASK-064/065/066/069 wait. Eligible: `TASK-068` About/diagnostics/logging** |
| Last completed | TASK-067 Settings service and dialog (Done 2026-10-30 12:57 +0500) |
| Git state | HEAD `39796a9` + uncommitted TASK-067 changes (commit only when the user asks). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-30 12:39 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1155/1155 passed**, two consecutive runs (2026-10-30 12:53 and 12:56 +0500). A run at 12:47 had 1154/1155: DEF-001 (the View-menu smoke test, Open) recurred.

## Changes since the previous handoff
- **TASK-067 Settings service and dialog** (Claude Code, uncommitted):
  - `src/AssemblyStudio.App/Settings/{AppSettings, SettingsService (ISettingsService, JsonSettingsService), SettingsViewModel, SettingsWindow.xaml(.cs) (+ SettingsLabels), EditorAppearance}.cs`.
  - App composition: settings service first, then theme, editor font, the bundled NASM path (`AppContext.BaseDirectory\tools\nasm\nasm.exe`), `ShellViewModel(..., openSettings)`.
  - **For TASK-064:** the editor must use `{DynamicResource Font.Editor}` / `FontSize.Editor` and read `ISettingsService.Current.TabWidth` / `UseSpaces` (and follow `Changed`).
  - **For TASK-080/081:** use `InstructionBudget` and `NumberFormat` from `ISettingsService`, and build with `NasmOptions { BundledPath, ConfiguredPath = settings.AssemblerPath }`.
  - `IDialogService.ShowOpenExecutable` was added; fakes must implement it.
  - The App project copies `tools/nasm/{nasm.exe, LICENSE}` to the output when present.

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
1. When the user asks: commit TASK-067. Files:
   - `src/AssemblyStudio.App/{App.xaml.cs, AssemblyStudio.App.csproj, Settings/*, Documents/IDialogService.cs, Documents/WpfDialogService.cs, Shell/MainWindow.xaml, Shell/ShellViewModel.cs, Shell/Strings.resx, Shell/Strings.cs, Themes/Shared.xaml}`
   - `tests/AssemblyStudio.App.Tests/{Settings/*, Documents/WorkspaceViewModelTests.cs}`
   - `docs/05-planning/backlog.md`, `docs/07-testing/defects.md`, shared memory files
2. User: AC-059 check → TASK-061 Done → `/assembly-implement TASK-064`. Meanwhile `/assembly-implement TASK-068`.
3. User: TASK-006 test VMs, DEF-003 decision.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
