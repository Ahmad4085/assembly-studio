# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## Active Task

None in progress. TASK-061 is **Blocked** on BLK-002 (the user's manual AC-059 display check); the editor tasks and M5 depend on it.

| Field | Value |
|---|---|
| Status | Idle / Ready for next task |
| Completed Milestones | **M1**, **M2**, **M3** |
| Milestone 4 progress | TASK-060, 062, 063, 067, 068 Done; TASK-061 Blocked (BLK-002); TASK-064..066, 069 wait for TASK-061; TASK-070 also needs TASK-006 |
| Milestone 6 progress | TASK-090 Done. Eligible: `TASK-091` (samples), `TASK-093` (help); `TASK-092` (tutorials/reference) also needs TASK-020 (Done) |
| Recommended next action | `/assembly-implement TASK-091` (samples, fills the Learn sidebar). **User: AC-059 check** for the editor chain and M5 |
| Parallel user actions | AC-059 manual check, `TASK-006` (clean test VMs), `TASK-100` (license decision), DEF-003 decision |

---|---|
| Status | In progress |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-30 14:07 +0500 |
| Milestone | M6 (Learning content) |
| Goal | Provide the content catalog and the Learn sidebar; samples open as untitled copies |
| Scope (in) | `IContentCatalog` from embedded resources; sidebar UI with keyboard navigation |
| Out of scope | Content authoring (TASK-091 samples, TASK-092 tutorials and reference) |
| Requirements | FR-069, FR-071, NFR-023; UI-015, UI-020; AC-035 (open-as-copy part) |
| Expected files | `src/AssemblyStudio.Content/`, `src/AssemblyStudio.App/Learn/`, tests |

---|---|
| Status | Idle / Ready for next task |
| Completed Milestones | **M1**, **M2**, **M3** |
| Milestone 4 progress | TASK-060, 062, 063, 067, 068 Done; TASK-061 Blocked (BLK-002); TASK-064..066, 069 wait for TASK-061; TASK-070 also needs TASK-006 |
| Recommended next action | **User: AC-059 check** (the remaining M4 tasks and all of M5 depend on TASK-061). Eligible now: `TASK-090` (content catalog and Learn sidebar), `TASK-101` (self-contained publish and installer); user: `TASK-100` (product license decision) |
| Parallel user actions | AC-059 manual check, `TASK-006` (clean test VMs), DEF-003 decision |

---|---|
| Status | In progress |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-30 13:16 +0500 |
| Milestone | M4 (App shell and editor) |
| Goal | Show versions, release notes and third-party notices, and add the local rolling log |
| Scope (in) | Copyable diagnostics text; release notes and notices views from installed files; rolling log <= 10 MB, no upload |
| Out of scope | Notice content (TASK-091) |
| Requirements | FR-076, FR-087, FR-090, NFR-012; UI-018; ADR-0007 §5; AC-038, AC-063 |
| Expected files | `src/AssemblyStudio.App/About/`, `src/AssemblyStudio.App/Logging/`, tests |

---|---|
| Status | Idle / Ready for next task |
| Completed Milestones | **M1**, **M2**, **M3** |
| Milestone 4 progress | TASK-060, 062, 063, 067 Done; TASK-061 Blocked (BLK-002); TASK-064..066, 069 wait for TASK-061; TASK-070 also needs TASK-006 |
| Recommended next action | User: AC-059 check (unblocks TASK-061 → TASK-064). Meanwhile eligible: `TASK-068` (About/diagnostics, release notes, notices, logging) |
| Parallel user actions | AC-059 manual check, `TASK-006` (clean test VMs), DEF-003 decision |

---|---|
| Status | In progress |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-30 12:24 +0500 |
| Milestone | M4 (App shell and editor) |
| Goal | Persist settings safely and provide the Settings dialog (UI-017) |
| Scope (in) | JSON settings with corrupt-file recovery + notice; assembler path validation; editor and execution preferences; theme; reset all; zero-config defaults with bundled NASM |
| Requirements | FR-075, FR-077..FR-082, NFR-009; ADR-0007 §4; UI-017, UI-019; AC-039, AC-053, AC-054 (UI part), AC-015 (UI part) |
| Expected files | `src/AssemblyStudio.App/Settings/`, startup wiring, tests |

---|---|
| Status | Idle / Ready for next task |
| Completed Milestones | **M1**, **M2**, **M3** |
| Milestone 4 progress | TASK-060, TASK-062, TASK-063 Done; TASK-061 Blocked (BLK-002); TASK-064..066, 069 wait for TASK-061 |
| Recommended next action | User: AC-059 check (unblocks TASK-061 → TASK-064). Meanwhile eligible: `TASK-067` (settings service and dialog), `TASK-068` (About/diagnostics, logging) |
| Parallel user actions | AC-059 manual check, `TASK-006` (clean test VMs), DEF-003 decision |

---|---|
| Status | In progress |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-30 11:56 +0500 |
| Milestone | M4 (App shell and editor) |
| Goal | Autosave unsaved documents at most every 60 s and offer recovery after an unexpected exit |
| Scope (in) | Recovery folder, autosave timer, recovery prompt (UI-021) |
| Out of scope | Versioned history |
| Requirements | NFR-011; UI-021; AC-050; architecture §5 (`%LOCALAPPDATA%\AssemblyStudio
ecovery\`) |
| Expected files | `src/AssemblyStudio.App/Documents/Recovery/`, startup wiring, tests |
| Note | Chosen by the user instead of TASK-064, which waits for TASK-061 (Blocked, BLK-002) |

---|---|
| Status | Idle / Ready for next task |
| Completed Milestones | **M1**, **M2**, **M3** |
| Milestone 4 progress | TASK-060, TASK-062 Done; TASK-061 Blocked (BLK-002) |
| Recommended next action | `/assembly-implement TASK-063` (crash recovery) or `TASK-064` (AvalonEdit editor integration) |
| Parallel user actions | AC-059 manual check, `TASK-006` (clean test VMs), DEF-003 decision |

---|---|
| Status | In progress |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-30 11:28 +0500 |
| Milestone | M4 (App shell and editor) |
| Goal | Open, edit, save (atomic) and close documents safely, preserving encoding and line endings |
| Scope (in) | New (empty / .COM template), Open (dialog, drag-drop), Save/Save As atomic, Close with prompt; encoding detection + preservation, EOL preservation + normalize confirmation; file error messages, recent files, welcome page |
| Out of scope | Editor control (TASK-064): tabs use a plain text box until then |
| Requirements | FR-001–FR-008, FR-019, FR-020, NFR-029; UI-003, UI-019, UI-020; AC-001–AC-008, AC-012 |
| Expected files | `src/AssemblyStudio.App/Documents/`, shell view and view model, `tests/AssemblyStudio.App.Tests/` |
| Note | TASK-061 stays Blocked on BLK-002 (user check), not active |

---|---|
| Status | Idle / Ready for next task |
| Completed Milestones | **M1**, **M2**, **M3** |
| Milestone 4 progress | TASK-060 Done; TASK-061 Blocked (BLK-002, user check) |
| Recommended next action | `/assembly-implement TASK-062` (document service and file workflow) |
| Parallel user actions | AC-059 manual check (`docs/06-implementation/task-061-verification.md`), `TASK-006` (clean test VMs), DEF-003 decision |

---|---|
| Status | In progress |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-30 11:12 +0500 |
| Milestone | M4 (App shell and editor) |
| Goal | Dark, Light and HighContrast dictionaries from the approved tokens, with an automated contrast test |
| Scope (in) | All token keys per theme; HC via SystemColors; follow Windows app mode, override in settings; automated WCAG contrast test; text scaling check at 200 % |
| Out of scope | Panel styling details |
| Requirements | NFR-022, FR-094, NFR-026; UI-001; design tokens v1.0; AC-059 (manual) |
| Expected files | `src/AssemblyStudio.App/Themes/`, theme selection in the shell, `tests/AssemblyStudio.App.Tests/` |

---|---|
| Status | Idle / Ready for next task |
| Completed Milestones | **M1** (engine), **M2** (runtime, DOS, toolchain), **M3** (debug session, headless) |
| Recommended next action | `/assembly-test` (milestone verification M1–M3), then M4: `/assembly-implement TASK-061` (themes) or `TASK-062` (document service) |
| Parallel user action | `TASK-006` (clean test VMs) can start any time; decide DEF-003 (.COM size limit 65,278 vs 65,280) |

---|---|
| Status | In progress |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-29 15:44 +0500 |
| Milestone | M3 (Debug session, headless) |
| Goal | Prove that hostile or faulty programs can't crash or hang the host |
| Scope (in) | Engine exception → session error state + local log; fuzz: random images under budget; 30-minute soak; output flood |
| Out of scope | UI |
| Requirements | NFR-010, FR-096, NFR-013, NFR-015; ADR-0004; AC-045 (session level) |
| Expected files | `src/AssemblyStudio.Debugging/DebugSession.cs` (containment), Core `IDebugSession.LastFault`, `tests/AssemblyStudio.Debugging.Tests/Robustness/` |

---|---|
| Status | Idle / Ready for next task |
| Completed Milestones | M1 (engine), M2 (runtime, DOS, toolchain) |
| Milestone 3 progress | M3: Debug session (TASK-050..053 Done; 4/5) |
| Recommended next action | `/assembly-implement TASK-054` (fault containment, fuzz and soak; last M3 task) |
| Also eligible | `TASK-061` (Themes) |
| Parallel user action | `TASK-006` (clean test VMs) can start any time; decide DEF-003 (.COM size limit 65,278 vs 65,280) |

---|---|
| Status | In progress |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-29 15:28 +0500 |
| Milestone | M3 (Debug session, headless) |
| Goal | Deliver console input and Ctrl-C to the program and replay recorded sessions identically |
| Scope (in) | SendInput / SendCtrlC commands; replay from recorded input |
| Out of scope | UI key handling (TASK-074) |
| Requirements | FR-064, NFR-016, D-015; AC-047 (session level); Ctrl-C during 01h/07h |
| Expected files | `src/AssemblyStudio.Debugging/DebugSession.cs`, Core `IDebugSession`/`IConsoleDevice` additions, tests |

---|---|
| Status | Idle / Ready for next task |
| Completed Milestones | M1 (engine), M2 (runtime, DOS, toolchain) |
| Milestone 3 progress | M3: Debug session (TASK-050, TASK-051, TASK-052 Done; 3/5) |
| Recommended next action | `/assembly-implement TASK-053` (input routing, Ctrl-C, replay) |
| Also eligible | `TASK-061` (Themes) |
| Parallel user action | `TASK-006` (clean test VMs) can start any time; decide DEF-003 (.COM size limit 65,278 vs 65,280) |

---

## Completed: TASK-090 Content catalog and Learn sidebar (Done 2026-10-30 14:26 +0500, Claude Code (Opus 5.5))
- `src/AssemblyStudio.Content/ContentCatalog.cs`:
  - `ContentKind` (Sample/Tutorial/Reference/Help), `ContentItem` (id, kind, title, summary, file, expected output for samples), `IContentCatalog`, `EmptyContentCatalog`, `ContentCatalogException`.
  - `EmbeddedContentCatalog` reads `content/catalog.json` and files from embedded resources. It validates unique ids, kinds, titles and summaries, that every file is embedded, and that samples (only) document expected output.
  - `Content/catalog.json` ships **empty**: content is authored by TASK-091 (samples), TASK-092 (tutorials, reference) and TASK-093 (help). The csproj embeds `Content/**`.
- `src/AssemblyStudio.App/Learn/LearnViewModel.cs` (UI-015):
  - Sections Samples/Tutorials/Reference/Help with empty-state text.
  - Selection details; tutorial/reference/help text in a read-only view captioned "Explanation, not program state" (FR-072).
  - "Open a copy" / Enter on a sample → `WorkspaceViewModel.OpenCopy`.
- Documents: an untitled copy titled "hello.asm (copy)"; Save is Save As suggesting "hello.asm"; the built-in original can't change (FR-069). `DocumentViewModel.CopyOf` / `SuggestedFileName`.
- Shell: Learn sidebar is a TreeView (arrow keys, Enter) with a details area. F6 and Ctrl+Alt+L focus the tree. Learn ▸ Samples/Tutorials/Reference and the welcome page's new "Open a sample" select the section. App loads the built-in catalog and logs + falls back to empty if it is broken.
- Tests:
  - Content: `ContentCatalogTests` (10) with embedded fixture catalogs (valid + 5 malformed) and the built-in catalog. The placeholder `SkeletonTests` was removed.
  - App: `LearnTests` (9), including AC-035 open-as-copy (title, untitled, Save As suggestion, original unchanged, two independent copies), menu/welcome commands, and the real window (render `learn-sample-selected.png`, `learn-tutorial-selected.png`).
  - Mutation: opening the sample as a file made 4 tests fail; restored.
- Release build 0 warnings / 0 errors; full solution 1193/1193 in two consecutive runs (14:21, 14:25).

| Field | Value |
|---|---|
| Status | Done |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-30 14:07 / 14:26 +0500 |

## Completed: TASK-068 About/Diagnostics, release notes, notices, logging (Done 2026-10-30 13:54 +0500, Claude Code (Opus 5.5))
- `src/AssemblyStudio.App/Logging/FileLog.cs`:
  - `ILog`, `NullLog`, `FileLog`: `%LOCALAPPDATA%\AssemblyStudio\logs\assemblystudio-<timestamp>-<pid>-<n>.log`; each entry is appended and flushed.
  - Files hold at most 2 MB; the oldest are deleted to keep the total at 10 MB or less (NFR-012). Oversized entries are truncated; writing never throws.
  - Local only (no networking references in any application assembly, tested).
- Logged:
  - Startup.
  - Unhandled exceptions (UI thread, AppDomain, unobserved tasks).
  - Invalid settings file, recovery folder unusable, autosave failure, open/save errors, recent list and layout save failures (the four places marked "logging arrives with TASK-068").
- `src/AssemblyStudio.App/About/` (UI-018), Help ▸ Release Notes / Third-Party Notices / About:
  - Diagnostics text: app, assembler (version, origin, path, or why unusable), engine and DOS-services assemblies and versions, .NET, OS, settings and log paths. "Copy diagnostics" handles a busy clipboard.
  - Release notes from `RELEASE-NOTES.md` and notices from `THIRD-PARTY-NOTICES.txt` next to the app, plus each bundled tool's LICENSE (NASM's is installed now). A missing file is shown as "This file isn't installed: <path>", never invented.
  - The App project copies `RELEASE-NOTES.md` and `THIRD-PARTY-NOTICES.txt` from the repository root when they exist; neither exists yet (release procedure / TASK-102).
- New startup option `--log <folder>`; app-launching tests use temporary folders.
- **DEF-007 found and fixed:** the WPF test thread's `new App()` ran the real startup (hidden app instance using the user's folders). Added `App.ComposeOnStartup`; set the test `ShutdownMode` after `InitializeComponent`; removed the test-created files from the user's folders.
- **DEF-006 opened:** the flood regression test passed its 10 s limit once under full-suite load.
- Tests (20 new): `LoggingTests` (10), `AboutTests` (10, including the window and Help menu); `SettingsProcessTests` now also checks the real app's log (AC-063). Mutation: disabling the size trim failed the cap test.
- Release build 0 warnings / 0 errors; App 161/161; full solution 1175/1175 in two consecutive runs (13:50, 13:53).
- Observations for the user:
  - The app reports version 1.0.0, the build default; no version has been set yet (release procedure).
  - The backlog's "Notice content (TASK-091)" points to the samples task; the notices task is TASK-102.

| Field | Value |
|---|---|
| Status | Done |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-30 13:16 / 13:54 +0500 |

## Completed: TASK-067 Settings service and dialog (Done 2026-10-30 12:57 +0500, Claude Code (Opus 5.5))
- `src/AssemblyStudio.App/Settings/`:
  - `AppSettings`: assembler path (empty = bundled), font family/size, tab width, spaces, theme, instruction budget, number format; defaults (FR-079); `Sanitized` for hand-edited values.
  - `JsonSettingsService`: the `"settings"` object of settings.json, readable camelCase, atomic write, other sections kept, older files merged with defaults. An invalid file (not JSON, not an object, unreadable `settings`) is renamed to `settings.invalid-<timestamp>.json` before the layout and recent-files stores read it, and `InvalidFileBackup` drives the FR-081 notice.
  - `SettingsViewModel` + `SettingsWindow` (UI-017): assembler path with Browse and Check. Path validated by `NasmAssembler.ValidateConfiguredPath` and by running `nasm -v`; unusable paths are not saved (AC-015). Shows the assembler in use with origin and version. Editor and execution values validated with messages; Reset layout; Reset all with confirmation (FR-082).
  - `EditorAppearance`: `Font.Editor` / `FontSize.Editor` tokens (× Windows text scale).
- App:
  - Settings are loaded first; the theme preference and editor font come from settings and follow changes; File ▸ Settings and Ctrl+, open the dialog; the corrupt-settings notice shows after the window renders.
  - `tools\nasm\nasm.exe` + LICENSE are copied next to the app from `tools/nasm` when present (FR-079 zero configuration; the installer bundling stays with the release task).
- Not applied yet: tab width / spaces (the editor control arrives in TASK-064); instruction budget and number format are stored for TASK-080 / TASK-081 (the session-level budget, AC-054, is already tested in `DebugSessionTests`).
- Tests (31 new):
  - `SettingsTests` (25): persistence across restarts with other sections kept, 5 corrupt-file forms backed up, out-of-range values, default path, **defaults build the .COM template with the bundled NASM**, AC-015 invalid paths / fake exe / real copy via Browse, dialog save, 7 invalid values, Cancel, Reset all, Reset layout.
  - `SettingsUiTests` (3): dialog render and invalid-path error, labels with access keys and UIA names, Ctrl+, and the editor font.
  - `SettingsProcessTests` (1): the real app started with a corrupted file shows the notice and keeps the backup.
- Mutation check: disabling the backup rename or the assembler check made 10 tests fail; restored.
- Release build 0 warnings / 0 errors; full solution 1155/1155 in two consecutive runs (one earlier run hit DEF-001, see defects).

| Field | Value |
|---|---|
| Status | Done |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-30 12:24 / 12:57 +0500 |

## Completed: TASK-063 Crash recovery (Done 2026-10-30 12:17 +0500, Claude Code (Opus 5.5))
- The user chose TASK-063 because TASK-064 depends on the Blocked TASK-061.
- `src/AssemblyStudio.App/Documents/Recovery/`:
  - `RecoveryStore`: `%LOCALAPPDATA%\AssemblyStudio\recovery\<session>\`, one autosave JSON per unsaved document (atomic). The session lock file is held open while running; a folder whose lock can be taken belongs to a dead instance and is offered. A clean exit deletes its folder; another running instance is never offered.
  - `AutosaveService`: every 30 s, writes changed unsaved documents and removes saved or closed ones (NFR-011 allows 60 s).
- `App` startup: create the store; if abandoned documents exist, the UI-021 prompt (Restore / Discard) shows once the window has rendered. Restore opens them unsaved with their file, encoding and line endings; Discard opens an empty document; both delete the abandoned folders.
  - The autosave runs on a DispatcherTimer; the store is disposed on Exit. Smoke mode skips the prompt.
  - New startup options: `--recovery <folder>`, `--autosave-seconds <n>`.
- Other changes: `DocumentViewModel.Id` and an `isDirty` constructor parameter; `WorkspaceViewModel.Restore`; `IDialogService.AskRestore` (WPF: MessageDialog); 6 strings.
- Tests:
  - `RecoveryTests` (7): changed-only autosave, cleanup on save/close, recovery with encoding/EOL, clean exit, live instance not offered, empty/corrupt sessions, numbering.
  - **`CrashRecoveryProcessTests` (AC-050):** starts the real app, types through UI Automation, waits for the autosave, **kills the process**, restarts it, the prompt appears, Restore brings the text back. Passed 4 times.
  - The two app-launching tests now run in the non-parallel `AppProcessTests` collection: startup measured 20.8 s next to the kill test, versus 1.5 s alone.
- Release build 0 warnings / 0 errors; App 110/110; full solution 1124/1124 in two consecutive runs.

| Field | Value |
|---|---|
| Status | Done |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-30 11:56 / 12:17 +0500 |

## Completed: TASK-062 Document service and file workflow (Done 2026-10-30 11:51 +0500, Claude Code (Opus 5.5))
- `src/AssemblyStudio.App/Documents/`:
  - `TextFileCodec`: UTF-8 with or without BOM (ASCII counts as UTF-8); invalid UTF-8 → ANSI code page with a notice; CRLF/LF/Mixed detection; saves the same encoding, BOM and line endings; mixed files are kept as-is until normalized.
  - `AtomicFile`: temporary file in the same folder, flushed, then `File.Replace`/`Move`; the target is untouched on failure.
  - `FileErrors`: path + reason for missing file or folder, access denied, read-only, locked, disk full, invalid path, path too long.
  - `IDialogService`/`WpfDialogService` and `IRecentFilesStore` (`JsonRecentFilesStore` = "recentFiles" in settings.json, 10 entries).
  - `DocumentViewModel` and `WorkspaceViewModel`: New / New .COM program, Open (dialog, drag-drop, recent), Save / Save As, Close and window close with Save / Don't Save / Cancel, normalize line endings after confirmation.
- Shell:
  - Editor area = tab strip + one TextBox per document (keeps caret and undo per tab); the AvalonEdit editor replaces the TextBox in TASK-064.
  - UI-020 welcome page, encoding and mixed-EOL banners, encoding and EOL in the status bar, File menu and shortcuts, drag-and-drop, close prompt.
  - New themed `MessageDialog` (UI-019). The app starts with an Untitled document (FR-001).
- `build/generate-strings.ps1` regenerates `Shell/Strings.cs` from `Strings.resx`; the TASK-060 script was lost with an old scratchpad. 52 new strings.
- Tests (42 new):
  - `TextFileCodecTests`, `AtomicFileAndErrorsTests` (AC-004 fault injection mid-write, read-only, locked, messages).
  - `WorkspaceViewModelTests`: AC-002, 003, 004, 005, 006, 007, 008 (missing, locked, read-only, **no read permission via a deny ACL**, invalid path), 012, normalize.
  - `DocumentUiTests`: welcome/tabs, AC-006 caret and undo per tab, AC-005 window close Cancel, status bar, F6, menu state; renders `editor-template.png`, `editor-tabs.png`.
- Not automated: dropping files onto the window (WPF's DragEventArgs can't be constructed in tests; the handler calls the tested `OpenFiles`); AC-001 with network adapters disabled (QA, TASK-103).
- Release build 0 warnings / 0 errors; App 102/102; full solution 1116/1116 in two consecutive runs.

| Field | Value |
|---|---|
| Status | Done |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-30 11:28 / 11:51 +0500 |

## Blocked: TASK-061 Themes, tokens and contrast test (2026-10-30 11:27 +0500, Claude Code (Opus 5.5))
- `src/AssemblyStudio.App/Themes/`:
  - `Shared.xaml`: typography, spacing and sizes.
  - `Dark.xaml`, `Light.xaml`: all 32 color and syntax tokens; syntax added, it was missing from TASK-060's Dark.
  - `HighContrast.xaml`: every token → `SystemColors`, per §3.
  - `App.xaml` merges Shared, theme and styles.
- `Shell/ThemeManager.cs`: high contrast wins; preference System/Dark/Light, where System follows the Windows app mode. It replaces the theme dictionary live on Windows changes. The Settings override persistence is TASK-067.
- Tests: `ThemeTokenTests` (15):
  - Keys per theme; values equal the token table.
  - **Contrast 190/190 (95 per theme)**, the formula reproducing the documented 4.85 / 3.47 / 5.28.
  - High-contrast mapping, theme resolution, and live switching of an open window.
  - Renders `artifacts/ui/shell-empty-light.png` and `shell-empty-highcontrast.png`.
- **Not 166 pairs:** the SDLC-007 script with the exact 166-pair list was never committed. The test uses 190 pairs rebuilt from the description, a superset.
- Text scaling at 200 % is still covered by the TASK-060 render test.
- **Blocked:** AC-059 needs a manual check on real displays (100/125/150/200 %, two monitors) by the user (BLK-002). The checklist is in `docs/06-implementation/task-061-verification.md`.
- Release build 0 warnings / 0 errors; App 60/60; full solution 1074/1074 in two consecutive runs.

| Field | Value |
|---|---|
| Status | Blocked (BLK-002) |
| Owner | Claude Code (Opus 5.5) |
| Started / blocked | 2026-10-30 11:12 / 11:27 +0500 |

## Completed: TASK-054 Fault containment, fuzz and soak (Done 2026-10-29 16:47 +0500, Claude Code (Opus 5.5)), M3 complete
- `DebugSession` containment (NFR-010):
  - Engine, loader and machine exceptions → Error with `LastFault` ("Type: message", cleared by Reset) and an optional `faultLog` callback for the local log.
  - If the error snapshot can't be built from the faulty machine, it's published without the machine.
  - Each snapshot subscriber is called on its own; an exception is logged without changing state.
  - A throwing log is ignored.
  - Before, a throwing subscriber or a failing error snapshot would have thrown on the worker thread and ended the process.
- **DEF-005 found by the 30-minute soak and fixed:** an output flood of long strings hung a run. `ConsoleDevice` trimmed its 1 MiB snapshot delta with `List.RemoveAt(0)`. The delta is now a capped counter over the ring buffer; mutation-checked.
- Tests in `tests/AssemblyStudio.Debugging.Tests/Robustness/` (10):
  - Fault containment ×4.
  - Hostile programs ×4: AC-045 output flood (cap held, oldest trimmed, Stop 3 ms); long-string flood regression; IVT wiped (garbage vector taken, clean halt); self-modifying code.
  - Fuzz: 600 random images with random input; fixed seed.
  - Soak: duration from `ASSEMBLY_STUDIO_SOAK_MINUTES`, 0.25 min in the suite.
  - Plus 2 `ConsoleDeviceTests`.
- **30-minute soak (16:10–16:40):** 396,642 programs, 1,586,560 random commands, 0 faults, 0 hangs; managed memory 1.1 → 2.2 MiB; threads 18 → 18.
- Release build 0 warnings / 0 errors; full solution 1055/1055 in two consecutive runs.

| Field | Value |
|---|---|
| Status | Done |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-29 15:44 / 16:47 +0500 |

## Completed: TASK-053 Input routing, Ctrl-C delivery and replay (Done 2026-10-29 15:43 +0500, Claude Code (Opus 5.5))
- Core:
  - `IConsoleDevice.EnqueueInput(byte)` (ConsoleDevice already had it; NullConsoleDevice and the MachineTests double now implement it).
  - `InputRecord(AtInstruction, Value)`.
  - `IDebugSession.SendInput(bytes)`, `SendCtrlC()`, `RecordedInput`, `Replay(recording)`.
  - `InstructionsExecuted` is now "as of the latest snapshot".
- `DebugSession`:
  - Input, Ctrl-C (03h, D-015) and memory refreshes go through a separate inline queue that a run handles between instructions **without stopping**. Only Run, Pause, Stop, Reset, Replay and the steps interrupt.
  - This also fixes a TASK-051 bug: `WatchMemory` during a run paused the program.
  - Delivered input is recorded with the instruction count. A program waiting for input continues what it was doing: a run keeps running (a Step Over keeps its target), a step completes.
  - `Replay` reloads the program and delivers each byte at its recorded count; live input is ignored until Reset.
  - `InstructionsExecuted` is published with the snapshot, which removes an intermittent failure in TASK-052's Step Over test (it read the counter before the snapshot).
- Tests: `InputAndReplayTests` (11 new): waiting run and step continue on input; input during a run doesn't pause it; type-ahead; Ctrl-C in 01h → CtrlC termination and in 07h → character 03h; AC-047 (replay reproduces console, CPU, count, history, termination, recording) in a new session and in the same session; Reset clears the recording; watch-while-running regression.
- Release build 0 warnings / 0 errors; Debugging 50/50 in 5 consecutive runs; full solution 1043/1043 in two consecutive runs.

| Field | Value |
|---|---|
| Status | Done |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-29 15:28 / 15:43 +0500 |

## Completed: TASK-052 Breakpoints and Step Over (Done 2026-10-29 15:00 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- Core:
  - `Breakpoint`: `public readonly record struct Breakpoint(SourceLocation Location, bool IsBound, ushort? Offset = null, bool IsEnabled = true);`
  - `PauseReason.Breakpoint`: added to enum for breakpoint pause reporting (FR-052, AC-032).
  - `IDebugSession`: added `Breakpoints`, `ToggleBreakpoint`, `SetBreakpoint`, `RemoveBreakpoint`, `RebindBreakpoints`, `StepOver`.
- `src/AssemblyStudio.Debugging/DebugSession.cs`:
  - `_activeBreakpoints`: `bool[65536]` fast array lookup ensuring O(1) nanosecond pre-instruction check in `RunUntilStopped()`. Rebuilt and atomically swapped under lock on breakpoint mutations.
  - Breakpoint continuation past current IP: `_lastBreakpointHit` records hit location to prevent re-pausing on resume without executing.
  - Breakpoints persist across `Reset()` (FR-052).
  - `StepOver`: decodes current instruction with `IDisassembler.Decode`. For CALL/INT (`call`, `int`, `int3`, `into`), sets a temporary breakpoint at next instruction (`IP + Length`) and executes run loop until reached; pauses with `PauseReason.Step` (or `PauseReason.Breakpoint` if user breakpoint also present); stops early if user breakpoint hit inside procedure, user pause, budget exhaustion, runtime error, or termination. Non-CALL/INT instructions step one instruction (`PauseReason.Step`).
- Tests: `tests/AssemblyStudio.Debugging.Tests/BreakpointStepOverTests.cs` (12 new tests):
  - AC-032 (looping program with breakpoint before line 10, multiple loop iterations, removal and run to termination).
  - AC-033 (step over 20-instruction procedure to instruction after call; step over stopping early on breakpoint inside procedure).
  - Step over INT 21h, step over non-call instructions, step over stopping early on termination or runtime error.
  - Bound/unbound state and rebinding after rebuild, disabled breakpoint, reset persistence, entry point breakpoint, toggle/remove semantics.
- Verification: Release build 0w/0e; 40/40 Debugging tests; 39/39 Architecture tests; 1032/1033 solution tests (DEF-001 pre-existing).

| Field | Value |
|---|---|
| Status | Done |
| Owner | Antigravity (IDE, Gemini 3.8 Flash) |
| Started / finished | 2026-10-29 14:20 / 15:00 +0500 |

---

---|---|
| Status | In progress |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-29 13:46 +0500 |
| Milestone | M3 (Debug session, headless) |
| Goal | Publish immutable snapshots with CPU state, undefined mask, changes, console delta, location, current instruction, memory windows, stack window and history |
| Scope (in) | `WatchMemory` (≤ 4 windows × 4 KiB); immediate publish on step/pause, ≤ 30/s while running with not-current state; history ring buffer 1,000 |
| Out of scope | View models |
| Requirements | FR-046, FR-048, FR-053–FR-058, NFR-018, NFR-002; ADR-0007 v1.1; AC-048, AC-049 (session level) |
| Expected files | `src/AssemblyStudio.Debugging/Snapshots/`, `DebugSession.cs`, Core snapshot/disassembler types, tests |

---|---|
| Status | Idle / Ready for next task |
| Completed Milestones | M1 (engine), M2 (runtime, DOS, toolchain) |
| Milestone 3 progress | M3: Debug session (TASK-050 Done; 1/5) |
| Recommended next action | `/assembly-implement TASK-051` (Snapshots) or `/assembly-implement TASK-053` (input routing, Ctrl-C, replay; depends only on 050/036) |
| Also eligible | `TASK-061` (Themes) |
| Parallel user action | `TASK-006` (clean test VMs) can start any time; decide DEF-003 (.COM size limit 65,278 vs 65,280) |

---|---|
| Status | In progress |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-29 13:31 +0500 |
| Milestone | M3 (Debug session, headless) |
| Goal | Run the machine on a dedicated worker with a command channel and session states |
| Scope (in) | Run/Pause/Stop/Reset/StepInto, per-instruction cancellation, budget (default 10,000,000, configurable); session states as in ui-spec §4 |
| Out of scope | Breakpoints (TASK-052), snapshot detail (TASK-051), input commands (TASK-053) |
| Requirements | FR-035, FR-042, FR-049, FR-051, FR-067, NFR-004, NFR-015; ADR-0007 §2; AC-027, AC-030, AC-054 (session level) |
| Expected files | `src/AssemblyStudio.Debugging/`, Core `IDebugSession` (ADR-0002), `tests/AssemblyStudio.Debugging.Tests/` |

---|---|
| Status | Idle / Ready for next task |
| Completed Milestones | **M1 8086 engine** (TASK-010..020) and **M2 Runtime, DOS and toolchain** (TASK-030..036, TASK-040..044): all tasks Done |
| Recommended next action | `/assembly-test` (M2 milestone verification), then `/assembly-implement TASK-050` (M3: debug session worker) |
| Also eligible | `TASK-061` (Themes) |
| Parallel user action | `TASK-006` (clean test VMs) can start any time; decide DEF-003 (.COM size limit 65,278 vs 65,280) |

---|---|
| Status | In progress |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-29 13:03 +0500 |
| Milestone | M2 (Runtime, DOS and toolchain) |
| Goal | Assemble → load → run headless with scripted input and assert output, exit path and trace |
| Scope (in) | Harness API used by samples later; fixtures for hello, input echo, unsupported service, divide error, four exit paths |
| Out of scope | UI |
| Requirements | FR-039, FR-041, NFR-016, NFR-020; ADR-0008 §4; AC-021/022/023/029/031/064 (runtime level), AC-047 (replay, runtime level) |
| Expected files | `tests/AssemblyStudio.ProgramTests/` (new test project, added to `AssemblyStudio.slnx`) |

---|---|
| Status | Idle / Ready for next task |
| Completed Milestone | **Milestone 1: 8086 engine (headless)** (TASK-010..020 all Done) |
| Milestone 2 progress | M2: Runtime, DOS and toolchain (TASK-030..036, TASK-040..043 Done; 11/12) |
| Recommended next action | `/assembly-implement TASK-044` (Program-level test harness, last M2 task) |
| Also eligible | `TASK-061` (Themes) |
| Parallel user action | `TASK-006` (clean test VMs) can start any time; decide DEF-003 (.COM size limit 65,278 vs 65,280) |

---|---|
| Status | In progress |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-29 12:42 +0500 |
| Milestone | M2 (Runtime, DOS and toolchain) |
| Goal | Productize POC-002's source map and close its unverified cases |
| Scope (in) | `ISourceMap` from listing + map (macro → invoking line, include → file, sections); POC-002 fixtures + 3 verifiers as regression tests; new fixtures: ≥10-level nesting, nested includes in subfolders, `%rep`, section switch inside a macro. Failures narrow documented scope (FR-045) |
| Out of scope | Debugger use |
| Requirements | FR-028, FR-052, FR-053; ADR-0005 §5–6 |
| Expected files | `src/AssemblyStudio.Toolchain.Nasm/SourceMapping/`, Core `ISourceMap` (ADR-0002), `tests/AssemblyStudio.Toolchain.Nasm.Tests/fixtures/` |

---|---|
| Status | Idle / Ready for next task |
| Completed Milestone | **Milestone 1: 8086 engine (headless)** (TASK-010..020 all Done) |
| Milestone 2 progress | M2: Runtime, DOS and toolchain (TASK-030..036, TASK-040..042 Done; 10/12) |
| Recommended next action | `/assembly-implement TASK-043` (listing/map source map) |
| Also eligible | `TASK-061` (Themes) |
| Parallel user action | `TASK-006` (clean test VMs) can start any time; decide DEF-003 (.COM size limit 65,278 vs 65,280) |

---|---|
| Status | In progress |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-29 11:43 +0500 |
| Milestone | M2 (Runtime, DOS and toolchain) |
| Goal | Parse GNU diagnostics, define stable message codes, add the MASM hint and the .COM size check |
| Scope (in) | Diagnostics parser (incl. info and warning class); message catalog (code, what, where, next step) in resources; MASM heuristics; 65,280-byte limit |
| Out of scope | UI |
| Requirements | FR-021, FR-029, FR-030, FR-031, FR-066, NFR-024; ADR-0005 §4 |
| Expected files | `src/AssemblyStudio.Toolchain.Nasm/Diagnostics/`, Core `Diagnostic` (ADR-0002), `NasmAssembler` wiring, tests |

---|---|
| Status | Idle / Ready for next task |
| Completed Milestone | **Milestone 1: 8086 engine (headless)** (TASK-010..020 all Done) |
| Milestone 2 progress | M2: Runtime, DOS and toolchain (TASK-030..036, TASK-040 Done; 8/12) |
| Recommended next action | `/assembly-implement TASK-041` (Diagnostics, message catalog, MASM hint, size limit) |
| Also eligible | `TASK-042` (CPU-directive guard), `TASK-043` (listing/map source map), `TASK-061` (Themes) |
| Parallel user action | `TASK-006` (clean test VMs) can start any time |

---|---|
| Status | In progress |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-29 11:05 +0500 |
| Milestone | M2 (Runtime, DOS and toolchain) |
| Goal | `IAssembler` process handling: locate, version-check, sandboxed invocation, timeout, output cap, failure kinds |
| Scope (in) | Bundled vs configured path, version ≥ 3.02 warning; ArgumentList, temp dir, working dir = source folder (untitled: temp), 10 s timeout with tree kill, 1 MiB cap; unsaved documents assembled from a temp copy; build time measured (NFR-006) |
| Out of scope | Diagnostics parsing (TASK-041), CPU guard (TASK-042), listing/map parsing (TASK-043) |
| Requirements | FR-023, FR-024, FR-025, FR-026, FR-030, FR-095, NFR-014, NFR-006; ADR-0005 |
| Expected files | `src/AssemblyStudio.Toolchain.Nasm/`, Core assembler abstractions (ADR-0002 lists `IAssembler` in Core), `tests/AssemblyStudio.Toolchain.Nasm.Tests/` |

---|---|
| Status | Idle / Ready for next task |
| Completed Milestone | **Milestone 1: 8086 engine (headless)** (TASK-010..020 all Done) |
| Milestone 2 progress | M2: Runtime, DOS and toolchain (TASK-030..036 Done; 7/12). All DOS services done; NASM tasks 040–044 remain |
| Recommended next action | `/assembly-implement TASK-040` (NASM runner) |
| Also eligible | `TASK-061` (Themes) |
| Parallel user action | `TASK-006` (clean test VMs) can start any time |

---|---|
| Status | In progress |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-29 10:28 +0500 |
| Milestone | M2 (Runtime, DOS and toolchain) |
| Goal | INT 21h 0Ah (MVP editing keys) and the Ctrl-C/INT 23h protocol |
| Scope (in) | 0Ah buffer format, max 0, full → BEL, BS/DEL, Enter (CR only), Esc cancel; Ctrl-C checks in 01/02/08/09/0Ah (never 07h); default INT 23h terminates (Ctrl-C); program handler IRET/RETF protocol; ^S pause; ^P consumed and unsupported |
| Out of scope | Template keys F1–F7 (post-MVP) |
| Requirements | FR-039, FR-064, ADR-0004 §4, D-015 |
| Expected files | `src/AssemblyStudio.Os.Dos/DosServices.cs`, tests in `tests/AssemblyStudio.Os.Dos.Tests/` |

---|---|
| Status | Idle / Ready for next task |
| Completed Milestone | **Milestone 1: 8086 engine (headless)** (TASK-010..020 all Done) |
| Milestone 2 progress | M2: Runtime, DOS and toolchain (TASK-030..035 Done; 6/12) |
| Recommended next action | `/assembly-implement TASK-036` (DOS buffered input and Ctrl-C) |
| Also eligible | `TASK-040` (NASM runner), `TASK-061` (Themes) |
| Parallel user action | `TASK-006` (clean test VMs) can start any time |

---|---|
| Status | In progress |
| Owner | Claude Code (Opus 5.5) |
| Started | 2026-10-29 10:19 +0500 |
| Milestone | M2 (Runtime, DOS and toolchain) |
| Goal | INT 21h 01h, 07h, 08h (extended keys 00h + scan code) and the WaitingForInput mechanism |
| Scope (in) | Echo rules; extended-key 00h + scan code; empty queue → WaitingForInput with CS:IP on the INT, re-executed on input |
| Out of scope | Buffered input 0Ah and Ctrl-C/^S/^P handling (TASK-036) |
| Requirements | FR-039, FR-064, ADR-0004 §4–5 |
| Expected files | `src/AssemblyStudio.Os.Dos/DosServices.cs`, `tests/AssemblyStudio.Os.Dos.Tests/DosServicesTests.cs` |

---|---|
| Status | Idle / Ready for next task |
| Completed Milestone | **Milestone 1: 8086 engine (headless)** (TASK-010..020 all Done) |
| Milestone 2 progress | M2: Runtime, DOS and toolchain (TASK-030..034 Done; 5/12) |
| Recommended next action | `/assembly-implement TASK-035` (DOS character input services) |
| Also eligible | `TASK-040` (NASM runner), `TASK-061` (Themes) |
| Parallel user action | `TASK-006` (clean test VMs) can start any time |

---

## Completed: TASK-051 Snapshots (ADR-0007 v1.1) (Done 2026-10-29 14:06 +0500, Claude Code (Opus 5.5))
- Core:
  - `MachineSnapshot.HistoryDelta` is now `IReadOnlyList<HistoryEntry>`; it was an `int` count, which can't feed the history panel.
  - New `HistoryEntry(Cs, Ip, Bytes, Disassembly)`.
  - `IDisassembler.Decode(bytes, ip)` → `DecodedInstruction(Text, Length)` (Iced adapter): exact instruction bytes and real branch targets.
  - `IDebugSession`: `History`, `WatchMemory(seg, off, len)` and `UnwatchMemory(id)`.
- `src/AssemblyStudio.Debugging/Snapshots/`:
  - `ExecutionHistory`: an allocation-free ring of 1,000 (CS, IP, 6 bytes), disassembled only when published. An instruction that halts, terminates or waits is not recorded.
  - `SnapshotContent`: the current instruction with its source line (program segment only), memory windows and a 16-word stack window.
- `DebugSession`:
  - Takes `(loadMachine, IDisassembler, ISourceMap?)`.
  - Snapshots now carry the current instruction (null while running), windows (≤ 4 × 4 KiB; a watch posts an immediate refresh), stack and history delta. `History` holds the last 1,000.
  - While running it publishes at most 30 per second (measured 27.7/s).
- Tests: `SnapshotTests` (13 new):
  - Snapshot = machine (NFR-018), exact bytes and real branch target, source line (program segment only), undefined flags.
  - AC-049 (20 steps → 20 ordered entries with disassembly), a halted instruction not in history, the 1,000 cap.
  - AC-048 (running snapshots not current, ≤ 30/s, refreshed on pause).
  - Watch limits, immediate refresh and immutability; step latency 0.24 ms median / 0.33 ms p95 (NFR-002 budget 100 ms).
  - Two TASK-050 tests were updated to the per-snapshot delta semantics.
- Release build 0 warnings / 0 errors; full solution 1021/1021 in two consecutive runs.

| Field | Value |
|---|---|
| Status | Done |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-29 13:46 / 14:06 +0500 |

## Completed: TASK-050 Debug session worker and commands (Done 2026-10-29 13:41 +0500, Claude Code (Opus 5.5))
- Core: `IDebugSession` (Run, Pause, StopSession, Reset, StepInto; State, PauseReason, InstructionBudget, InstructionsExecuted, LastStep, LatestSnapshot, SnapshotPublished) and `PauseReason` (Loaded, Step, UserPause, BudgetExhausted).
  - `Stop` was renamed `StopSession` because of CA1716 (`Stop` is a VB keyword).
- `src/AssemblyStudio.Debugging/DebugSession.cs`:
  - Dedicated background worker with a command channel. Takes a factory for a freshly loaded `IMachine`, so Debugging stays Core-only; Reset calls the factory again.
  - Run checks for a new command before every instruction. The budget is per Run (default 10,000,000, configurable); exhausting it gives Paused(BudgetExhausted), and Run continues with a new budget.
  - Halted / Terminated / WaitingForInput come from the step outcome. Engine or loader exceptions become Error, and Reset recovers.
  - Snapshots after each command: CPU, undefined flags, memory changes since the last snapshot (one entry per byte, drained every 65,536 instructions), console delta, state, termination path/exit.
  - State, PauseReason and LastStep are read from the published snapshot record, so they always agree with it.
- Tests: `DebugSessionTests` (15 new), on real machines from hand-assembled images. AC-027, AC-030 (Pause 3 ms, Stop 0.4 ms measured), AC-054, budget continue, memory-change folding, engine and loader faults, Dispose.
- Release build 0 warnings / 0 errors; full solution 1009/1009.

| Field | Value |
|---|---|
| Status | Done |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-29 13:31 / 13:41 +0500 |

## Completed: TASK-044 Program-level test harness (Done 2026-10-29 13:25 +0500, Claude Code (Opus 5.5))
- New test project `tests/AssemblyStudio.ProgramTests` (added to `AssemblyStudio.slnx`).
- `ProgramHarness`: build with the bundled NASM → load into a fresh machine (real engine, runtime, router, DOS, console) → run headless.
  - Inputs: scripted input (one chunk per wait) or replay of a recording; an instruction budget; an optional CPU trace.
  - Result `ProgramRun`: end kind, output, termination, halt/DOS error, recorded input, machine.
- NASM program fixtures in `Programs/`: hello, echo (0Ah), open-file (3Ch/3Dh), divide-error, the four exit paths, and an endless loop.
- Tests (14): AC-021, AC-022, AC-023, AC-029, AC-031 and AC-064 at runtime level; AC-047 replay (identical trace and output; diverging input diverges); chunked input; budget.
- Also fixed **DEF-004**, flaky toolchain timing tests from TASK-040 under full-suite load: a separate `NasmOptions.VersionCheckTimeout`, and the build-time test moved to a non-parallel best-of-3 class.
- Release build 0 warnings / 0 errors. Full solution 994/994 (13:24) and a confirming re-run.
- **Milestone M2 complete.**

| Field | Value |
|---|---|
| Status | Done |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-29 13:03 / 13:25 +0500 |

## Completed: TASK-043 Listing/map parsing and source map (Done 2026-10-29 12:58 +0500, Claude Code (Opus 5.5))
- Core: `ISourceMap` (`Find(offset)`, `FirstOffsetOf(file, line)`), `SourceLocation`, `SourceMapping`; `BuildResult.SourceMap` (init, additive).
- `src/AssemblyStudio.Toolchain.Nasm/SourceMapping/`: `NasmListing` (rows; nesting marker found by pattern), `NasmMapFile`, `NasmSourceMap`.
  - Macro → outermost invoking line (flagged); `%rep` → own lines; include → file (working dir first, then includer folder); sections via map starts.
  - A section line inside a macro *definition* is ignored.
  - The temp copy maps to the document (or null when untitled).
- `NasmAssembler` builds the map after a successful build. If the map can't be read: no map plus BLD010 (new catalog code, warning).
- Fixtures in `tests/AssemblyStudio.Toolchain.Nasm.Tests/fixtures/`: POC-002 01–07 plus new 08 (12-level nesting), 09 (nested includes in subfolders), 10 (`%rep`), 11 (section switch in a macro), 12 (wrapped data, `.bss`).
- **Finding:** NASM's nesting marker moves one column left for `<9>` and deeper on rows without bytes. POC-002's fixed column would read those rows as level 0. Fixed, and mutation-checked.
- Verification note: `docs/06-implementation/task-043-verification.md`. All four ADR-0005 unverified cases now work; the remaining limits are documented there.
- Tests: `SourceMapTests` (3 verifiers × 10 fixtures + 14 behavior tests); toolchain 124/124. Release build 0 warnings / 0 errors; full solution 980/980.

| Field | Value |
|---|---|
| Status | Done |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-29 12:42 / 12:58 +0500 |

## Completed: TASK-042 CPU-directive guard (Done 2026-10-29 12:35 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- `src/AssemblyStudio.Toolchain.Nasm/Diagnostics/CpuDirectiveGuard.cs`:
  - Scans preprocessed NASM output (`-Xgnu -E`, FR-027, ADR-0005 §3) for CPU directives requesting a processor level other than 8086.
  - Accurately tracks NASM `%line <line>[+<step>] [<file>]` directives across blank lines, instruction blocks, macro expansions, and include files.
  - Matches normalized directive lines `^\s*\[\s*cpu\s+(?<level>[^\]\s]+)\s*\]\s*$` (case-insensitive) while ignoring string literals like `db "[cpu 386]"`.
  - Maps main source file to `documentPath` (or `null` if untitled); resolves included files relative to `workingDirectory`.
  - Allows `8086`; rejects `186`, `286`, `386`, `486`, `586`, `686`, `PENTIUM`, `x64`, etc.
- Catalog additions (`BuildMessages.cs` and `BuildMessages.resx`):
  - Stable code `BLD009` (`UnsupportedCpu`, severity `DiagnosticSeverity.Error`).
  - Text templates for `BLD009_What` and `BLD009_Next`.
- `NasmAssembler.BuildAsync`:
  - Runs `nasm -Xgnu -E <source>` prior to binary assembly.
  - Halts build immediately on non-8086 CPU directives returning `BuildOutcome.Failed`, empty binary, and `BLD009` diagnostics without assembling machine code.
  - Passes through timeout (`BLD002`), crash (`BLD003`), and cancellation (`BLD005`).
- Tests:
  - 8 new unit tests in `CpuDirectiveGuardTests` (allowed 8086, non-8086 rejection, macro reporting, include reporting, untitled/saved mapping, multiple directives, string literal safety, blank line counting).
  - 5 new integration tests in `BuildDiagnosticsTests` verifying AC-018 instruction rejection with `ASM001`, `cpu 386` bypass rejection with `BLD009`, hidden-in-macro bypass rejection with `BLD009`, include file bypass rejection with `BLD009`, and explicit `cpu 8086` successful assembly.
  - Toolchain test suite now **79/79 passed**.
- Solution verification:
  - Release build `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors**.
  - `AssemblyStudio.ArchitectureTests` → **39/39 passed**.
  - `dotnet test --solution AssemblyStudio.slnx -c Release` → **934/935 passed** (1 pre-existing defect DEF-001 in App.Tests).

| Field | Value |
|---|---|
| Status | Done |
| Owner | Antigravity (IDE, Gemini 3.8 Flash) |
| Started / finished | 2026-10-29 12:15 / 12:35 +0500 |

## Completed: TASK-041 Diagnostics, message catalog, MASM hint, size limit (Done 2026-10-29 11:53 +0500, Claude Code (Opus 5.5))
- Core: `Diagnostic(Code, Severity, What, File, Line, NextStep)` and `DiagnosticSeverity`; `BuildResult.Diagnostics` (init, additive).
- `src/AssemblyStudio.Toolchain.Nasm/Diagnostics/`:
  - `NasmDiagnosticParser`: GNU format, all severities, message kept as written incl. `[-w+class]`. A temp copy maps to the document (or null when untitled); other files are kept. Unparseable lines stay only in the raw output.
  - `BuildMessages` (+ `BuildMessages.resx`): catalog codes ASM001–003 and BLD001–008, each with severity, what, next step.
  - `MasmHint`: `.model/.code/.data/…`, `assume`, `name proc/endp/segment/ends`, `@data`; comments ignored; NASM `segment .text` not matched.
- `NasmAssembler`:
  - Parsed diagnostics, plus build messages for missing (BLD001), timeout (BLD002), crash (BLD003), failure without a source diagnostic (BLD004), cancelled (BLD005), version warning (BLD008).
  - The MASM hint (BLD007) is added on failed builds.
  - The 65,280-byte .COM limit fails the build with BLD006.
- Tests: 22 new in `BuildDiagnosticsTests` + code checks in `NasmAssemblerTests` (toolchain 58/58). Covers AC-013 (toolchain part), AC-014, AC-020, and AC-055 catalog completeness.
- Release build 0 warnings / 0 errors; full solution 914/914.
- Found **DEF-003**: `ComLoader` allows only 65,278 bytes (its message says 65,280). Not changed; needs a decision.

| Field | Value |
|---|---|
| Status | Done |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-29 11:43 / 11:53 +0500 |

## Completed: TASK-040 NASM runner (Done 2026-10-29 11:16 +0500, Claude Code (Opus 5.5))
- Core (ADR-0002 lists these in Core): `IAssembler` (`Check()`, `BuildAsync`), `BuildRequest`, `BuildResult`, `BuildOutcome` (Succeeded, Failed, AssemblerMissing, TimedOut, Crashed, Cancelled), `AssemblerInfo`, `AssemblerOrigin`, `AssemblerStatus`.
- `src/AssemblyStudio.Toolchain.Nasm/`: `NasmAssembler`, `NasmOptions`, `ProcessRunner`.
  - Locating: the configured path overrides the bundled one, with no fallback when it is invalid. `ValidateConfiguredPath` requires a full local `.exe` path; network paths are rejected before any lookup.
  - Version: `nasm -v` is checked before each build (cached per file) → Ready / OlderVersion (< 3.02, warning) / UnknownVersion / Missing / InvalidPath / NotRunnable.
  - Process: no shell, `ArgumentList`, per-build temp folder (deleted afterwards), working directory = source folder (untitled: temp), 10 s timeout and cancellation with process-tree kill, each stream capped at 1 MiB (flagged). Unsaved text goes through a temp copy that keeps the file name. A crash is an NTSTATUS exit code.
  - Build time is measured (`Duration`).
- **Implementation deviation from ADR-0005 §2 (flag text):** `[map all]` without a file name, so the map goes to stdout, separate from the diagnostics on stderr. NASM's map directive can't take a path containing spaces (measured: quoting fails). Intent unchanged: nothing is written next to the source.
- Tests: 35 new in `NasmAssemblerTests` (toolchain 36/36), covering AC-015, AC-016 and AC-019 (toolchain parts), malicious file names, the output cap, cancellation, and a 1,000-line build in about 110 ms (NFR-006 ≤ 2 s).
- Release build 0 warnings / 0 errors; full solution 892/892.

| Field | Value |
|---|---|
| Status | Done |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-29 11:05 / 11:16 +0500 |

## Completed: TASK-036 DOS buffered input and Ctrl-C (Done 2026-10-29 11:04 +0500, Claude Code (Opus 5.5))
- `src/AssemblyStudio.Os.Dos/DosServices.cs`: INT 21h AH=0Ah with the MVP editing keys (STRIN.ASM).
  - Buffer format: max including CR; count without CR; chars + CR. The buffer is written on Enter. Max 0 returns at once.
  - A character that doesn't fit → BEL. Control characters echo as `^X`; TAB echoes as spaces.
  - BS/DEL erase the last character's cells. LF echoes CR LF and is not stored. Enter echoes CR only.
  - Esc prints `\` CR LF and restarts the line.
  - Extended (template) keys are consumed and ignored in the MVP.
  - The line in progress is kept across `WaitingForInput` steps.
- Break checks in 01h/02h/08h/09h/0Ah (never 07h): ^C → Ctrl-C protocol; ^S → pause until the next key (discarded; ^C still breaks); ^P consumed and ignored (unsupported). Other keys stay queued.
- Ctrl-C protocol: prints `^C` CR LF, then INT 23h through the router.
  - Default vector → terminate (`TerminationPath.CtrlC`, exit 00h).
  - A program handler is entered with a real frame returning to the INT 21h.
  - IRET, or RETF with CF clear → the function is retried. RETF with CF set → terminate. The handler's live CF decides (CTRLC.ASM); the stacked FLAGS word is removed.
  - A program INT 23h with the default vector also terminates.
- Core/Runtime (additive): `IConsoleDevice.TryPeekInput` (replay-aware in `ConsoleDevice`), so the break check doesn't consume other keys; `TerminationPath.CtrlC`.
- Tests: 30 new in `DosServicesTests` (Os.Dos 72/72), including the AC-023 runtime part; 2 new peek tests in `ConsoleDeviceTests`.
- Release build 0 warnings / 0 errors. Full solution 862/862. The first full run stalled (DEF-002, not reproduced).

| Field | Value |
|---|---|
| Status | Done |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-29 10:28 / 11:04 +0500 |

## Completed: TASK-035 DOS character input services (Done 2026-10-29 10:24 +0500, Claude Code (Opus 5.5))
- `src/AssemblyStudio.Os.Dos/DosServices.cs`: INT 21h AH=01h (read with echo), 07h and 08h (read without echo).
  - AL returns the input byte.
  - 01h echoes through the DOS output path: TAB echoes as spaces with AL=09h; Enter echoes CR only.
  - Extended keys are queued as 00h + scan code, so the call returns 00h and the next call returns the scan code. 01h echoes both bytes, as DOS does.
  - Empty queue → `WaitingForInput` with the state untouched (CS:IP on the INT); the INT re-executes when input arrives.
  - 07h and 08h differ only in the Ctrl-C check, which is TASK-036.
- Tests: 12 new in `DosServicesTests`, covering each function, extended keys, wait/resume and a program that waits, echoes, prints and exits.
- Release build 0 warnings / 0 errors. Os.Dos 42/42; full solution 829/829.

| Field | Value |
|---|---|
| Status | Done |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-29 10:19 / 10:24 +0500 |

## Completed: TASK-034 DOS output and termination services (Done 2026-10-29 10:16 +0500, Claude Code (Opus 5.5))
- `src/AssemblyStudio.Os.Dos/DosServices.cs` (`IOsServices`):
  - INT 21h AH=02h, 09h, 00h and 4Ch, plus INT 20h, with DOS 3.3/4.0 semantics.
  - DOS's own output column: TAB → spaces to the next multiple of 8, CR → 0, BS → −1, DEL and bytes below 20h raw.
  - AL returns: 02h → the last character output (20h for TAB); 09h → 24h.
  - INT 20h and AH=00h require CS = PSP (otherwise DOS001). A near RET to PSP:0000 is reported as `ReturnToPsp`.
  - Deviation: 09h with no '$' in the segment → DOS002 halt with no output (real DOS would print forever).
- Core (additive):
  - `StepOutcome.Terminated` and `StepResult.Termination` (`ProgramTermination(TerminationPath, ExitCode)`) in the new `ProgramTermination.cs`.
  - `MachineSnapshot.ExitCode` doc aligned with AC-064.
- Tests: 30 in `tests/AssemblyStudio.Os.Dos.Tests` (29 new in `DosServicesTests` + the existing skeleton test), run through the real `Cpu8086` + `InterruptRouter` + `ConsoleDevice`.
  - Covers each function and AC-064 (all four paths: hand-assembled .COM images through `ComLoader`).
  - Covers AC-022, runtime part (hello world → output + 4Ch exit 0).
- Release build 0 warnings / 0 errors; full solution 817/817.

| Field | Value |
|---|---|
| Status | Done |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-29 10:01 / 10:16 +0500 |

## Completed: TASK-033 Console device (Done 2026-10-28 18:05 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- Implemented `InputEvent` in `src/AssemblyStudio.Runtime/Console/InputEvent.cs`:
  - Immutable record struct `InputEvent(ulong InstructionCount, byte Value)` stamping input bytes with CPU execution count (ADR-0004 §5, NFR-016).
- Implemented `ConsoleDevice` in `src/AssemblyStudio.Runtime/Console/ConsoleDevice.cs`:
  - Output buffer capped at 1 MiB (`DefaultMaxCapacity = 1024 * 1024` bytes, NFR-015, AC-045).
  - Circular buffer implementation guaranteeing O(1) appending and oldest content trimming with zero reallocations.
  - Tracking metrics: `TotalBytesWritten`, `TotalBytesTrimmed`, `HasTrimmed`, `BufferLength`.
  - Snapshot delta API: `TakeOutput()` returns pending output string and clears delta (ADR-0007 §2).
  - Chronological buffer decoding: `ToByteArray()` and `GetAllText()` using CP437.
  - DOS column counter (ADR-0004 §6, R-DOS §2):
    - CR (0Dh) resets column to 0.
    - BS (08h) decrements column without dropping below 0.
    - TAB (09h) advances column to next multiple of 8 (`(col + 8) & ~7`).
    - BEL (07h) increments `BelCount` without advancing column.
    - Printable characters (>= 20h) advance column by 1.
    - Control characters (< 20h) do not advance column.
  - Input queue with recording and deterministic replay (NFR-016, AC-047):
    - Live mode: `EnqueueInput`, `TryReadInput` dequeues and appends stamped `InputEvent` to `RecordedInputs`.
    - Replay mode: `SetReplayInputs` loads recorded events; `TryReadInput` releases bytes only when `CurrentInstructionCount` reaches the stamped instruction count.
  - Reset / Clear: `Clear()` resets output, delta, column, BEL, and live queue, preserving recorded log (FR-063).
- Tests in `tests/AssemblyStudio.Runtime.Tests/ConsoleDeviceTests.cs`:
  - 22 unit tests (Runtime.Tests now 79/79 passed):
    - Delta draining and full text retrieval.
    - Column counter arithmetic (CR, BS non-underflow, TAB stops 8/16/24, printable, LF/control bytes).
    - BEL character counting and bell ringing.
    - AC-045 cap and trim at arbitrary capacity and under 1 MB flood.
    - Input queue FIFO ordering and empty checks.
    - Input recording with instruction counts.
    - AC-047 deterministic replay verifying step-synchronized byte release.
    - Clear and reset state preservation.
- Verification:
  - `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors**.
  - `AssemblyStudio.Runtime.Tests.exe` → **79/79 passed** in 1.10s.
  - `AssemblyStudio.ArchitectureTests.exe` → **39/39 passed** in 1.58s.
  - Core suites (Core, Os.Dos, Nasm, Debugging, Content) → **18/18 passed**.
  - Engine quick regression suite → **80/80 passed** in 12.8s.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | Antigravity (IDE, Gemini 3.8 Flash) |
| Started / finished | 2026-10-28 17:58 / 2026-10-28 18:05 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-033 |

---

## Completed: TASK-032 Interrupt router and runtime error policy (Done 2026-10-28 17:53 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- Implemented `IvtStubs` in `src/AssemblyStudio.Runtime/Interrupts/IvtStubs.cs`:
  - Default reserved stub segment `0xF000` (physical `0xF0000`).
  - Stubs for all 256 interrupt vectors allocated at `0xF000:(vector * 4)`.
  - Populates each stub address with an `IRET` instruction opcode (`0xCF`, ADR-0004 §2–3).
  - Populates IVT (0000:0000h..03FFh) with `(offset, segment)` pairs pointing to the reserved stubs.
  - `IsDefaultVector(memory, vector)` accurately distinguishes default vectors from program-installed vectors.
- Implemented `RuntimeHaltInfo` and `RuntimeErrorPolicy` in `src/AssemblyStudio.Runtime/Interrupts/`:
  - Conforms to the What/Where/Next contract (FR-038, FR-040, FR-066, NFR-024, AC-029, AC-031, AC-055).
  - CPU exceptions (INT 0..4):
    - Vector 0: `RTE001` Divide error (AC-031).
    - Vector 1: `RTE002` Single-step trap.
    - Vector 2: `RTE003` Non-maskable interrupt.
    - Vector 3: `RTE004` Breakpoint interrupt (INT 3).
    - Vector 4: `RTE005` Overflow trap (INTO with OF=1).
  - Unsupported services and interrupts:
    - Vector 0x21: `RTE021` naming function (e.g. `INT 21h function 3Dh (AH=3Dh) is unsupported`, AC-029).
    - General INT: `RTE020` naming interrupt and function AH.
- Implemented `InterruptRouter` in `src/AssemblyStudio.Runtime/Interrupts/InterruptRouter.cs`:
  - Implements `IInterruptRouter`.
  - Program-installed vector check: returns `null`, letting CPU vector through IVT with real 8086 semantics (push Flags/CS/IP, clear IF/TF, ADR-0004 §3).
  - CPU exceptions with default vector halt with `HaltReason.RuntimeError` and populate `LastHalt` (FR-038).
  - OS services delegation (`IOsServices.HandleService`), passthrough of `Executed`, `WaitingForInput`, or `UnsupportedService` halts.
  - Unsupported interrupts and functions halt with `HaltReason.UnsupportedService` and populate `LastHalt` (FR-040, NFR-013).
  - Auto-wired in `Machine.cs` (`router.Machine = this;`).
- Tests in `tests/AssemblyStudio.Runtime.Tests/InterruptRouterTests.cs`:
  - 18 unit tests (Runtime.Tests now 57/57 passed):
    - IVT stub initialization and IRET opcodes across all 256 vectors.
    - Program-installed vector detection.
    - AC-031 divide error halt at CS:IP with what/where/next runtime error message.
    - AC-029 unsupported INT 21h function 3Dh halt naming function 3Dh with what/where/next, no host file access.
    - CPU exceptions (INT 1, 2, 3, 4) with stable codes RTE002..RTE005.
    - Unsupported software interrupts (INT 10h, 13h, 16h, 2Fh) with RTE020.
    - Program-installed vector for INT 21h and divide error vectors through IVT and executes guest handler.
    - OS services delegation (Executed, WaitingForInput, UnsupportedService, fallback).
- Verification:
  - `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors**.
  - `AssemblyStudio.Runtime.Tests.exe` → **57/57 passed** in 0.72s.
  - `AssemblyStudio.ArchitectureTests.exe` → **39/39 passed** in 1.47s.
  - Core suites (Core, Os.Dos, Nasm, Debugging, Content) → **18/18 passed**.
  - Engine quick regression suite (Determinism, Performance, Disassembler) → **80/80 passed** in 14.7s.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | Antigravity (IDE, Gemini 3.8 Flash) |
| Started / finished | 2026-10-28 17:41 / 2026-10-28 17:53 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-032 |

---

## Completed: TASK-031 .COM loader and PSP (Done 2026-10-28 16:55 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- Implemented `ProgramSegmentPrefix` in `src/AssemblyStudio.Runtime/Loading/ProgramSegmentPrefix.cs`:
  - 256-byte (0x100) structure initialized at `pspSegment:0000h` (FR-034, research R-DOS §3):
    - `00h`: `CD 20` (INT 20h termination).
    - `02h`: Top of memory segment (`0x1800` for 64 KiB block).
    - `05h`: CALL far to `PSP:0050h` (`9A 50 00 [psp_lo] [psp_hi]`).
    - `0Ah..15h`: Saved INT 22h (terminate), INT 23h (Ctrl-C), INT 24h (critical error) vectors copied from IVT.
    - `16h`: Parent PSP segment (`pspSegment`).
    - `18h..2Bh`: Job file table (20 bytes: handles 0..4 open, 5..19 unused 0xFF).
    - `2Ch`: Environment segment (`0x0000`).
    - `50h`: `CD 21 CB` (`INT 21h; RETF`).
    - `5Ch..6Bh`: Unopened FCB 1 (zeroed).
    - `6Ch..7Fh`: Unopened FCB 2 (zeroed).
    - `80h..FFh`: Command tail / DTA (`00 0D` when empty; length byte, characters, CR terminator when provided; max 126 chars).
- Implemented `ComLoader` and `ComLoadResult` in `src/AssemblyStudio.Runtime/Loading/`:
  - Places PSP at 0800h (`DefaultProgramSegment = 0x0800`).
  - Loads binary image at `0800:0100h` (`DefaultEntryPoint = 0x0100`).
  - Max image size enforced: `MaxComImageSize = 65,278` bytes (`0xFEFE = 0xFFFE - 0x0100`), ensuring the program does not overwrite the stack return word.
  - Places word `0000h` at `SS:SP` (`0800:FFFEh`), providing the return target for a near `RET` to `PSP:0000h` (`INT 20h`) to terminate cleanly (AC-064).
  - Clears memory journal after load so initial baseline state generates zero change highlights (FR-056).
  - Initializes CPU registers: `CS=DS=ES=SS=0800h`, `IP=0100h`, `SP=FFFEh`, `AX=BX=CX=DX=SI=DI=BP=0000h`, `Flags=0002h` (AC-021, ADR-0004 §2).
- Tests in `tests/AssemblyStudio.Runtime.Tests/ComLoaderTests.cs`:
  - 9 comprehensive unit tests verifying AC-021 verbatim, byte-level PSP field verification against the research table, command tail formatting and truncation, IVT vector preservation, near RET termination path, max size boundary checks, and custom segment support.
- Verification:
  - `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors**.
  - `AssemblyStudio.Runtime.Tests.exe` → **39/39 passed** in 4.6s.
  - `AssemblyStudio.ArchitectureTests.exe` → **39/39 passed** in 0.53s.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | Antigravity (IDE, Gemini 3.8 Flash) |
| Started / finished | 2026-10-28 16:45 / 2026-10-28 16:55 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-031 |

---

## Completed: TASK-030 Runtime memory, journal and machine (Done 2026-10-28 16:40 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- Implemented `RealModeMemory` in `src/AssemblyStudio.Runtime/Memory/RealModeMemory.cs`:
  - Implements `IMemory` with exactly 1 MiB physical address space (`1024 * 1024` bytes, NFR-015, FR-033).
  - 20-bit physical address calculation: `((segment << 4) + offset) & 0xFFFFF` (modulo 1 MiB wrapping, no A20 on 8086, FR-059, ADR-0004 §2).
  - Word access wrapping within 16-bit segment: `Read16` and `Write16` wrap offset + 1 to 0x0000 at segment boundary (e.g. offset 0xFFFF has low byte at 0xFFFF, high byte at 0x0000 of same segment).
  - Little-endian byte ordering.
  - Per-step write journal tracking `MemoryChange(PhysicalAddress, OldValue)`:
    - `TakeJournal()` atomically snapshots and clears the current journal.
    - `Rollback(changes)` and `RollbackJournal()` revert writes in reverse chronological order restoring pre-write values without generating new journal entries (FR-056).
    - `ExtractChangedRanges(changes)` extracts and merges contiguous, non-overlapping sorted `AddressRange` spans with deduplication.
  - Direct memory access helpers: `ReadPhysical`, `WritePhysical`, `LoadBytes` (unjournaled bulk loading for loader/tests), `CopyTo`, and `Reset`.
- Implemented `AddressRange` in `src/AssemblyStudio.Runtime/Memory/AddressRange.cs`:
  - Contiguous interval `[StartAddress, StartAddress + Length)` with `EndAddress`, `Contains`, and `CanMergeWith`.
- Implemented `NullConsoleDevice` in `src/AssemblyStudio.Runtime/Console/NullConsoleDevice.cs`:
  - Safe default fallback for `IMachine.Console` discarding output and reporting empty input queue.
- Implemented `Machine` in `src/AssemblyStudio.Runtime/Machine.cs`:
  - Implements `IMachine` composing `ICpu`, `IMemory`, `IConsoleDevice`, and optional `IInterruptRouter` and `IOsServices` (ADR-0004 §2).
  - `Step()` helper executing `Cpu.StepInstruction()`.
- Tests in `tests/AssemblyStudio.Runtime.Tests/`:
  - 30 unit tests in `RealModeMemoryTests.cs` and `MachineTests.cs`:
    - 1 MiB boundary, address wrap, aliasing, little-endian word access, word segment wrapping at 0xFFFF offset, combined 20-bit + segment wrap.
    - Journal recording for byte and word writes, `TakeJournal` clearing and persistence across steps.
    - Contiguous range coalescing, disjoint ranges, deduplicated repeat writes, empty input.
    - Rollback of single, disjoint, and repeat overwrites at the same address.
    - Unjournaled `LoadBytes`, span `CopyTo`, and `Reset`.
    - Machine composition, default fallbacks, step delegation, and null console behavior.
- Verification:
  - `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors**.
  - `AssemblyStudio.Runtime.Tests.exe` → **30/30 passed** in 0.54s.
  - `AssemblyStudio.ArchitectureTests.exe` → **39/39 passed** in 0.53s.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | Antigravity (IDE, Gemini 3.8 Flash) |
| Started / finished | 2026-10-28 16:30 / 2026-10-28 16:40 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-030 |

---

## Completed: TASK-020 Engine conformance and performance gate (Done 2026-10-28 16:00 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- Executed full 8086 hardware oracle suite:
  - 283 SingleStepTests oracle files covering all in-scope 8086 instructions pass at **100.0%** (~566,000 cases).
  - Secondary 8088 oracle dataset verified against 17 opcode files across all categories (17,000 cases at 100%).
  - Full 256 opcode table verified: 210 Supported, 7 Prefix, 39 Unsupported. All 39 unsupported opcodes (including 186/286/386 extensions, coprocessor ESC, port I/O, SALC) halt with `UnsupportedInstruction` with byte-identical pre-step state preserved (FR-037).
- Benchmarked on reference machine (D-014: Intel Core i5-8265U):
  - **Step + Register Snapshot Latency (NFR-002):** Mean **0.178 µs**, p50 **0.200 µs**, p95 **0.300 µs**, p99 **0.300 µs**, Max 474.7 µs (OS jitter). Budget: <= 100 ms (100,000 µs); engine consumption is **0.0002%** of budget.
  - **Worst-case Snapshot Latency:** Mean **91.9 µs** per full 1 MiB memory copy.
  - **Free-Run Throughput (NFR-003):** **6.42 – 7.29 M instructions/sec** across 3 runs of 50M instructions (Target: >= 1.00 M/s; **6.4x – 7.3x headroom**).
- Deterministic Execution (NFR-016, AC-047):
  - Verified in `DeterminismTests.cs`: Running complex multi-instruction routines (arithmetic, logic, shifts, conditional branches, subroutine calls, stack manipulation, REP string moves) produces 100% bit-for-bit identical CPU states, flags, instruction IPs, and memory traces across runs.
- Conformance Report (FR-036, AC-058):
  - Authored formal report `TR-2026-10-28-01` in `docs/07-testing/engine-conformance-report.md`.
- Tests: 611/611 Engine tests passed; 39/39 Architecture tests passed; Release build 0 warnings, 0 errors.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | Antigravity (IDE, Gemini 3.8 Flash) |
| Started / finished | 2026-10-28 15:35 / 2026-10-28 16:00 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-020 |

---

## Completed: TASK-019 Disassembler adapter (Iced) (Done 2026-10-28 15:24 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- Implemented in `src/AssemblyStudio.Engine.I8086/Disassembly/IcedDisassembler.cs`:
  - `IDisassembler` implementation using `Iced 1.21.0` (MIT, pinned in `Directory.Packages.props`) with 16-bit decoding.
  - NASM formatter configured with `SpaceAfterOperandSeparator = true`, `UppercaseHex = false`, and `ShowUselessPrefixes = true`.
  - Static helper `(string? Text, int Length) DisassembleInstruction(ReadOnlySpan<byte> bytes, ushort ip = 0)` for zero/low allocation decoding with relative branch resolution.
  - Strict 8086-only filtering:
    - Rejects invalid encodings (`instruction.IsInvalid` or length 0).
    - Rejects 386+ segment prefixes (`Register.FS`, `Register.GS`).
    - Validates CPU features: requires all `instruction.CpuidFeatures` to be `CpuidFeature.INTEL8086` or `CpuidFeature.INTEL8086_ONLY`, filtering out 186+, 286+, 386+ extensions (e.g. `PUSHA`, `ENTER`, `SHL r, imm`, 32-bit registers/addressing).
    - Filters out 386+ prefixes (`0x66` operand size, `0x67` address size, `0x64` FS, `0x65` GS).
    - Rejects undocumented/unsupported MVP opcodes (e.g. `0xD6` SALC halts engine per ADR-0003 §4 and returns `null` from disassembler).
  - Resolved ADR-0003 unverified claim in `docs/decisions/ADR-0003-execution-engine.md`: verified `Iced 1.21.0` on .NET 10, confirmed `INTEL8086_ONLY` tags `Code.Popw_CS`, and verified that Iced's 16-bit decoder treats `0x0F` as invalid (lone byte) or 286+ two-byte escape, producing `null` in `IcedDisassembler` which matches the engine's `UnsupportedInstruction` halt.
- Tests in `tests/AssemblyStudio.Engine.I8086.Tests/IcedDisassemblerTests.cs`:
  - 28 documented 8086 instructions across all categories verified for NASM text and length agreement.
  - 9 prefix combinations (REP, REPE, REPNE, CS/ES/SS/DS segment overrides) tested.
  - Relative branch target calculations using IP.
  - Rejection of 186+ opcodes, 286/386+ opcodes and prefixes, and undocumented opcodes.
  - Length agreement test across 17 SingleStepTests oracle files (thousands of test cases): 100% agreement between Iced decoded length and oracle instruction byte length.
  - ADR-0003 verification test.
  - 76/76 disassembler tests passed.
- Architecture rules: 39/39 passed (`AssemblyStudio.ArchitectureTests`).
- Solution tests: 687/688 passed (1 pre-existing DEF-001 App smoke test failure). Release build 0 warnings, 0 errors.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | Antigravity (IDE, Gemini 3.8 Flash) |
| Started / finished | 2026-10-28 14:45 / 2026-10-28 15:24 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-019 |

---

## Completed: TASK-018 Flag ops, misc and the full opcode table (Done 2026-10-28 14:06 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- Implemented in `src/AssemblyStudio.Engine.I8086/Cpu8086.FlagsMisc.cs` and `Cpu8086.cs`:
  - Flag operations: CLC (F8), STC (F9), CMC (F5), CLD (FC), STD (FD), CLI (FA), STI (FB).
  - Processor control: HLT (F4) halts execution reporting `StepOutcome.Halted` and `HaltReason.Halt`, with IP advanced past HLT.
  - Synchronization: WAIT / FWAIT (9B) is a no-op on 8086 without 8087 coprocessor (advances IP by 1, flags/registers untouched).
  - Prefix LOCK (F0) is ignored per ADR-0003 §4 in `Decoder.ParsePrefixes`.
  - Coprocessor ESC (D8-DF) and Port I/O (E4-E7, EC-EF) explicitly unsupported in MVP, halting with `UnsupportedInstruction` and state preserved byte-identical (FR-037).
  - Undocumented aliases (0F, 60-6F, C0-C1, C8-C9, D6, F1) halt with `UnsupportedInstruction`.
  - Created `OpcodeTable.cs` enumerating and classifying all 256 opcode bytes into Supported (206), Prefix (7), and Unsupported (43).
  - Created `docs/06-implementation/opcode-support.md` documenting complete opcode space, policies, Group ModR/M arms, and full 256-opcode reference table for help (FR-045).
- Tests: 7 SingleStepTests oracle files at 100% (F5, F8, F9, FA, FB, FC, FD; 283 oracle files total) + 40 hand tests in `FlagsMiscAndOpcodeTableTests.cs` (including exhaustive 256 opcode table agreement and ModR/M group arm tests); all 513 engine tests passed.
- Release build 0 warnings, 0 errors.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | Antigravity (IDE, Gemini 3.8 Flash) |
| Started / finished | 2026-10-28 13:43 / 2026-10-28 14:06 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-018 |

---

## Completed: TASK-017 String instructions and REP prefixes (Done 2026-10-28 13:28 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- Implemented in `src/AssemblyStudio.Engine.I8086/Cpu8086.Strings.cs` and `Cpu8086.cs`:
  - MOVSB/MOVSW (A4/A5), CMPSB/CMPSW (A6/A7), STOSB/STOSW (AA/AB), LODSB/LODSW (AC/AD), SCASB/SCASW (AE/AF).
  - Prefix support: F2 (REPNZ / REPNE) and F3 (REP / REPZ / REPE).
  - Segment rules: Destination is always [ES:DI] (unoverridable); Source defaults to [DS:SI] and respects segment overrides (CS/DS/ES/SS).
  - Direction flag: DF=0 increments SI/DI by 1 (byte) or 2 (word); DF=1 decrements SI/DI by 1 (byte) or 2 (word); 16-bit address wrap.
  - Flags: MOVS, STOS, LODS leave flags unchanged; CMPS and SCAS compute arithmetic flags via subtraction (CF, AF, ZF, SF, PF, OF).
  - Loop behavior: CX=0 with REP prefix is a no-op (advances IP past instruction without touching registers, memory or flags); REPE stops on mismatch (ZF=0); REPNE stops on match (ZF=1); without REP prefix executes single iteration without touching CX.
  - Documented decision on per-iteration step semantics: In `ICpu.StepInstruction`, an instruction with a REP prefix executes as a single, complete instruction step until termination, advancing IP past the instruction.
- Tests: 10 SingleStepTests oracle files at 100% (A4, A5, A6, A7, AA, AB, AC, AD, AE, AF; 276 oracle files total) + 16 hand tests in `StringInstructionTests.cs`; all 466 engine tests passed.
- Release build 0 warnings, 0 errors.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | Antigravity (IDE, Gemini 3.8 Flash) |
| Started / finished | 2026-10-28 13:05 / 2026-10-28 13:28 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-017 |

---

## Completed: TASK-016 Control transfer and interrupts (Done 2026-10-28 12:45 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- Implemented in `src/AssemblyStudio.Engine.I8086/Cpu8086.ControlTransfer.cs` and `Cpu8086.Arithmetic.cs`:
  - 70-7F conditional jumps: all 16 conditions, 8-bit sign-extended displacement, 16-bit IP wrap, flags unchanged.
  - E3 JCXZ: jumps on CX=0 without flag modification.
  - E0-E2 Loops: LOOP, LOOPE/LOOPZ, LOOPNE/LOOPNZ decrement CX without touching flags; jump conditions verified.
  - EB, E9, EA: JMP short, near, and far.
  - E8, 9A: CALL near and far, pushing return addresses and segment appropriately.
  - C2, C3, CA, CB: RET near/far with optional immediate byte stack popping.
  - FF /2-/5: Indirect near and far CALL/JMP; mod=3 halts UnsupportedInstruction.
  - CC (INT 3), CD (INT imm8), CE (INTO on OF=1): Router interception evaluated first; null router vectors through IVT with FLAGS/CS/IP push and IF/TF clear.
  - CF (IRET): Pops IP, CS, and FLAGS with canonical 8086 reserved bits.
  - Fixed IVT entry calculation from `vector * 2` to `vector * 4` (2-byte IP + 2-byte CS per vector in real hardware).
- Tests: 37 SingleStepTests oracle files at 100% (266 oracle files total) + 37 hand tests in `ControlTransferTests.cs`; all 440 engine tests passed.
- Defects: DEF-001 opened for pre-existing `MainWindowSmokeTests.View_menu_opens_with_shortcuts_and_reset_layout` in App.Tests.
- Release build 0 warnings, 0 errors.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | Antigravity (IDE, Gemini 3.8 Flash) |
| Started / finished | 2026-10-28 12:00 / 2026-10-28 12:45 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-016 |

---

## Completed: TASK-060 WPF shell and layout (Done 2026-10-28 11:31 +0500, Claude Code (Opus 5.5))
- Main window per ui-spec §2 in `src/AssemblyStudio.App/Shell/` (MVVM, CommunityToolkit.Mvvm 8.4.2 per ADR-0007): menus (UI-002 skeleton, every command with access key and shortcut; later-task commands disabled), command bar, status bar (UI-016, app version), Learn / editor / machine column / bottom tab group + memory/stack regions with keyboard-resizable splitters, empty states only (no machine data). F6/Shift+F6 region cycling, View ▸ Go to (Ctrl+Alt+R/F/M/K/O/P/U/H/B/L), region toggles, Reset Layout; compact layout below 1280 px; layout persisted in `settings.json` "layout"; 2 px focus ring; PerMonitorV2 manifest; Windows text scaling applied to font tokens; strings in `Strings.resx`; dark tokens only (TASK-061 owns themes).
- Tests (App.Tests, 41): view-model layout rules, settings-file store, options/text-scale, UI smoke on the real XAML (window opens, F6 cycles,
  Go to focuses, hidden region returns focus, View menu opens), in-process renders for review (`artifacts/ui/`, not committed), and a real-exe
  startup measurement (NFR-005). Verification notes: `docs/06-implementation/task-060-verification.md`.
- Release build 0 warnings, 0 errors. Solution tests 465/465 passed.

| Field | Value |
|---|---|
| Status | Done (all DoD items met; partial items documented in the verification notes) |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-28 11:02 / 2026-10-28 11:31 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-060 |

---

## Completed: TASK-015 Multiply, divide, BCD and conversions (Done 2026-10-28 10:25 +0500, Claude Code (Opus 5.5))
- Started by OpenCode (GLM-5.3-Flash Free) on 2026-10-27 (oracle analysis scripts `poc/task015-*.ps1`, untracked, no engine code); OpenCode
  hit its usage limit and the user asked Claude Code to continue ("please continue opencode task its limit reach").
- `src/AssemblyStudio.Engine.I8086/Cpu8086.MulDiv.cs` (new partial class): MUL/IMUL/DIV/IDIV (F6/F7 /4-/7), AAA/AAS/DAA/DAS, AAM/AAD, CBW/CWD in new `Cpu8086.MulDiv.cs`. Division follows the 8086 CORD microcode loop, so divide-error FLAGS pushed on the stack match the real chip; divide error (overflow, divisor 0, AAM 0) goes through the router as vector 0, else real vectoring with the 8086 return address (next instruction). 8086 quirks oracle-verified: IDIV rejects -128/-32768, REP negates the IDIV quotient, AAA/AAS adjust AL and AH separately, DAA/DAS high-digit threshold 9Fh when AF was set, DAS low-digit borrow does not set CF. Undefined-flag masks reported per step. 16 oracle files at 100% (229 total) + 21 hand tests incl. AC-031 engine part.
- `Cpu8086.cs`: dispatch for 27/2F/37/3F/98/99/D4/D5; INT vectoring factored into `VectorInterrupt(vector, flagsToPush, returnIp)` (shared by
  INT imm8 and the divide error). `Cpu8086.Logic.cs`: `ExecF6F7Group` arms 4-7. The undocumented F6/F7 /1 TEST alias still halts (FR-037).
- Tests: `MulDivBcdTests.cs` (21); the TASK-014 test that expected MUL to halt was removed (MUL is now in scope).
- No new dependencies. Release build 0 warnings, 0 errors. Solution tests 425/425 passed.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | Claude Code (Opus 5.5) (taken over from OpenCode (GLM-5.3-Flash Free) on the user's request) |
| Started / finished | 2026-10-27 (OpenCode, time not recorded) / 2026-10-28 10:25 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-015 |

---

## Completed: TASK-014 Logic, shift and rotate instructions (Done 2026-10-27 16:12 +0500, OpenCode (GLM-5.3-Flash Free))
- `src/AssemblyStudio.Engine.I8086/Cpu8086.Logic.cs` (new partial class): AND/OR/XOR in rm/r families (bases 08/20/30, arms 0-3 via the shared TASK-013 `Arith` core — `opcode>>3` maps onto the existing `ArithKind`) and acc-imm forms (0C/0D, 24/25, 34/35); TEST (84/85 rm,reg; A8/A9 acc,imm; F6/F7 arm 0 rm,imm) — AND flag semantics with the result discarded, AF untouched; NOT (F6/F7 arm 2) with no flags; shifts/rotates D0-D3 all /reg.
- 8086 count semantics: count NOT masked (masking arrived with the 186), count 0 changes nothing at all (not even flags), OF set only for count 1 (oracle-verified: ROL/RCL/SHL = MSB(result)^CF; ROR = bit7(result)^bit6(result); RCR = MSB(orig)^MSB(result); SHR = MSB(orig); SAR = 0), count>1 leaves OF untouched (metadata masks exclude it: rotates D2/D3 0xF7FF, shifts 0xF7EF), rotates never touch SF/ZF/PF/AF, logic ops clear CF/OF (mask 0xFFEF excludes AF only).
- **Undocumented D0-D3 /6 = SETMO** (the backlog's "/6 handling decision", oracle-verified over all 2000+2000+2000 cases named "setmo"/"setmoc"): operand becomes all ones (0xFF/0xFFFF), no-op at CL count 0; flags set to the real chip's raw values (SF=1, ZF=0, PF=1, CF=OF=AF=0) — the metadata mask 0xF72A officially reports them undefined.
- `Cpu8086.cs`: dispatch cases for the logic families, 84/85, A8/A9, D0-D3, F6/F7; `ExecF6F7Group` (arms 0/2/3) and `ExecShiftGroup`. `ExecNeg` refactored to `ExecNegCore(m, cursor, wide)`. The undocumented F6/F7 /1 TEST alias and MUL/DIV arms 4-7 halt `UnsupportedInstruction` with state unchanged (FR-037; /1 not in approved scope).
- **Oracle harness**: 58 TASK-014 files added to `SingleStepOracleTests` → **100% pass on all 213 oracle files** (71 + 84 + 58), first run.
- 23 deterministic hand tests (`LogicShiftRotateTests.cs`): logic CF/OF clears, TEST discards (rm,reg / acc,imm / rm,imm), NOT no-flags, count=1 OF rules for all six shift/rotate ops, rotates CF-only, CL=0 no-op (byte-identical state), CL=2 OF untouched, SHR word, SETMO byte/word/CL=0, FR-037 halts.
- No new dependencies. Release build: 0 warnings, 0 errors. Solution tests: 389/389 passed.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | OpenCode (GLM-5.3-Flash Free) |
| Started / finished | 2026-10-27 15:45 / 16:12 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-014 (goal, scope, tests, DoD) |
| Procedure | `docs/sdlc/procedures/assembly-implement.md` (invoke with `/assembly-implement TASK-014`) |
| Depends on | TASK-011 (Done ✔) |

**Parallel user action:** TASK-006 (clean test VMs) can start any time.

---

## Completed: TASK-013 Arithmetic instructions (Done 2026-10-27 14:59 +0500, OpenCode (GLM-5.3-Flash Free))
- `src/AssemblyStudio.Engine.I8086/Cpu8086.Arithmetic.cs` (new partial class): ADD/ADC/SUB/SBB/CMP in all three shapes — rm/r families (bases 00/10/18/28/38, arms 0-3), acc-imm (arms 4/5: AL,imm8 / AX,imm16), and the immediate groups 80/82 (rm8, imm8; 82 = undocumented alias of 80), 81 (rm16, imm16), 83 (rm16, sign-extended imm8) with the reg field selecting the operation; INC/DEC r16 (40-4F, all INC/DEC flags except CF); INC/DEC rm (FE/FF arms 0/1); NEG (F6/F7 arm 3, halts for other arms).
- Exact 8086 flags: CF carry/borrow out; OF signed overflow (width-aware, bit 7 for 8-bit); AF carry/borrow out of bit 3; SF top bit; ZF on zero; PF even parity of the low 8 bits. INC/DEC leave CF unchanged; NEG sets CF unless the operand is 0 and overflows only on 0x80/0x8000; CMP computes SUB flags and discards the result. TF/IF/DF and reserved bits pass through; everything touched is defined (`FlagBit.None` report).
- **AC-025 hand test verbatim**: `add al, bl` with AL=7Fh, BL=01h → AL=80h, OF=1, SF=1, ZF=0, AF=1, PF=0, CF=0 (flags word 0x0A92).
- `Cpu8086.cs`: arithmetic cases added to dispatch; the FF group now dispatches arms 0/1 (INC/DEC rm16) alongside arm 6 (PUSH, TASK-012) — `ExecPushRm16` became `ExecFfGroup`; FE is its own group handler (`ExecFeGroup`, arms other than 0/1 halt unsupported).
- **Oracle harness**: 84 TASK-013 files added to `SingleStepOracleTests` (30 family + 16 INC/DEC r16 + 32 group 80-83 all /reg + NEG F6.3/F7.3 + rm INC/DEC FE.0/FE.1/FF.0/FF.1) → **100% pass on all 155 oracle files** (71 TASK-012 + 84 TASK-013).
- 20 deterministic hand tests (`ArithmeticTests.cs`): AC-025, ADC/SBB carry/borrow chains, INC/DEC CF-untouched, NEG of 0/0x80/word, CMP discards, 83 sign-extension, group imm writes to memory, ES override, 16-bit SF/ZF/OF edges, FE/FF out-of-scope arms halt with byte-identical state (FR-037).
- No new dependencies. Release build: 0 warnings, 0 errors. Solution tests: 308/308 passed.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | OpenCode (GLM-5.3-Flash Free) |
| Started / finished | 2026-10-27 14:00 / 14:59 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-013 (goal, scope, tests, DoD) |
| Procedure | `docs/sdlc/procedures/assembly-implement.md` (invoke with `/assembly-implement TASK-013`) |
| Depends on | TASK-011 (Done ✔) |

**Parallel user action:** TASK-006 (clean test VMs) can start any time.

---

## Last completed: TASK-012, Data transfer instructions (Done 2026-10-27 13:55 +0500, OpenCode (GLM-5.3-Flash Free))
- `src/AssemblyStudio.Engine.I8086/Cpu8086.DataTransfer.cs` (new partial class): MOV all forms — rm/reg (88-8B), segment registers 8C/8E with the 8086 `reg&3` field aliasing (ES/CS/SS/DS), moffs A0-A3, reg-imm B0-BF, rm-imm C6/C7 (every reg field behaves as /0); XCHG 86/87 + AX-form 90-97 (90 = NOP); LEA 8D, LES C4, LDS C5 (memory forms; mod=11 is undefined and halts `UnsupportedInstruction` with state unchanged); XLAT D7 (DS default, override honoured); LAHF 9F (flags low byte verbatim), SAHF 9E (`(AH&0xD5)|0x02`); PUSH/POP reg 50-5F and seg 06/07/0E/16/17/1E/1F; POP rm 8F (every reg field behaves as /0); PUSH rm via FF/6 (other FF forms still unsupported); PUSHF 9C (FLAGS verbatim), POPF 9D (`(popped|0xF002)&~0x0028` — 8086 reserved bits forced to canonical form).
- Oracle-confirmed 8086 quirks baked in and hand-tested: PUSH SP pushes the NEW (decremented) SP — including the register form of FF/6; POP SP (5C and 8F rm4) loads SP with the popped value; PUSHF pushes FLAGS verbatim; POPF forces the canonical reserved bits.
- `Cpu8086.cs`: now `sealed partial`; dispatch is a switch expression returning `StepResult?` (null = unsupported/undefined → state restored, halt — FR-037 intact). Executed results carry the instruction's start address (CS:IP at entry, prefixes included); handlers advance IP via the `Executed(endIp)` helper.
- **Oracle harness** (`tests/.../SingleStepOracleTests.cs`): 71 TASK-012 opcode files, every case: load initial regs/ram into a fresh `FakeMemory`, one `StepInstruction()`, compare all 14 registers (flags via per-case mask resolved from `metadata.json` opcode → reg-subkey when present), compare every `final` ram byte, fail on unexpected writes → **100% pass on all 71 files**.
- 30 deterministic hand tests (`DataTransferTests.cs`): PUSH SP, POP SP (5C + 8F), segment moves incl. aliasing, LAHF/SAHF, PUSHF/POPF reserved bits, XLAT+override, LEA (+mod=11 halt), LDS/LES, moffs+override, C6/C7 alias regs, XCHG mem/reg and AX/SP, NOP, word access at offset 0xFFFF.
- No new dependencies. Release build: 0 warnings, 0 errors. Solution tests: 204/204 passed (includes new App.Tests project that exists in the tree, not touched by this task).

---

## Last completed: TASK-011, Engine skeleton: decode, ModR/M, memory access (Done 2026-10-27 12:45 +0500, OpenCode (GLM-5.3-Flash Free))
- `src/AssemblyStudio.Engine.I8086/`: `Cpu8086 : ICpu` — pure decode (state untouched until a decision), opcode dispatch with only `CD nn` (INT imm8) in this task as the interception-contract carrier; every other opcode (including aliases `0F`, `60–6F`, `C0/C1/C8/C9`, `D6`) halts `Halted(UnsupportedInstruction)` with CS:IP restored to the instruction start and full state byte-identical (FR-037, AC-028 engine part).
- `Decoder` (internal, pure): prefix parsing (segment overrides `26/2E/36/3E` last-wins, `F2/F3` REP capture, `F0` LOCK ignored), full 8086 ModR/M effective-address table with default segments (rm 2/3 and rm 6 mod≠0 → SS, else DS), disp8 sign-extension, disp16, `mod 00 rm 110` direct displacement, 16-bit EA and IP fetch wrap.
- Interrupt contract tested three ways: handled result returned as-is; `WaitingForInput` pass-through (CS:IP stays on the INT); `null`/no router → real vectoring (push FLAGS/CS/return-IP with 16-bit SP wrap, IF/TF cleared, CS:IP loaded from the IVT).
- 37 deterministic hand tests in `tests/AssemblyStudio.Engine.I8086.Tests` (full-state assertions, raw bytes, no assembler); `FakeMemory` double implementing the documented IMemory semantics (1 MiB, 20-bit wrap, word wrap within segment).
- No new dependencies (Iced deferred to TASK-019). No implementations of instruction families (TASK-012+).
- Release build: 0 warnings, 0 errors. Solution tests: 103/103 passed (9 test projects).

---

## Last completed: TASK-010, Core abstractions v1 (Done 2026-10-27 12:21 +0500, OpenCode (GLM-5.3-Flash Free))
- `src/AssemblyStudio.Core/`: `FlagBit`, `CpuState` (record struct, 14 ushort fields), `StepOutcome`/`HaltReason`/`StepResult`, `IMemory` (+ `MemoryChange` write-journal drain), `IInterruptRouter` (`StepResult? Intercept`, null = vector through IVT), `ICpu` (`State`/`SetState`/`StepInstruction`), `IDisassembler`, `IConsoleDevice`, `IMachine`, `IOsServices` (`HandleService(vector, state, IMachine)`), `SessionState`/`CurrentInstruction`/`MemoryWindow`/`StackWindow`/`MachineSnapshot` (ADR-0007 v1.1 fields, types only). XML docs on every public abstraction, citing ADR/FR.
- No implementations added (out of scope): `IAssembler`/`ISourceMap` (TASK-040/043) and `IDebugSession` (TASK-050) intentionally not defined here.
- 8 model tests in `tests/AssemblyStudio.Core.Tests`: CpuState equality/hash/with-expressions/value-copy, MachineSnapshot immutability (CPU advancing after publish leaves the snapshot unchanged), v1.1 field shape.
- Renamed `ICpu.Step()` → `StepInstruction()` (CA1716 reserved keyword, warnings-as-errors). No suppressions added.
- Release build: 0 warnings, 0 errors. Solution tests: 66/66 passed (9 test projects).

---

## Last completed: TASK-005, Bundled NASM tooling layout (Done 2026-10-27 12:00 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- `build/fetch-nasm.ps1`: Fetches and extracts pinned NASM 3.02 win64 binary into `tools/nasm/` (git-ignored per ADR-0005).
- Verifies archive SHA-256 (`161d0bfa...`), extracts `nasm.exe`, `ndisasm.exe`, and `LICENSE` (BSD-2-Clause).
- Verifies extracted file hashes and writes deterministic `tools/nasm/SHA256SUMS.txt` and `tools/nasm/SOURCE.txt` (no timestamps).
- `NasmLocator` test helper in `tests/AssemblyStudio.Toolchain.Nasm.Tests` with 6 passing tests: locating nasm, verifying version 3.02, ndisasm locator, BSD-2-Clause license & source metadata, SHA-256 manifest integrity, and actionable failure on missing tool.
- Release build: 0 warnings, 0 errors. Solution tests: 58/58 passed (9 test projects). `build/coverage.ps1`: exit 0.

---

## Earlier: TASK-004, Coverage tooling (Done 2026-10-27 10:48 +0500, Claude Code)
- `coverlet.MTP` 10.1.0 (MIT) pinned in `Directory.Packages.props`, referenced by every test project via `Directory.Build.props`.
- `build/coverage.ps1`: per-project Cobertura, merged per product assembly, `artifacts/coverage/summary.md`; exit 1 on a failed or missing run.
- Skeleton report: 9 Cobertura files from 9 projects, all assemblies "n/a (no coverable lines)".
