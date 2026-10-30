# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-30 14:26 +0500 by Claude Code (Opus 5.5), after completing TASK-090 |
| Current task | **None in progress. M4: TASK-060, 062, 063, 067, 068 Done; TASK-061 Blocked (BLK-002), so the editor tasks and M5 wait. M6: TASK-090 Done. Eligible: `TASK-091` samples, `TASK-092`, `TASK-093`, `TASK-101`** |
| Last completed | TASK-090 Content catalog and Learn sidebar (Done 2026-10-30 14:26 +0500) |
| Git state | HEAD `5cf0171` (TASK-090 committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-30 14:17 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1193/1193 passed**, two consecutive runs (2026-10-30 14:21 and 14:25 +0500).

## Changes since the previous handoff
- **TASK-090 Content catalog and Learn sidebar** (Claude Code, uncommitted):
  - `src/AssemblyStudio.Content/{ContentCatalog.cs, Content/catalog.json, AssemblyStudio.Content.csproj}`, `src/AssemblyStudio.App/Learn/LearnViewModel.cs`, and the Learn panel in `Shell/MainWindow.xaml`.
  - **For TASK-091/092/093 (content authors):**
    - Put files under `src/AssemblyStudio.Content/Content/` (for example `Samples/hello.asm`) and add an item to `Content/catalog.json`: `{ "id", "kind": "sample|tutorial|reference|help", "title", "summary", "file", "expectedOutput" (samples only) }`.
    - Files are stored with LF (`.gitattributes`); a sample copy opens as a CR LF document.
    - `ContentCatalogTests.The_built_in_catalog_loads_and_every_item_is_readable` validates the shipped catalog.
    - TASK-091 should run each sample through the program-level harness and compare with `expectedOutput` (AC-035).
  - `ShellViewModel` takes `learn:`; its default is built from the built-in catalog.

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
1. Done: TASK-090 committed. Files were:
   - `src/AssemblyStudio.Content/{AssemblyStudio.Content.csproj, ContentCatalog.cs, Content/catalog.json}`
   - `src/AssemblyStudio.App/{App.xaml.cs, Learn/*, Documents/DocumentViewModel.cs, Documents/WorkspaceViewModel.cs, Shell/MainWindow.xaml, Shell/MainWindow.xaml.cs, Shell/ShellViewModel.cs, Shell/Strings.resx, Shell/Strings.cs}`
   - `tests/AssemblyStudio.Content.Tests/{AssemblyStudio.Content.Tests.csproj, ContentCatalogTests.cs, Fixtures/**}` and the deletion of `SkeletonTests.cs`
   - `tests/AssemblyStudio.App.Tests/Learn/*`
   - `docs/05-planning/backlog.md`, shared memory files
2. `/assembly-implement TASK-091` (samples). User: AC-059 check → TASK-061 → TASK-064 and M5.
3. User: TASK-006 test VMs, TASK-100 license decision, DEF-003 decision, the first version number.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
