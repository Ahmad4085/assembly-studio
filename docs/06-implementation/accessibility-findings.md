# Editor accessibility findings (TASK-069)

| Field | Value |
|---|---|
| Document | Implementation verification record |
| Task | TASK-069 (FR-093, NFR-022; ADR-0006 Unverified item "Narrator support in AvalonEdit"; UI-003; AC-044 editor part) |
| Last updated | 2026-11-02 |
| Author | Claude Code (Opus 5.5) |

Evidence labels as in `docs/sdlc/guides/sdlc-documentation.md`. Narrator reads what UI Automation (UIA) exposes. Part A records what
the editor exposes, measured by tests. Part B is the manual Narrator check, which only a person can do.

## Part A: what the editor exposes to UI Automation

| Item | Result | Evidence |
|---|---|---|
| Which element has keyboard focus | AvalonEdit's inner `TextArea`, inside the `CodeEditor` (a `TextEditor`). Both report control type **Document** | [Measured] in-process peers (`TextEditorAutomationPeer`, `TextAreaAutomationPeer`) |
| Name of the focused element | **Gap found and fixed.** The `TextArea` had no name, so a screen reader would announce an unnamed document. It now takes the editor's name (the document title, for example `hello.asm`), and follows renames. It also has a help text naming F9, Ctrl+K Ctrl+I and Ctrl+M | [Measured] `EditorAccessibilityTests.The_focused_part_of_the_editor_is_a_named_document…`; mutation (binding removed) fails both tests |
| Patterns of the focused element | **Text** and **Value**. The outer `TextEditor` has Value only. The Text pattern is the one Narrator uses to read by character, word and line | [Measured] in-process probe |
| Text at the caret | `GetSelection` returns the caret position; expanding it to a **line** gives `mov ax, 1`, to a **word** gives `ax`; a selection returns its text (`mov`). Single selection | [Measured] `The_focused_part_of_the_editor…` |
| Events for caret moves and selections | `TextAreaAutomationPeer` subscribes to `Caret.PositionChanged` and `TextArea.SelectionChanged` and raises an automation event in `OnSelectionChanged`. A UIA client **outside the process** (as Narrator is) receives `TextSelectionChanged` when a range is selected through the Text pattern | [Measured] reflection over AvalonEdit 6.3.1.120's peer; `A_screen_reader_outside_the_app_finds_the_editor_by_name_and_hears_selection_changes` (real app process) |
| Text changes | The peer raises no text-changed event. Narrator's own typing echo reads typed characters; whether it re-reads edited lines is part of the manual check | [Measured] reflection (no other `RaiseAutomationEvent` call in the peer) |
| Editor chrome | Document tabs are named by `AutomationName` (the title and the unsaved state); the tab close button is "Close tab"; the gutter is "Breakpoints and messages" (TASK-066); banners are text with live regions where they change (stale session); the current-instruction strip is "Current instruction" (TASK-083); the line-number margin is not focusable and has no name, as in Visual Studio | [Measured] existing UI tests (`DocumentUiTests`, `AdornmentTests`, `SessionPanelsTests`) and `MainWindow.xaml` review |

**Assessment from Part A:** the structure Narrator needs is present: a named, focusable Document with the Text pattern,
line/word/character ranges and caret/selection events. ADR-0006 calls Narrator support in AvalonEdit "unverified" because of an
open upstream report; Part A doesn't find a structural cause for it. Whether Narrator actually speaks the expected text is
confirmed by the user's manual check in Part B (overall result).

## Part B: manual Narrator walkthrough (AC-044 editor part)

**Status: RUN by the user, 2026-11-02 12:24 +0500.** The user's report, verbatim: "i check narrates its work and commit". Result: **Narrator reads the editor (overall pass)**. The user didn't report the steps one by one, so the table records the overall result; a step-by-step check can be added later if a problem shows up.

Setup:
1. Run Assembly Studio (`src/AssemblyStudio.App/bin/Release/net10.0-windows/AssemblyStudio.App.exe`, or the installed app).
2. Open the hello sample: Learn ▸ Samples ▸ Hello, World ▸ Open a copy. The editor tab is `hello.asm (copy)`.
3. Start Narrator: **Ctrl+Win+Enter**. Stop it later with the same keys.

| # | Do | Narrator should say (roughly) | Result |
|---|---|---|---|
| 1 | Click in the editor, or press F6 until the editor has focus | "hello.asm (copy), document" (the name), then the current line | Reported working (overall, not itemized) |
| 2 | Press **Down arrow** a few times | Each new line's text, e.g. "mov ah, 09h" | Reported working (overall, not itemized) |
| 3 | Press **Right arrow** and **Left arrow** | The character at the caret | Reported working (overall, not itemized) |
| 4 | Press **Ctrl+Right arrow** | The next word | Reported working (overall, not itemized) |
| 5 | Press **Shift+End** | "selected" and the selected text | Reported working (overall, not itemized) |
| 6 | Narrator key (Caps Lock or Insert) **+ Up arrow** (read current line) | The whole current line | Reported working (overall, not itemized) |
| 7 | Type `nop` at the start of an empty line | Each typed character (typing echo) | Reported working (overall, not itemized) |
| 8 | Press **Ctrl+Z** | Something indicating the text changed (Narrator may say nothing; note what it says) | Reported working (overall, not itemized) |
| 9 | Press **F9** on a code line, then Narrator key + Up | The line (the breakpoint itself is shown in the gutter; note whether anything is said) | Reported working (overall, not itemized) |
| 10 | Press **Tab** after **Ctrl+M** | Focus leaves the editor; Narrator names the next control | Reported working (overall, not itemized) |

How to judge: steps 1–6 are the minimum for NFR-022 (reading lines, the caret and the selection). If any of them says nothing or
says the wrong text, record what was heard; I then raise it as a requirement/design issue (OQ) as ADR-0006 says, rather than
patching AvalonEdit internals here (out of scope unless the fix is small).

## Part C: step announcements (TASK-085, AC-044)

**Automated (measured):** each step, pause, halt and end raises one announcement through a polite live region
(`AnnouncementRegion`), built from the real state: "Ready at line 2.", "Paused at line 3. Changed: AX, IP.", "Halted: RTE001
Divide error…", "Waiting for keyboard input. Type in the console.", "Program ended with exit code 0.". The state badge no longer
announces itself, so nothing is read twice. Evidence: `AnnouncementTests` (3); mutation (no "Changed" part) fails. The keyboard-only
workflow (AC-043) passed with real keys on 2026-11-02 12:52 +0500 (`UiSmokeTests.AC_043…`, opt-in, D-018): focus was in the app,
on screen and keyboard-focused after every step.

**Manual Narrator check: RUN by the user, 2026-11-02 13:11 +0500.** The user's report, verbatim: "i check narrator says paused and changed, commit". Result: the step
announcement is spoken ("Paused …" with "Changed …"). Checks 1 and 3 (after the build, after F5) weren't reported separately.
1. Run Assembly Studio, open the Hello sample (Learn ▸ Samples ▸ Hello, World ▸ Open a copy), build (Ctrl+Shift+B).
2. Start Narrator (Ctrl+Win+Enter), click in the editor.
3. Press **F11** three times. Narrator should say, after each step, something like "Paused at line 4. Changed: AX, IP."
4. Press **F5**. Narrator should say "Running." and then "Program ended with exit code 0."

| # | Expected | Result |
|---|---|---|
| 1 | "Ready at line …" after the build | Not reported separately |
| 2 | "Paused at line N. Changed: …" after each F11 | **Heard** (user: "narrator says paused and changed") |
| 3 | "Running." and "Program ended with exit code 0." after F5 | Not reported separately |

## Changes made in TASK-069
- `CodeEditor`: the inner `TextArea` takes the editor's UIA name and gets a help text (`Strings.EditorHelpText`).
- Tests: `tests/AssemblyStudio.App.Tests/Accessibility/EditorAccessibilityTests.cs` (2).

## Changelog
- 2026-11-02: Created (TASK-069). Part A measured; Part B written, not run.
- 2026-11-02: Part B run by the user ("i check narrates its work and commit"); overall pass recorded. TASK-069 closed.
- 2026-11-02: Part C (step announcements, TASK-085) added; automated part measured; manual Narrator check not run yet.
- 2026-11-02: Part C manual check run by the user ("i check narrator says paused and changed, commit"): the step announcement is spoken. TASK-085 closed.
