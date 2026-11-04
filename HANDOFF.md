# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-11-04 12:45 +0500 by Claude Code (Opus 5.5); planning deferred by the user |
| Current task | **None. Release 1.0.0 is complete; the next milestone will be planned when the user asks** |
| Last completed | Release 1.0.0 published: https://github.com/Ahmad4085/assembly-studio/releases/tag/v1.0.0 |
| Git state | master pushed to origin (this commit included). Tag `v1.0.0` pushed. Local only (never push): `backup/before-vuid-rewrite`, `backup/after-first-rewrite`, `refs/original`. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-11-02, in `run-e2e.ps1` at 15:02 +0500 and the test rebuild after the AC-045 fix).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1549 passed, 7 skipped, 0 failed**, two consecutive runs (2026-11-02 19:42–19:47 and 19:47–19:51 +0500, after TASK-065). The skips are the opt-in measurement and real-key tests (D-018, `ASSEMBLY_STUDIO_BENCH`).
- `build/bench/run-e2e.ps1` → 7/7 measurement tests passed, every NFR target met (15:02 +0500; `docs/07-testing/performance-report-2026-11-02.md`).
- Notes: full runs now take 5–6.5 min (the UI test project starts the app). Debug runs can't judge the performance tests. While the user runs the app from `src/AssemblyStudio.App/bin/Release`, the Release build can't replace its files; never stop the user's app. Real-key UI tests: `ASSEMBLY_STUDIO_UI_INPUT=1 dotnet test --project tests/AssemblyStudio.App.UiTests -c Release`, with the user's agreement and the user away from the PC (D-018).

## Changes since the previous handoff
- **Status refresh**: `PROJECT_STATE.md` and `ROADMAP.md` corrected (phases 7–9 and milestones were stale; WVR-001 ended; open questions). Document inconsistencies outside `--refresh` are listed in `WORK_LOG.md` for a cleanup task.

- **Release 1.0.0 published** (Claude Code): Release 1.0.0 **published** to the private repository: https://github.com/Ahmad4085/assembly-studio/releases/tag/v1.0.0 (tag `v1.0.0` on `12ac130`; asset AssemblyStudio-1.0.0-setup.exe, 56,099,852 bytes, GitHub digest sha256:16ac15358b80d2d12425be5ed05bce5e89dbf2a453514cd5dfaa8ce7153f3d83 equal to the local file). Pushed `master` and the tag only. The unpushed history was rewritten first to remove the user's student ID (`docs/sdlc/commit-hash-map-2026-11-04.md`).

- **Release 1.0.0** (Claude Code): Release 1.0.0 candidate final: installer rebuilt from `0aaeaf6`, 53.5 MB, SHA-256 355e2428ba738c59038c509fe29f02da461544d88eaf24de000a33b808eb7c51, installer test 21/21; not published (the user's choice); unsigned (D-027).

- **Release candidate 1.0.0** (Claude Code): Release candidate 1.0.0 (`docs/09-release/release-1.0.0.md`): **Ready, with accepted conditions**; pending the user's decisions on shipping unsigned (OQ-020) and on publishing. Clean publish 495 files; full suite 1584 + 3 passed, 0 failed; measurements 7/7; installer 53.5 MB, SHA-256 e385d9f5…687bc (to be rebuilt after commit); installer test 21/21 on this PC; release notes (also RELEASE-NOTES.md, in-app), install guide, third-party obligations check written; `<Version>1.0.0</Version>` set.

- **QA 2026-11-04-2** (Claude Code; documents only): QA 2026-11-04-2: **Pass with conditions** (P0 71 verified, 4 not verified, 0 failed; P1 38 verified, 3 not verified; no Critical or High defect). Conditions: clean machines waived (WVR-002, release notes say so), usability session (TASK-105, or the user's explicit acceptance at release), user-reported evidence accepted as such, open Low test-run defects.

- **Testing phase 2026-11-04-1** (Claude Code; documents only): 1594 tests: 1587 passed, 0 failed, 7 skipped (opt-in); coverage engine 99.6%, runtime 95.3% (complete); measurement script 7 of 7 passed (NFR-005 warm median 1,780 ms, max 2,853 ms); 57 of 65 acceptance criteria pass in the run (AC-042 and AC-061 first time), 5 on manual or record-based evidence, 0 fail, 3 not run (AC-040, AC-041 under WVR-002; AC-062).

- **Installer test on this PC** (WVR-002 condition met): `build/installer/test-installer.ps1` 21/21 on this PC (2026-11-04 11:00:57–11:01:56), with the installer built at 10:44 from `e9a4ed0`'s code (license page, notices) and an upgrade installer compiled at 11:00 from the same publish folder: silent install 19.7 s (NFR-027 <= 120 s); installed 185.1 MB (NFR-028 <= 300 MB); files, shortcut, uninstall entry, association off by default; the installed application started (smoke test, 3,757 ms, its first start) and its NASM 3.02 ran; upgrade 1.0.0 -> 1.0.0-upgradetest with the association (26.5 s); uninstall removed program files, shortcut, association and entry; settings and a user source file unchanged. Not covered: the license page (a silent install skips it) and a check that LICENSE and THIRD-PARTY-NOTICES.txt were installed (the script has none; both were in the publish folder the installer was compiled from).

- **TASK-102** (Claude Code, Done, committed `e9a4ed0`): `build/installer/license-inventory.ps1` maps every published file to a component (494 files, 8 components; an unknown file or a NASM checksum mismatch stops it) and writes `THIRD-PARTY-NOTICES.txt` (shown in Help > About), the installer's license page `build/installer/license-agreement.txt` (D-026), `docs/09-release/license-inventory.md` and the SPDX 2.3 SBOM `docs/09-release/sbom.spdx.json`; Microsoft's .NET Library License and Windows SDK License texts saved in `build/installer/licenses/` with their source; the installer shows the license page and installs `LICENSE` and the notices; installer rebuilt: 53.5 MB. Run `build/installer/build-installer.ps1` for every release: it regenerates these files from the publish folder.

- **TASK-100** Done: MIT, "Copyright (c) 2026 Assembly Studio contributors" (D-025, OQ-007 answered); `LICENSE`; TC-096.
- **WVR-002**: TASK-006 (clean machines) waived for the first release; AC-040/AC-041 stay Not verified; known limitation "Tested systems"; the installer must still be rebuilt and tested on this PC.
- **TASK-105**: kit ready (`docs/08-qa/usability-session-kit.md`); the sessions need at least 5 real first-time participants (the user).

- **QA 2026-11-04-1** (Claude Code; documents only): QA 2026-11-04-1: **Pass with conditions** (P0 69 verified, 6 not verified, 0 failed; P1 37 verified, 4 not verified; no Critical or High defect, no open question). Conditions: clean-machine install (TASK-006, TASK-070, TASK-103), notices and SBOM (TASK-100, TASK-102), usability session (TASK-105), user-reported evidence accepted as such, open Low test-run defects.

- **TASK-116** (Claude Code, Done, committed `1d13231`): UI spec v1.2: §4 state matrix, UI-002 Command bar, "Build failed" now reads "Build; Run after an edit (rebuilds)", as AC-013 requires and the app does (D-024, OQ-026 answered); `BuildRunTests` comments cite it. No code change.

- **Testing phase 2026-11-03-4** (Claude Code; documents only): 1587 tests: 1580 passed, 0 failed, 7 skipped (opt-in); coverage engine 99.6%, runtime 95.3% (rerun after Claude Code stopped the first attempt for low memory); measurement script 7 of 7 passed (NFR-005 warm median 1,796 ms); 55 of 65 acceptance criteria pass in the run, 5 on manual or record-based evidence, 0 fail, 5 not run; DEF-003 verified. Low memory on this PC stopped one background run; long runs are safer in the foreground or with build servers shut down.

- **TASK-115** (Claude Code, Done, committed `53fc76c`): The build's .COM limit and its BLD006 message now say 65,278 bytes, matching the loader (FR-031 v2.2, D-023); the loader's message lost its wrong "(65,280 bytes)"; the help pages state the limit plainly (the DEF-003 note is gone); ADR-0005, the architecture's error table and `known-limitations.md` follow; new `ProgramTests.ComSizeLimitTests` builds, loads and runs an image of exactly `ComLoader.MaxComImageSize` bytes and checks one byte more fails with BLD006. DEF-003 Fixed (unverified).

- **QA 2026-11-03-3** (Claude Code; documents only): QA 2026-11-03-3: **Pass with conditions** (P0 69 verified, 6 not verified, 0 failed; P1 37 verified, 4 not verified; no Critical or High defect open). Conditions: clean-machine install (TASK-006, TASK-070, TASK-103), notices and SBOM (TASK-100, TASK-102), usability session (TASK-105), an on-screen high-contrast recheck of the TASK-112 controls, user-reported evidence accepted as such, open Low defects.

- **Testing phase 2026-11-03-3** (Claude Code; documents only): 1585 tests: 1578 passed, 0 failed, 7 skipped (opt-in); measurement script 7 of 7 passed (NFR-005 warm median 1,861 ms); coverage engine 99.6%, runtime 95.3% (run complete); 55 of 65 acceptance criteria pass in the run (AC-060 first time), 5 on manual or record-based evidence, 0 fail, 5 not run; DEF-028 verified.

- **TASK-114** (Claude Code, Done, committed `745b3a2`): RTE021's next step now reads "Host file, device, process and network services are not available in the offline emulator. Use supported DOS functions (00h, 01h, 02h, 07h, 08h, 09h, 0Ah, 4Ch); see Help, DOS services." (no requirement ID; 00h added); `HelpTests.TASK_114_…` ties its list to the help page. DEF-028 Fixed (unverified).

- **TASK-104** (Claude Code, Done, committed `a9f59df`; scope widened with the user's approval): 46 user-facing texts moved into resource files with identical English: Runtime run-time messages (new `RuntimeMessages.resx`, 24), DOS service errors (new `DosMessages.resx`, 5), assembler problems (new `AssemblerProblems.resx`, 12), program-end names (`Strings.resx`, 5); `ArchitectureTests.LocalizableStringTests` scans all C# and XAML under `src` (allow-list with reasons); review record `docs/06-implementation/localization-review.md`; DEF-028 raised (RTE021 wording). New user-facing text must go into a `.resx`; `LocalizableStringTests` fails otherwise (add an allow-list entry with a reason only for text no user reads).

- **TASK-113** (Claude Code, Done, committed `26a64a7`): A "Known limitations" section in the help page "What Assembly Studio supports" (the first start after a Windows restart; the Variables panel: 1- and 2-byte variables shown as numbers, at most 64 bytes, 2 regions of 4,096 bytes, "not shown") and `docs/09-release/known-limitations.md` v0.1 (the release-notes text with its sources). Requirements citation corrected FR-073 → NFR-033, FR-045. `/assembly-release` copies `docs/09-release/known-limitations.md` §2 into the release notes.

- **Testing phase 2026-11-03-2** (Claude Code; documents only): 1566 tests: 1559 passed, 0 failed, 7 skipped (opt-in); measurement script 7 of 7 passed (NFR-005 warm median 1,562 ms); coverage engine 99.6%, runtime 93.8% (run incomplete: DEF-011 clipboard under coverage); 54 of 65 acceptance criteria pass in the run, 5 on manual or record-based evidence, 0 fail, 6 not run; DEF-022, 024, 025, 026 verified.

- **TASK-112** (Claude Code, Done, committed `de42bf2`): Themed input controls (UI spec and design tokens v1.1): the control tokens in Dark, Light and HighContrast (system colors); implicit TextBox, Button, CheckBox, ComboBox (+ ComboBoxItem) styles with rest, hover, pressed, 2 px outside focus ring, disabled, checked and the accent default button; the Go to Line error border; the Settings window's local button style now based on the themed one. A window-level implicit style of these four types must be `BasedOn` the themed one, or it replaces it.

- **Design v1.1** (Claude Code; documents only): UI spec v1.1 and design tokens v1.1 (themed input controls: `Control.Input/Button/Hover/Pressed/Selected`, `Accent.Primary.Hover`, `Size.Control`, `Size.CheckBox`; TextInput, Button, CheckBox, ComboBox conventions as implicit styles; 50 contrast pairs measured, all passing) **Awaiting approval**.

- **TASK-110** (Claude Code, Done, committed `99f2dd9`): `build/test.ps1 -ResultsDirectory <dir>` (relative or absolute) makes both steps write TRX and xUnit XML results; the broken `--` pass-through is gone; the script help and test plan §4 (v0.7) say so (DEF-024). A test-phase run: `powershell -File build/test.ps1 -NoBuild -ResultsDirectory artifacts/test-results/<run>`.

- **TASK-111** (Claude Code, Done, committed `bfdf168`): Missing memory windows and repeated announcements: both panels look their window up with `MemoryViewModel.FindWindow` (null when the snapshot has none; `MemoryWindow` is a struct, DEF-025); announcements are made once per (state, instruction count) and reset when a session ends (DEF-026). Use `MemoryViewModel.FindWindow` for any new memory-window lookup.

- **TASK-109** (Claude Code, Done, committed `362dad6`): The find bar window test checks the Enter, F3 and Esc key bindings (found as WPF routes the key, from the target up) and runs their commands instead of simulating key presses (DEF-022). Full runs found **DEF-025 (High)**: `VariablesViewModel.Show` (and the same pattern in `MemoryViewModel.Show`) treats a missing `MemoryWindow` (a struct) as present and throws; DEF-026 duplicate "Ready" announcement; DEF-027 load. Fix task TASK-111 approved.

- **QA 2026-11-03-2** (Claude Code; documents only): QA verdict **Pass with conditions** (`docs/08-qa/qa-report-2026-11-03-2.md`): no P0 acceptance criterion fails, no Critical or High defect open; P0 69 verified (some user-reported), 6 not verified, all tied to unbuilt release tasks; conditions: TASK-103 clean-machine install (rebuild the installer with ReadyToRun first), TASK-100/102 notices, TASK-104/105, known limitations in the help and release notes, user-reported evidence accepted as such, Low defects DEF-003/022/024.

- **Requirements change (approved):** NFR-005 revised for the warm start (D-022, approved 2026-11-03 12:11 +0500; NFR v2.1, AC v2.5): OQ-027 answered, DEF-023 Won't fix (approved), AC-056 passes against the new target; AC-001 still needs its offline check; QA must run again.

- **QA 2026-11-03-1** (Claude Code; documents only): QA verdict **Fail** (`docs/08-qa/qa-report-2026-11-03-1.md`): AC-001 (FR-001, P0) fails on its NFR-005 timing (cold start 7,752 ms, DEF-023); P0 64 verified, 1 failed, 10 not verified; P1 32 verified, 1 failed (NFR-005), 7 not verified; no Critical defect; code checks clean (no network APIs, no fake data, no hard-coded colors). Traceability has a "QA status" column now.

- **Testing phase 2026-11-03-1** (Claude Code; documents only): 1560 tests: 1553 passed, 0 failed, 7 skipped (opt-in); measurement script 7 of 7 passed (NFR-005 warm median 1,975 ms); coverage complete (engine 99.6%, runtime 93.8%, total 90.2%); 53 of 65 acceptance criteria pass, AC-056 fails (NFR-005 cold start 7,752 ms, DEF-023), 11 not run; DEF-020 and DEF-021 verified; DEF-024 new (`build/test.ps1` `--` options). Full suite until DEF-024 is fixed: run the two `dotnet test` steps of `build/test.ps1` directly when result options are needed.

- **TASK-108 Startup time** (Claude Code, In progress, committed `e6af964`): Startup time (DEF-021): a startup timeline in the log found the main window's XAML (1.0–1.9 s) and first layout (0.7–0.9 s) dominant, with AvalonEdit's first use 0.3–0.5 s of it; changes: ReadyToRun for the published app (D-021, ADR-0009 amendment), an editor warm-up thread, the content catalog loaded in parallel; NFR-005 on the published app 2026-11-03: median 1,915 ms, max 2,237 ms. `docs/06-implementation/task-108-verification.md`. The startup timeline is in every startup log ("Startup … ms to the first frame: …"). `run-e2e.ps1` now publishes the app (installer configuration) to `artifacts/bench/app` and measures it.

- **TASK-107 UI test robustness** (Claude Code, Done, committed `26a7330`): UI test robustness (DEF-020): `AppUnderTest` waits up to 60 s for the window; when it doesn't appear, or the start fails otherwise, it stops the process, deletes its temporary folder and fails with the application's log in the message; `build/test.ps1` runs every project except the UI tests, then the UI tests alone. Full-suite command: `powershell -File build/test.ps1` (add `-NoBuild`; result options after `--`). DEF-020 Fixed (unverified); DEF-022 (find bar window test, simulated Esc) Open.

- **Testing phase 2026-11-02-1** (Claude Code; documents only): 1556 tests: 1548 passed, 1 failed (TC-087 UI smoke test, DEF-020), 7 skipped (opt-in); measurement script 6 of 7 passed (NFR-005 startup median 3,005 ms > 3 s, DEF-021); coverage complete (engine 99.6%, runtime 93.8%, total 90.9%); 53 of 65 acceptance criteria pass, AC-056 fails, 11 not run; DEF-006, 009, 010, 013, 017, 018, 019 verified. Test plan v0.3 (TC-074..TC-091), traceability v2.6, AC v2.3, defects DEF-020 and DEF-021 opened.

- **TASK-065 Find, replace and go to line** (Claude Code, Done, committed `17313c4`):
  - `src/AssemblyStudio.App/Editor/Search/`: `SearchRules` (pure matching rules), `ISearchTarget` (implemented explicitly by `CodeEditor`), `FindReplaceViewModel` (`ShellViewModel.Find`; the window supplies `ActiveSearchTarget` and `AskLine`), `GoToLineDialog`.
  - `FindBar` in `MainWindow.xaml` above the editors; key bindings on `EditorPanel` (Ctrl+F, F3, Shift+F3, Ctrl+H, Ctrl+G); Esc in the bar closes it (element binding wins over the window's Esc = Cancel build).

- **TASK-106 Variables panel** (Claude Code, Done, committed `a40f682`; added by the user's request, D-020):
  - Core: `DataSymbol`, `ISourceMap.DataSymbols` (default empty). NASM: `NasmSourceMap.FindDataSymbols` (map symbols whose first bytes come from a data line; size over the following data lines).
  - App: `Panels/Memory/VariablesViewModel.cs` (up to 2 watch regions of 4 KiB; the memory view now matches only its own 256-byte window), `VariablesTab` in `MainWindow.xaml`, `BottomTab.Variables`, `ShellViewModel` tab rule (Ready → Variables; end → Console if it printed, else Variables).
  - The memory panel and the variables panel share the session's 4 watch slots (1 + up to 2).

- **TASK-086** (Claude Code, Done, committed `b238255`):
  - Measurements: `tests/AssemblyStudio.App.Tests/Performance/EndToEndPerformanceTests.cs`, `tests/AssemblyStudio.App.UiTests/PerformanceTests.cs`, `build/bench/run-e2e.ps1` (opt-in with `ASSEMBLY_STUDIO_BENCH=1`; the script sets it). Report: `docs/07-testing/performance-report-2026-11-02.md`.
  - Fixes: DEF-017 BOM build (`NasmAssembler`), DEF-018 step latency (`MemoryViewModel` rows updated in place: `ByteCell`/`MemoryRow`/`StackRow` are observable classes now, not records), DEF-019 output flood (`ConsoleScreen.Width = 80`, wrap per **D-019**).
  - Measurement notes: waits in timing tests pump at `Input` priority (a `Background` wait starves while a program runs); render is a dispatcher round at `Loaded`.

- **TASK-085** (Claude Code, Done; Narrator check by the user):
  - Announcements: `BuildRunViewModel.Announcement`/`Announced`; `AnnouncementRegion` (polite live region) in `MainWindow.xaml`; the state badge no longer announces.
  - New project `tests/AssemblyStudio.App.UiTests` (FlaUI.UIA3 5.0.0; `Directory.Packages.props`, `AssemblyStudio.slnx`): `AppUnderTest` (temp folders, focus guard, waits for a click), `UiSmokeTests` (structure; AC-043 opt-in).
  - D-018 (opt-in real-key tests), ADR-0008 changelog (licenses), accessibility-findings.md Part C.
  - **For TASK-086:** NFR targets and the existing performance tests (`Category=Performance`) are the starting point; the UI test project can drive end-to-end timings without key presses.

- **TASK-069 Editor accessibility** (Claude Code, Done; Narrator walkthrough run by the user, overall pass):
  - `CodeEditor`: TextArea UIA name bound to the editor's name + help text; `tests/AssemblyStudio.App.Tests/Accessibility/EditorAccessibilityTests.cs`; `docs/06-implementation/accessibility-findings.md` (Part A measured, Part B manual walkthrough NOT RUN); ADR-0006 changelog.
  - **For TASK-085:** the step announcement ("Paused at line 7. Changed: AX, DX, ZF.") can use `CpuViewModel.ChangedNames`; the state badge already raises LiveRegionChanged.

- **TASK-083 Session panels** (Claude Code, uncommitted):
  - `src/AssemblyStudio.App/Panels/Session/SessionPanels.cs`; strip and tabs in `MainWindow.xaml`; `BuildRunViewModel.{Strip, Messages, History, Breakpoints}` and the event log (`LogEvent`).
  - **Additive contract change:** `StepResult.Message` (`RuntimeMessage` in Core) set by `InterruptRouter`; `RuntimeErrorPolicy` RTE010/011/090; `SnapshotContent` keeps 6 bytes for an undecodable instruction. ADR-0007 changelog updated.
  - **For TASK-085:** `CpuViewModel.ChangedNames` and the runtime messages list (polite live region) are ready for the step announcement ("Paused at line 7. Changed: AX, DX, ZF.").

- **TASK-082 Memory and stack** (Claude Code, uncommitted):
  - `src/AssemblyStudio.App/Panels/Memory/{MemoryAddress, MemoryViewModel}.cs`; `MemoryPanelTemplate`/`StackPanelTemplate` in `MainWindow.xaml` (docked and compact tabs); `BuildRunViewModel.Memory` and the write-journal collection.
  - **For TASK-083:** the history panel can use `IDebugSession.History` (or accumulate `HistoryDelta` like the write journal); breakpoints already live on `DocumentViewModel.Breakpoints` and `IDebugSession.Breakpoints`; the runtime messages come from `RunState`/`Detail`/`ExitCode` in `BuildRunViewModel`; `ApplyPendingSnapshot` now separates a new pause from a refresh (`newPause`).

- **TASK-081 Registers and flags** (Claude Code, uncommitted):
  - `src/AssemblyStudio.App/Panels/Machine/{ValueFormat, CpuViewModel}.cs`; templates and panels in `MainWindow.xaml`; `BuildRunViewModel.Cpu` and `LatestSnapshot`.
  - **D-017 (user request):** register rows show AX–DX with their halves on the same row, values in boxes (`RegisterRow`, `RegisterChipTemplate`, `RegisterRowTemplate`); DECISIONS.md and ui-spec UI-005 updated. Note: the decision table in DECISIONS.md is not in numeric order; check the highest ID before adding one.
  - **For TASK-082:** reuse `CpuViewModel.Format` (or move it to a shared machine-panel object) for the memory and stack values; `MemoryWindows` need `IDebugSession.WatchMemory`; follow the same Show/MarkRunning/Clear pattern from `ApplyPendingSnapshot`. Ctrl+Alt+N is bound on `MachineRegion`; the memory and stack panels are in the bottom region, so add the binding there too.

- **TASK-084 Console** (Claude Code, uncommitted):
  - `src/AssemblyStudio.App/Panels/Console/{ConsoleScreen, ConsoleViewModel, ConsoleKeys, ConsoleView}.cs`; console tab in `MainWindow.xaml`; `BuildRunViewModel.Console` (bounded pending output); `ShellViewModel` tab follows the run state.
  - **For TASK-081..083:** same pattern as the console: give `BuildRunViewModel` a panel view model and feed it from `ApplyPendingSnapshot` (UI thread, latest snapshot only).

- **TASK-080 Build/Run/Step** (Claude Code, uncommitted):
  - `src/AssemblyStudio.App/Commands/{BuildRunViewModel, SessionFactory, CommandTable}.cs`, `src/AssemblyStudio.App/Panels/Problems/ProblemsViewModel.cs`.
  - `ShellViewModel.BuildRun`; the App composes it with Settings (assembler path, instruction budget) and the log.
  - **For TASK-084 (console):** `BuildRunViewModel.ConsoleText` holds the output since load/reset (cleared on Reset and on a new run); input goes through the session (`SendInput`); `SendCtrlCCommand` exists (Ctrl+Break). The console's own Ctrl+C routing (D-015) and Copy (Ctrl+Shift+C) are TASK-084.
  - **For TASK-081..083:** read the session through `BuildRunViewModel` (add a snapshot property or event there); running snapshots have no current instruction (FR-057).
  - Tests that wait for snapshots pump the dispatcher, so they must be in the `AppProcessTests` (non-parallel) collection.
- DEF-013 fixed (`Renderers.cs`), DEF-014 Open, OQ-026 Open.

- **TASK-066 Gutter and diagnostics** (Claude Code, uncommitted):
  - `src/AssemblyStudio.App/Editor/Adornments/`; `CodeEditor`, `DocumentViewModel`, `MainWindow.xaml`, strings.
  - **For TASK-080:** set `DocumentViewModel.Diagnostics` from `BuildResult.Diagnostics` (lines in the built file); bind `Breakpoints` and replace each with `Bound` + address or `Unbound` after a build; set `ExecutionLine`/`ExecutionMarker` from the paused snapshot through the source map; set `HasSession` while a session runs (editing then sets `IsSessionStale`).
- Test fixes: DEF-010 (latency test serial, best of 3), DEF-011 (clipboard retry). DEF-012 Open.

- **TASK-064 Editor** (Claude Code, uncommitted):
  - AvalonEdit package; `src/AssemblyStudio.App/Editor/{CodeEditor.cs, NasmHighlighting.cs, NasmSyntax.xshd}`; zoom in `ShellViewModel`/`LayoutState`/`EditorAppearance`/App; MainWindow editor template and Edit/View menu wiring.
  - **For TASK-066:** add the diagnostics squiggles (a background renderer) and the breakpoint/current-line margin to `CodeEditor`.
  - `MainWindow.ActiveEditor()` returns the active `CodeEditor`.
  - UI Automation sees the editor as ControlType **Document** (Value/Text patterns).

## Failed tests and known issues
- DEF-006 (flood test, failed in report 2026-10-30-1): fixed in TASK-087 (now `FloodPerformanceTests`, CPU-time limit); passed in two full runs (21:30, 21:33). **Fixed (unverified)** until `/assembly-test rerun DEF-006`.
- DEF-009 (coverage incomplete): fixed in TASK-087 (performance trait excluded); coverage run complete at 21:27. **Fixed (unverified)**.
- `DEF-001`: App smoke test can fail in background runners (WPF menu focus). Open.
- `DEF-002`: one full solution run stalled in the engine tests (2026-10-29 10:36). Not reproduced since.
- `DEF-003`: `ComLoader` accepts at most 65,278 bytes while FR-031 and the build allow 65,280. **Needs a user decision.**
- `WVR-001`: the AC-059 manual display check is waived until QA (BLK-002 resolved by the waiver); it must still pass before release.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-020.
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
  - Performance tests carry `[Trait("Category", "Performance")]` and a non-parallel collection; the coverage script excludes the trait.
  - Test cases: one TC per test class (inventory in `docs/07-testing/test-plan.md`); new classes take the next free number.

## Exact next action
1. Nothing pending. The user stopped `/assembly-plan` (2026-11-04 12:45 +0500): the IDE is complete for 1.0.0 and further changes come later.
2. When the user wants more: `/assembly-plan` with a scope (options discussed: 1.0.x maintenance and document cleanup; debugger features FR-060/061/062/009; learning features FR-073/074; bigger programs FR-010/043/044, which need `/assembly-architecture` first).
3. Open items that stay recorded: TASK-105 (usability session, kit ready); TASK-006/070/103 (waived for 1.0.0, WVR-002); OQ-020 (signing); making the repository public (delete the local backup branches and `refs/original` first).

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
