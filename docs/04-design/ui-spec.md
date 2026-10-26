# Assembly Studio — UI/UX Specification

| Field | Value |
|---|---|
| Document | UI/UX specification |
| Status | Approved |
| Version | 1.0 |
| Last updated | 2026-10-26 |
| Depends on | `docs/01-requirements/` set v2.0, `docs/03-architecture/architecture.md` v1.0, ADR-0006, ADR-0007 v1.1, `docs/04-design/design-tokens.md` v1.0 |

This spec defines what to build. It builds nothing. All visual values come from `design-tokens.md`. Quality bar:
`docs/sdlc/guides/desktop-ui-quality.md`.

## 1. Design principles
| ID | Principle | Requirements |
|---|---|---|
| DP-1 | **Machine state is one glance away.** Registers and flags stay visible while a program is loaded, next to the code that changes them | FR-046, FR-048, PG-1 |
| DP-2 | **Truth over illustration.** Panels show only snapshot data. Stale, undefined and unsupported states are stated, never guessed or hidden | NFR-018, FR-057, FR-072, FR-048 |
| DP-3 | **Explain the change.** After every step the change is visible: highlight, weight and marker, and an announcement for screen readers | FR-056, FR-093 |
| DP-4 | **Calm density.** An IDE, not a dashboard: neutral surfaces, monospaced values, color only for meaning. No cards, tiles or decorative charts | desktop-ui-quality §1 |
| DP-5 | **Keyboard first, accessible always.** Every command has a key path. Focus is always visible. Works in high contrast and at 200 % | FR-091–FR-094, NFR-022, NFR-026 |
| DP-6 | **Recoverable.** Every error says what, where and what next. Nothing the user typed is lost | FR-005, FR-008, FR-066, NFR-011, NFR-024 |

## 2. Layout
```
┌ Assembly Studio — hello.asm ─────────────────────────────────────────────────────── ─ □ ✕ ┐
│ File  Edit  View  Build  Debug  Learn  Help                                                 │ UI-002 menus
│ [Build] [▶ Run] [❚❚ Pause] [■ Stop] [↺ Reset] [↓ Step Into] [↷ Step Over]  ● Paused · line 7 │ UI-002 command bar + state badge
├────────┬──────────────────────────────────────────────────────────────┬────────────────────┤
│ Learn  │ hello.asm ×  loop.asm ×                                       │ REGISTERS   hex ▾  │
│ ────── │ ➜ 0107h  B4 09        mov ah, 09h          (current instr.)   │ AX 0905  BX 0000   │ UI-005
│Samples │  1 ; hello                                                    │ CX 0000  DX 010C   │
│Tutorial│  2 org 100h                                                   │ SP FFFE  BP 0000   │
│Ref.    │ ●5   mov dx, msg                                              │ SI 0000  DI 0000   │
│        │➜ 7   mov ah, 09h          ← execution line                    │ CS 0800 … IP 0107  │
│ UI-015 │  8   int 21h                                                  │ FLAGS              │ UI-006
│        │                                     UI-003 editor + UI-007     │ O D I T S Z A P C  │
│        │                                                               │ 0 0 1 0 0 0 0 1 0  │
├────────┴──────────────────────────────┬───────────────────────────────┴────────────────────┤
│ Console │ Problems │ Runtime │ History │ Breakpoints   │ Memory  DS:010C ▾ │ Stack SS:FFFE  │
│ Hello, World!                          │ 0800:0100 B4 09 CD 21 …  ´..!     │ FFFE 0000 ◂SP  │
│ UI-010…UI-014 (tab group)              │ UI-008                            │ UI-009         │
├────────────────────────────────────────┴───────────────────────────────────┴────────────────┤
│ Build OK · 28 bytes · 0 warnings   Ln 7, Col 5   UTF-8   CRLF   NASM 3.02   Paused · 1,204 instr. │ UI-016
└──────────────────────────────────────────────────────────────────────────────────────────────┘
```
- **Regions:** command area (top); Learn sidebar (left, collapsible, default collapsed after the first run); editor (center); machine
  column (right: registers above flags); bottom area (left tab group: Console, Problems, Runtime, History, Breakpoints; right: Memory
  and Stack); status bar.
- **Resizing:** regions are separated by splitters (keyboard-resizable via View ▸ Resize Panel, arrow keys). Minimum window
  1024 × 768. Below 1280 px width the Learn sidebar auto-collapses and Memory/Stack join the bottom tab group.
- **Docking:** fixed regions with collapsible and resizable panes and tab groups. **Floating or re-docking panels are out of scope for
  the MVP** (no docking library in the approved ADRs).
- **Persistence:** region sizes, collapsed state, selected tabs, number format, zoom and theme are stored in settings (FR-080) and
  restored. "View ▸ Reset Layout" restores defaults.
- **Focus targets:** the editor has initial focus. F6/Shift+F6 cycle editor → machine column → bottom area → Learn sidebar.

## 3. Screens and panels
Data sources are the architecture's interfaces (`docs/03-architecture/architecture.md` §2). Panels marked † use the snapshot content added by
the ADR-0007 v1.1 amendment (§10, approved 2026-10-26).

| ID | Panel / screen | Purpose and content | Data source | Requirements |
|---|---|---|---|---|
| UI-001 | Main window and layout | Regions, splitters, focus cycling, theme, scaling, persistence | `ISettingsService` (layout) | FR-065, FR-067, FR-091, FR-092, FR-093, FR-094, NFR-022, NFR-026, NFR-032 |
| UI-002 | Menus and command bar | All commands (§6). Session state badge (text + glyph, e.g. "● Paused · line 7"). Cancel build | `ICommandService`, `IDebugSession` state | FR-004, FR-025, FR-035, FR-049, FR-050, FR-051, FR-052, FR-061, FR-067, FR-091, NFR-004 |
| UI-003 | Editor | Tabs, text, gutter (line numbers, breakpoint margin, execution arrow), squiggles, current/execution line, encoding/EOL handling, zoom, MASM hint link, external-change banner (P2) | `IDocumentService`; `BuildResult.Diagnostics`; `ISourceMap` + `MachineSnapshot.Location` | FR-002, FR-003, FR-006, FR-009, FR-010, FR-011, FR-012, FR-013, FR-014, FR-018, FR-019, FR-020, FR-021, FR-022, FR-052, FR-053, NFR-001 |
| UI-004 | Find/Replace bar and Go to Line | Inline bar above the editor (match case, whole word, next/previous, replace, replace all) and a Go to Line dialog | `IDocumentService` | FR-015, FR-016, FR-017 |
| UI-005 | Registers panel | AX, BX, CX, DX (with AH/AL … halves on expand), SI, DI, BP, SP, CS, DS, ES, SS, IP, FLAGS. Formats: hex, unsigned, signed, binary. Changed-value marking | `MachineSnapshot.Cpu` | FR-034, FR-046, FR-047, FR-056, FR-057, FR-062, NFR-002, NFR-018 |
| UI-006 | Flags panel | O D I T S Z A P C as a row of labeled cells: 0, 1, or "?" for undefined (tooltip and UIA name give the full name and meaning). Changed marking | `MachineSnapshot.Cpu.Flags` + `UndefinedFlagsMask` | FR-048, FR-056, FR-057, NFR-018 |
| UI-007 | Current-instruction strip | At the top of the editor while loaded: `CS:IP`, bytes, disassembly (NASM syntax). "No source mapping for this address" when unmapped. Unsupported-instruction details on halt | `MachineSnapshot.CurrentInstruction` † | FR-037, FR-053 |
| UI-008 | Memory panel | Hex + ASCII rows of 16 bytes at a user-entered `seg:off` (default DS:program). Changed bytes marked. Unknown/out-of-window rows never invented | `MachineSnapshot.MemoryWindows` † | FR-054, FR-056, FR-059, FR-062, NFR-018 |
| UI-009 | Stack panel | Words from SS:SP upward (default 16), SP marker, return-address hint when the word follows a CALL (P2) | `MachineSnapshot.StackWindow` † | FR-055, FR-056, NFR-018 |
| UI-010 | Console panel | Program output (CR, LF, BS, TAB, visual BEL). Input line when waiting ("⌨ Waiting for input"). Buffer-cap notice. Ctrl-C routing (§6) | `MachineSnapshot.ConsoleDelta`, `IDebugSession.SendInput` | FR-039, FR-063, FR-064, FR-096 |
| UI-011 | Problems panel | Build diagnostics: severity glyph, code, message, file:line, "next step" text. MASM hint. Assembler missing/timeout/crash, with a link to Settings. Activating an entry jumps to the line | `BuildResult` (`Diagnostic`, failure kind) | FR-018, FR-021, FR-024, FR-026, FR-027, FR-029, FR-030, FR-031, FR-066, FR-095, NFR-024 |
| UI-012 | Runtime messages panel | Session events: loaded, breakpoint hit, terminated (path + exit code), halted (unsupported instruction/service, runtime error), budget reached (Continue / Stop), engine error | `MachineSnapshot.SessionState` + `StepResult` | FR-037, FR-038, FR-040, FR-041, FR-042, FR-065, FR-066, FR-095, FR-096, NFR-024 |
| UI-013 | Execution history panel | Last 1,000 executed instructions (newest last): `CS:IP`, bytes, disassembly, source line. Activating an entry selects its line (read-only). Reverse step (P2) | `MachineSnapshot.HistoryDelta` † | FR-058, FR-060 |
| UI-014 | Breakpoints panel | List: file:line, address, bound/unbound state (unbound = line has no code). Enable/disable/remove | `IDebugSession.Breakpoints` + `ISourceMap` | FR-052 |
| UI-015 | Learn sidebar | Samples (open as untitled copy), tutorials (step lists with "open sample" and "show me" links), instruction and concept reference, help: supported scope and limitations | `IContentCatalog` | FR-032, FR-045, FR-068, FR-069, FR-070, FR-071, FR-072, FR-073, NFR-023, NFR-033 |
| UI-016 | Status bar | Build summary (OK/failed, bytes, warnings), Ln/Col, encoding, line ending, NASM version (warning glyph if below 3.02), session state and instruction count | `BuildResult`, `IDocumentService`, `IAssembler` version, `MachineSnapshot` | FR-019, FR-020, FR-024, FR-028, FR-057 |
| UI-017 | Settings dialog | Assembler path (validate + version), editor (font, size, tabs/spaces, width, theme), execution (budget, default format), layout reset, reset all | `ISettingsService`, `IAssembler` | FR-023, FR-075, FR-077, FR-078, FR-079, FR-080, FR-082 |
| UI-018 | About / Diagnostics | Versions (app, NASM path and origin, engine, .NET, OS) as copyable text. Release notes. Third-party notices | `ISettingsService`, `IAssembler`, assembly metadata, content files | FR-023, FR-076, FR-087, FR-090 |
| UI-019 | Message dialogs | Unsaved changes (Save / Don't Save / Cancel), file errors (path + reason + action), corrupt-settings notice, confirmations | `IDocumentService`, `ISettingsService` | FR-004, FR-005, FR-007, FR-008, FR-081, NFR-024, NFR-029 |
| UI-020 | Welcome / empty state | No document: New, Open, Open sample, recent files. Drop target for files | `IDocumentService`, `IContentCatalog`, `ISettingsService` (recent) | FR-001, FR-002, FR-003, FR-007, FR-069, NFR-005, NFR-023 |
| UI-021 | Recovery prompt | On start after an unexpected exit: list recoverable documents with Restore / Discard | `IDocumentService` (recovery folder, architecture §5) | NFR-011 |
| UI-022 | Presentation mode (P2) | One action: larger code and value fonts, thicker execution and focus markers | `ISettingsService` | FR-022, FR-074 |

Not app UI (engine or installer only): FR-033, FR-036, FR-043, FR-044, FR-083–FR-086, FR-088, FR-089 (Inno Setup's own wizard follows its defaults).

## 4. Interaction states
**Session states** (from `MachineSnapshot.SessionState`), plus document states:

| State | Meaning |
|---|---|
| Empty | No document open |
| Editing | Document open, no current build (never built, build stale, or session stopped) |
| Assembling | Build in progress (cancellable) |
| Build failed | Last build has errors. No image, no map |
| Ready | Built and loaded, not started (initial snapshot shown) |
| Running | Executing freely. Snapshots throttled (≤ 30/s) and marked not current |
| Paused | Stopped at a step, breakpoint, Pause, or budget |
| Stepping | Executing one Step Into/Over (transient. Commands disabled until the snapshot arrives) |
| Waiting for input | Program blocked on keyboard input |
| Halted: error | Runtime error (divide error, engine fault). State inspectable |
| Halted: unsupported | Unsupported instruction or DOS/BIOS service. State inspectable |
| Terminated | Program ended normally (path + exit code). Last state inspectable |

"Disabled" in the procedure's sense means Empty, Editing, Assembling and Build failed for the machine panels.

**State matrix** (✓ = normal content; values shown are always the latest snapshot):

| Panel | Empty | Editing | Assembling | Build failed | Ready | Running | Paused | Stepping | Waiting for input | Halted: error | Halted: unsupported | Terminated |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| UI-003 Editor | Welcome (UI-020) | ✓ editable | ✓ editable, status "Building…" | ✓ squiggles + markers | ✓ breakpoints bound/unbound shown | ✓ editable. Execution arrow hidden, header "Running…" | ✓ execution arrow + line | ✓ as Paused | ✓ arrow on the INT line | ✓ arrow + error marker on the line | ✓ arrow + "unsupported" marker | ✓ last line marked "ended here" |
| UI-007 Instruction strip | hidden | hidden | hidden | hidden | ✓ first instruction | "Running — not current" | ✓ | ✓ (previous until done) | ✓ the INT instruction | ✓ + error text | ✓ + bytes + "not supported" reason | ✓ last instruction |
| UI-005 Registers / UI-006 Flags | Empty state: "Build and run a program to see the CPU" | same | same | "Fix build errors to inspect the CPU" | ✓ initial values | dimmed values + "Running — values not current" | ✓ + changes marked | ✓ | ✓ | ✓ | ✓ | ✓ final values |
| UI-008 Memory / UI-009 Stack | Empty state (as registers) | same | same | same as registers | ✓ | dimmed + "not current" | ✓ + changes | ✓ | ✓ | ✓ | ✓ | ✓ |
| UI-010 Console | "Program output appears here" | same | same | same | empty (cleared) | ✓ live (throttled) | ✓ | ✓ | ✓ + input line focused, "⌨ Waiting for input" | ✓ | ✓ | ✓ + "— program ended (exit code n) —" |
| UI-011 Problems | "No problems" | last build's problems (marked "outdated" if edited) | "Building…" | ✓ list (errors first) | ✓ warnings or "No problems" | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| UI-012 Runtime messages | "Session messages appear here" | same | same | same | "Loaded at 0800:0100" | "Running…" | reason paused (step, breakpoint, Pause, budget + Continue/Stop) | — | "Waiting for keyboard input" | error with what/where/next | unsupported item with what/where/next | termination path + exit code |
| UI-013 History | "No instructions executed yet" | same | same | same | same | dimmed, updates on pause | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| UI-014 Breakpoints | list (unbound) | list (unbound) | list | list (unbound) | ✓ bound addresses | ✓ | ✓ hit one highlighted | ✓ | ✓ | ✓ | ✓ | ✓ |
| UI-002 Command bar | New/Open only | Build, Run enabled | Cancel build only | Build, Run (rebuilds) | Run, Step Into/Over, Reset, Stop | Pause, Stop | Run, Steps, Reset, Stop | none | Pause, Stop | Reset, Stop | Reset, Stop | Reset, Build, Run |
| UI-016 Status bar | app version | doc info | "Building…" + Cancel | "Build failed · n errors" | "Build OK · bytes" + "Ready" | "Running · count" | "Paused · line n · count" | as Paused | "Waiting for input" | "Halted: error" | "Halted: unsupported" | "Ended · exit n" |

**Changed-value highlighting (FR-056):**
- A value that differs from the previous **paused** snapshot is drawn in `Value.Changed` + `FontWeight.Changed`, with a 2 px left
  marker bar (cell) or underline (memory byte). It stays until the next step or pause.
- Flags that changed show the marker and their new 0/1. Flags that are **undefined** after the instruction show "?" (never 0 or 1)
  with the tooltip "Undefined after `<instruction>`".
- Screen readers get one polite announcement per step: "Paused at line 7. Changed: AX, DX, ZF."
- Nothing is highlighted while Running.

## 5. Editor behavior
- **Highlighting** (FR-011): XSHD for NASM 8086 using the `Syntax.*` tokens. Off in high contrast. Unknown words stay `Syntax.Text`
  (no guessing).
- **Gutter:** line numbers (FR-012), a breakpoint margin (click or F9 toggles. ● bound, ○ unbound, tooltip shows the address or "no
  code on this line"), an execution arrow ➜ (`Accent.ExecutionArrow`) with the execution line background (`Editor.ExecutionLine`).
  A macro line shows "(macro expansion)" in the instruction strip (`FromMacroExpansion`).
- **Diagnostics** (FR-018): wavy underline per severity (error, warning, info) + gutter glyph (⨯, ▲, ⓘ). Hover or Ctrl+K, Ctrl+I shows
  the message. The squiggle range is the line's text (NASM reports lines only).
- **Editing** (FR-013, FR-014): standard Windows editing. Tab/Shift+Tab indent and unindent the selection. Auto-indent new lines.
  **Ctrl+M toggles "Tab moves focus"** (escape route from the editor, DP-5).
- **Encoding and line endings** (FR-019, FR-020): shown in the status bar. Non-UTF-8 files show a banner: "Opened as Windows-1252,
  saving keeps this encoding". Mixed line endings show a banner with "Normalize to CRLF" (confirmation required).
- **Editing during a session:** allowed. The session is marked stale. Banner: "Changed since build — positions may not match.
  Reset (Ctrl+Shift+F5) rebuilds." Breakpoints keep their lines and are re-bound on the next build.
- **Zoom** (FR-022): Ctrl+Plus/Minus/0 and Ctrl+wheel apply to editor and machine panels together, 50–300 %. The value is shown
  briefly in the status bar.
- **MASM hint** (FR-021): Problems shows an extra info entry: "This looks like MASM/TASM syntax. Assembly Studio uses NASM syntax —
  see Help ▸ NASM vs MASM".

## 6. Commands and keyboard
| Command | Shortcut | Menu | Enabled when |
|---|---|---|---|
| New | Ctrl+N | File | always |
| Open… | Ctrl+O | File | always |
| Save / Save As… | Ctrl+S / Ctrl+Shift+S | File | document open |
| Close tab | Ctrl+W, Ctrl+F4 | File | document open |
| Recent files | Alt+F, R | File | list not empty |
| Exit | Alt+F4 | File | always |
| Undo / Redo | Ctrl+Z / Ctrl+Y | Edit | history available |
| Cut / Copy / Paste / Select all | Ctrl+X / Ctrl+C / Ctrl+V / Ctrl+A | Edit | editor focused |
| Find / Find next / previous | Ctrl+F / F3 / Shift+F3 | Edit | document open |
| Replace | Ctrl+H | Edit | document open |
| Go to Line | Ctrl+G | Edit | document open |
| Toggle "Tab moves focus" | Ctrl+M | Edit | editor focused |
| Zoom in / out / reset | Ctrl+Plus / Ctrl+Minus / Ctrl+0 | View | always |
| Next / previous pane | F6 / Shift+F6 | View | always |
| Go to Registers / Flags / Memory / Stack | Ctrl+Alt+R / Ctrl+Alt+F / Ctrl+Alt+M / Ctrl+Alt+K | View | always |
| Go to Console / Problems / Runtime / History / Breakpoints / Learn | Ctrl+Alt+O / Ctrl+Alt+P / Ctrl+Alt+U / Ctrl+Alt+H / Ctrl+Alt+B / Ctrl+Alt+L | View | always |
| Cycle number format | Ctrl+Alt+N | View | machine panel focused |
| Reset layout | Alt+V, L | View | always |
| Build | Ctrl+Shift+B | Build | document open, not running |
| Cancel build | Esc (status bar) | Build | Assembling |
| Run / Continue | F5 | Debug | document open (builds if needed), not running |
| Pause | Ctrl+Alt+Break | Debug | Running, Waiting for input |
| Stop | Shift+F5 | Debug | session active |
| Reset (rebuild + reload) | Ctrl+Shift+F5 | Debug | session active or terminated |
| Step Into | F11 | Debug | Ready, Paused |
| Step Over | F10 | Debug | Ready, Paused |
| Toggle breakpoint | F9 | Debug | editor focused |
| Run to Cursor (P2) | Ctrl+F10 | Debug | Ready, Paused |
| Send Ctrl-C to the program | Ctrl+C in the **console**, or Ctrl+Break anywhere | Debug | Running, Waiting for input |
| Copy in the console | Ctrl+Shift+C | context menu | console focused |
| Samples / Tutorials / Reference | Ctrl+Alt+L, then arrows | Learn | always |
| Settings | Ctrl+, | Tools (in File) | always |
| Help / About | F1 / Alt+H, A | Help | always |

Every command is in a menu with an access key, shows its shortcut in the menu and tooltip, and is reachable by keyboard (FR-091).
**Ctrl+C routing (OQ-021, approved, D-015):** Ctrl+C means *copy* everywhere except the focused console of an active program, where it is delivered
to the program as Ctrl-C (DOS behavior, ADR-0004). Ctrl+Break delivers Ctrl-C to the program from anywhere. Pause is Ctrl+Alt+Break,
so it never collides.

## 7. Themes and design tokens
See `docs/04-design/design-tokens.md`. Dark and light follow the Windows app mode, with an override in Settings. Windows high-contrast
mode overrides both. All 166 measured contrast pairs pass WCAG 2.2 AA (text ≥ 4.5:1, components ≥ 3:1).

## 8. Accessibility
- **Keyboard:** §6. Logical tab order within panels (header actions → content). F6 between regions. No traps (Ctrl+M for the editor).
- **Focus:** 2 px `Focus.Ring` on every focusable element in all themes (FR-092).
- **UI Automation** (FR-093): every control and value cell has `AutomationProperties.Name` (e.g. "AX register, 0905 hexadecimal,
  changed"). Flags expose the full name ("Zero flag, set"). The session state badge is a polite live region (paused location, errors,
  waiting for input, ended).
- **High contrast and scaling** (FR-094, NFR-026): system colors (design tokens §3). Text scale up to 200 % and DPI 100–200 %
  without clipping. Layouts reflow (machine column scrolls instead of clipping).
- **Motion:** none during run/step. Honors "Show animations" off.
- **Never color alone:** breakpoints, execution line, changes, errors and warnings all have a glyph, weight or marker as well.

## 9. Component conventions
| Component | Parts and states | Style key pattern |
|---|---|---|
| PanelHeader | Title (`Text.Secondary`), actions, overflow menu | `PanelHeader.*` |
| ValueCell | Name, value (`Font.Code`, tabular), states: normal, changed, undefined ("?"), stale (dimmed + tooltip) | `ValueCell.Value.Changed` |
| FlagCell | Letter, value 0/1/?, states as ValueCell, full name as tooltip and UIA name | `FlagCell.*` |
| HexByteGrid | Address column, 16 byte cells, ASCII column, changed underline, keyboard caret | `HexByteGrid.*` |
| StateBadge | Glyph + text, live region | `StateBadge.{State}` |
| Banner | Info/warning text + action buttons, dismissible, in the editor or a panel | `Banner.{Severity}` |
| EmptyState | Title, one-sentence explanation, primary action | `EmptyState.*` |
| MessageDialog | What happened, where, next step, actions (default = safest) | `MessageDialog.*` |
| CommandButton | Icon + text label (labels visible on the command bar ≥ 1280 px, tooltips always) | `CommandButton.*` |

Rules: views use only token resources (no literals). Design-time data (`d:DataContext`) only, never sample machine data at run time
(NFR-018). Icons are vector (Segoe Fluent Icons / Segoe MDL2 Assets are Windows system fonts. Availability on Windows 10 LTSC
[Unverified → shell task]).

## 10. Architecture dependency: snapshot content (OQ-023)
Panels †UI-007, UI-008, UI-009 and UI-013 need data that ADR-0007 v1.0 doesn't list in `MachineSnapshot` (it lists CPU state,
undefined mask, changed memory ranges, console delta, session state and source location). **ADR-0007 v1.1 amendment, approved 2026-10-26 19:03 +0500**:
- `CurrentInstruction`: address, bytes, disassembly text (via `IDisassembler`), source location, `FromMacroExpansion`.
- `MemoryWindows`: immutable copies of the regions the UI subscribes to (`IDebugSession.WatchMemory(seg, off, length)`, at most 4 windows of 4 KiB).
- `StackWindow`: an immutable copy of N words from SS:SP (default 16).
- `HistoryDelta`: instructions executed since the last snapshot (bounded by the 1,000-entry history).

These copies are produced by the session worker at publish time, so NFR-018 holds by construction. Cost: about 4 × 4 KiB copies per
publish (≤ 30/s while running) [Hypothesis: negligible, validated by the session benchmark].

## 11. Open design questions
| ID | Question | Impact | Status |
|---|---|---|---|
| OQ-021 | Ctrl+C routing between program and copy | Low | **Resolved** 2026-10-26 (§6 approved, D-015) |
| OQ-023 | ADR-0007 snapshot content for instruction strip, memory, stack and history panels | Medium | **Resolved** 2026-10-26 (ADR-0007 v1.1) |
| OQ-024 | Floating and re-dockable panels (needs a docking library and a new ADR) | Low | Post-MVP |
| OQ-025 | Cascadia Mono and Segoe Fluent Icons availability on Windows 10 Enterprise LTSC 21H2 | Low | Verified in the first shell task. Fallbacks defined (Consolas, Segoe MDL2 Assets) |

## Changelog
- 2026-10-26 v0.1: Initial UI/UX specification (SDLC-007).
- 2026-10-26 v1.0: **Approved** by the user (2026-10-26 19:03 +0500: "approve" (reply to the gate question naming ui-spec v0.1, design tokens v0.1, the ADR-0007 v1.1 amendment and the Ctrl+C proposal)).
