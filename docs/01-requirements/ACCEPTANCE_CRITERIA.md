# Acceptance Criteria

| Field | Value |
|---|---|
| Document | Acceptance criteria |
| Status | Approved |
| Version | 1.0 |
| Last updated | 2026-10-26 |
| Depends on | `docs/00-project-overview/project-overview.md` v0.3. `DECISIONS.md` D-006 to D-009 |
| Requirements set | Approved as one set together with `PRODUCT_REQUIREMENTS.md` |

Nothing here can be executed yet: no application code exists. The criteria are split by what they
depend on:
- **Engine-independent:** testable once the application shell, editor, assembler integration,
  settings or installer exist. They don't depend on the execution-engine decision (OQ-001).
- **Engine-dependent:** require the CPU execution engine, loader and runtime services, which aren't selected yet.

Each criterion has a test status of `NOT RUN`. Test cases (`TC-###`) are created in the testing phase and linked in `REQUIREMENTS_TRACEABILITY.md`.

## Engine-independent criteria

### AC-001 Launch offline
- **Given** the application is installed on a supported Windows version and all network adapters are disabled
- **When** the user launches it
- **Then** the main window with an editable document appears within the NFR-005 target, and no sign-in prompt or network error is shown
- Verifies: FR-001, NFR-005, NFR-008 · Priority: P0 · Test status: NOT RUN

### AC-002 Create and open source files
- **Given** the application is running
- **When** the user creates a new file from the .COM template, then opens an existing UTF-8 `.asm` file via the Open dialog and via drag-and-drop
- **Then** each document shows its exact content in its own tab titled with the file name (`Untitled` for the new file)
- Verifies: FR-002, FR-003 · Priority: P0 · Test status: NOT RUN

### AC-003 Save a source file
- **Given** an opened file has unsaved edits
- **When** the user presses Ctrl+S
- **Then** the file on disk equals the editor content and the unsaved indicator clears. Save As writes to the chosen new path and retitles the tab
- Verifies: FR-004 · Priority: P0 · Test status: NOT RUN

### AC-004 Atomic save under failure
- **Given** a save is started and the write fails part-way (simulated)
- **When** the failure occurs
- **Then** the original file on disk is unchanged, an error names the path and reason, and the editor still holds the unsaved content
- Verifies: FR-004, FR-008, NFR-029 · Priority: P0 · Test status: NOT RUN

### AC-005 Unsaved-change protection
- **Given** a document has unsaved changes
- **When** the user closes its tab or exits the application
- **Then** a Save / Don't Save / Cancel prompt appears. Cancel keeps the document open and unchanged. Save writes it before closing
- Verifies: FR-005 · Priority: P0 · Test status: NOT RUN

### AC-006 Multiple tabs
- **Given** three files are open in tabs
- **When** the user switches between them and edits each
- **Then** each tab keeps its own content, caret position and undo history, and Build uses the active tab
- Verifies: FR-006 · Priority: P1 · Test status: NOT RUN

### AC-007 Recent file no longer exists
- **Given** a recent-files entry points to a deleted file
- **When** the user selects it
- **Then** a message names the missing path and offers to remove the entry. The application continues normally
- Verifies: FR-007, FR-008 · Priority: P0 · Test status: NOT RUN

### AC-008 File access errors
- **Given** paths that (a) don't exist, (b) lack read permission, (c) are read-only on save, (d) are locked by another process
- **When** the user opens or saves them
- **Then** each case shows a message naming the path and reason, and no editor content is lost
- Verifies: FR-008 · Priority: P0 · Test status: NOT RUN

### AC-009 Syntax highlighting and line numbers
- **Given** a file containing every token category (mnemonic, register, directive, label, number, string, comment)
- **When** it is opened
- **Then** each category is rendered in its defined style and line numbers are shown
- Verifies: FR-011, FR-012 · Priority: P0 · Test status: NOT RUN

### AC-010 Editing commands
- **Given** a document is open
- **When** the user applies undo, redo, cut, copy, paste, select-all, Tab and Shift+Tab via keyboard shortcuts
- **Then** each command produces the standard result, and new lines auto-indent to the previous line
- Verifies: FR-013, FR-014 · Priority: P0 · Test status: NOT RUN

### AC-011 Find, replace and go to line
- **Given** a document containing repeated words
- **When** the user searches with match-case and whole-word on and off, replaces one and all matches, and uses Go to Line
- **Then** matches, replacements and caret positions are exactly as specified
- Verifies: FR-015, FR-016, FR-017 · Priority: P0 · Test status: NOT RUN

### AC-012 Encoding and line endings preserved
- **Given** a UTF-8-with-BOM CRLF file and a UTF-8 LF file
- **When** each is opened, edited and saved
- **Then** the BOM state and line endings of each file are unchanged, and a non-UTF-8 file opens with a visible encoding notice
- Verifies: FR-019, FR-020 · Priority: P1 · Test status: NOT RUN

### AC-013 Invalid source is reported
- **Given** a source with an error on line 7 and a warning on line 3
- **When** the user builds
- **Then** the build fails, both diagnostics appear with line and severity in the diagnostics pane (separate from console and runtime panes), both lines are marked in the editor, activating an entry moves the caret to it, and Run is not enabled
- Verifies: FR-018, FR-029, FR-065, NFR-024 · Priority: P0 · Test status: NOT RUN

### AC-014 MASM-style source hint
- **Given** a source containing `.model small` and `.code`
- **When** the user builds
- **Then** the assembler errors are shown together with a hint that NASM syntax is required and a link to help
- Verifies: FR-021 · Priority: P1 · Test status: NOT RUN

### AC-015 Assembler detection and configuration
- **Given** a bundled or configured assembler
- **When** the application starts or Settings is opened
- **Then** the assembler path, its origin and version are shown. Entering an invalid path shows a validation error and the path is not saved
- Verifies: FR-023, FR-024, FR-075 · Priority: P0 · Test status: NOT RUN

### AC-016 Missing assembler
- **Given** no usable assembler (bundled one removed, configured path invalid)
- **When** the user builds
- **Then** no build is attempted, a message explains the problem and offers to open Settings, and editing continues normally
- Verifies: FR-024, FR-030 · Priority: P0 · Test status: NOT RUN

### AC-017 Assemble a valid program
- **Given** the hello-world sample is open
- **When** the user builds
- **Then** the build succeeds and reports image size in bytes, duration and 0 warnings, address-to-source mapping data is produced, and Run becomes available
- Verifies: FR-025, FR-026, FR-028 · Priority: P0 · Test status: NOT RUN

### AC-018 Instruction beyond 8086 rejected at build
- **Given** a source using an 80186+ instruction (e.g. `pusha`)
- **When** the user builds
- **Then** the build fails with a diagnostic on that line stating the instruction is not supported for the 8086 target
- Verifies: FR-027 · Priority: P0 · Test status: NOT RUN

### AC-019 Assembler hang or crash
- **Given** an assembler stub that hangs, and one that crashes
- **When** the user builds with each
- **Then** the hanging build is cancelled after the timeout and reported, the crash is reported, and the next build works without restarting
- Verifies: FR-025, FR-030, FR-095, NFR-010, NFR-014 · Priority: P0 · Test status: NOT RUN

### AC-020 .COM size limit
- **Given** a source whose image exceeds 65,280 bytes
- **When** the user builds
- **Then** the build fails with a message stating the .COM size limit
- Verifies: FR-031 · Priority: P0 · Test status: NOT RUN

### AC-036 Tutorials and reference offline
- **Given** networking is disabled
- **When** the user opens each tutorial and the instruction reference
- **Then** all content displays, and each tutorial's sample link opens its sample
- Verifies: FR-070, FR-071 · Priority: P1 · Test status: NOT RUN

### AC-038 Version information and release notes
- **Given** the application is running offline
- **When** the user opens About / Diagnostics and the release notes
- **Then** the application, assembler, engine, .NET and OS versions are shown as copyable text and the release notes display
- Verifies: FR-076, FR-087 · Priority: P0 · Test status: NOT RUN

### AC-039 Settings persistence and recovery
- **Given** a changed setting, and separately a corrupted settings file
- **When** the application restarts
- **Then** the changed setting is retained. With the corrupted file, the application starts with defaults, keeps a backup of the bad file and notifies the user. Defaults build the samples without configuration
- Verifies: FR-079, FR-080, FR-081 · Priority: P0 · Test status: NOT RUN

### AC-041 Uninstall and upgrade
- **Given** an installed version with user source files and changed settings elsewhere on disk
- **When** a newer version is installed over it, then uninstalled
- **Then** the upgrade keeps settings, and the uninstall removes program files and shortcuts while user source files remain
- Verifies: FR-085, FR-088, NFR-027 · Priority: P0 · Test status: NOT RUN

### AC-042 Third-party notices
- **Given** the installed application
- **When** the user opens third-party notices
- **Then** every bundled component is listed with its license text, matching the release license inventory
- Verifies: FR-090, NFR-030 · Priority: P0 · Test status: NOT RUN

### AC-046 Zoom
- **Given** editor and debug panels are visible
- **When** the user presses Ctrl+Plus repeatedly, then Ctrl+Minus
- **Then** text scales within the defined range without clipping and returns to the original size
- Verifies: FR-022 · Priority: P1 · Test status: NOT RUN

### AC-050 Crash recovery
- **Given** unsaved edits made more than 60 s ago
- **When** the application process is killed and restarted
- **Then** the user is offered recovery of the edits
- Verifies: NFR-011 · Priority: P1 · Test status: NOT RUN

### AC-051 Limitations documented
- **Given** the built-in help
- **When** a reviewer checks it against the implemented scope
- **Then** supported instructions, services, formats, assembler options and known deviations are listed and match the implementation
- Verifies: FR-032, FR-045, NFR-033 · Priority: P0 · Test status: NOT RUN

### AC-053 Editor preferences and reset
- **Given** the user changes font, tab width, spaces/tabs and theme
- **When** the application restarts, then settings are reset
- **Then** preferences persist across the restart and return to defaults after the reset
- Verifies: FR-077, FR-082 · Priority: P1 · Test status: NOT RUN

### AC-055 Error message structure
- **Given** the message catalog
- **When** it is reviewed
- **Then** every user-facing error has a stable code, what happened, where, and a next step
- Verifies: FR-066, NFR-024 · Priority: P1 · Test status: NOT RUN

### AC-057 Modular, headless-testable build
- **Given** the solution
- **When** it is reviewed and the test command runs
- **Then** engine, runtime, assembler integration and UI are separate components (no UI references from core), the full suite runs headless with one command, and core libraries have zero warnings and meet the coverage target
- Verifies: NFR-019, NFR-020, NFR-021 · Priority: P0 · Test status: NOT RUN

### AC-059 Display scaling
- **Given** Windows scaling at 100%, 125%, 150% and 200%, on two monitors with different DPI
- **When** the main window is used and moved between monitors
- **Then** layouts render without clipping or blurring at a minimum window size of 1024x768
- Verifies: NFR-026 · Priority: P1 · Test status: NOT RUN

### AC-060 Localizable strings
- **Given** the source code
- **When** it is inspected
- **Then** no user-facing string is hard-coded outside resource files
- Verifies: NFR-032 · Priority: P1 · Test status: NOT RUN

### AC-061 Supply-chain record
- **Given** a release candidate
- **When** the release checklist runs
- **Then** every dependency is pinned, has a source, license and maintenance note, bundled binaries match recorded checksums, and an SBOM exists
- Verifies: NFR-031 · Priority: P1 · Test status: NOT RUN

### AC-063 Local diagnostics log
- **Given** errors occur during use
- **When** the log folder is inspected
- **Then** errors are logged locally, total size stays within the cap, and no log data is transmitted
- Verifies: NFR-012 · Priority: P1 · Test status: NOT RUN

## Engine-dependent criteria

### AC-021 Program loaded with documented initial state
- **Given** a successful build
- **When** the user starts a debug session
- **Then** the image is at offset 0100h of the program segment, the PSP area is initialized as documented, and the registers show the documented initial values
- Verifies: FR-033, FR-034 · Priority: P0 · Test status: NOT RUN

### AC-022 Execute a supported sample
- **Given** the built hello-world sample
- **When** the user runs it
- **Then** the console shows exactly the documented output, termination via INT 21h AH=4Ch is reported with exit code 0, and the program does not run natively on the host
- Verifies: FR-035, FR-039, FR-041, FR-051, FR-063 · Priority: P0 · Test status: NOT RUN

### AC-023 Keyboard input
- **Given** a sample that reads a line via INT 21h AH=0Ah and prints it
- **When** the user runs it, types `abc` and presses Enter
- **Then** the console shows a waiting indicator, echoes the typed text, and the program prints `abc`
- Verifies: FR-039, FR-064 · Priority: P0 · Test status: NOT RUN

### AC-024 Step through instructions
- **Given** a loaded program paused at its first instruction
- **When** the user presses Step Into
- **Then** exactly one instruction executes, IP advances to the next instruction (or jump target), and the current-instruction display and source highlight move accordingly within the NFR-002 target
- Verifies: FR-049, FR-053, NFR-002 · Priority: P0 · Test status: NOT RUN

### AC-025 Register and flag changes are visible
- **Given** execution paused before `add al, bl` with AL=7Fh and BL=01h
- **When** the user steps
- **Then** AL shows 80h and the flags show OF=1, SF=1, ZF=0, AF=1, PF=0, CF=0. AL and each changed flag are highlighted with a non-color cue, and the displayed values equal the engine state
- Verifies: FR-046, FR-048, FR-056, NFR-018 · Priority: P0 · Test status: NOT RUN

### AC-026 Memory and stack inspection
- **Given** execution paused before `push ax` with AX=1234h and SP=FFFEh
- **When** the user steps
- **Then** SP shows FFFCh, the stack view shows 1234h at the top with SP marked, the memory view at SS:FFFC shows bytes `34 12`, and the changed bytes are highlighted
- Verifies: FR-054, FR-055, FR-056 · Priority: P0 · Test status: NOT RUN

### AC-027 Pause, stop and reset
- **Given** a running program
- **When** the user pauses, then stops, then resets
- **Then** Pause shows the current state, Stop ends the session, and Reset reloads the image, restores initial registers, clears the console, and the next run behaves like the first
- Verifies: FR-035, FR-051 · Priority: P0 · Test status: NOT RUN

### AC-028 Unsupported instruction at run time
- **Given** an image containing opcode bytes outside the supported set (e.g. `db 0Fh, 0A2h`)
- **When** execution reaches them
- **Then** execution halts before them with CS:IP, the bytes and 'unsupported instruction' shown, and the state remains inspectable
- Verifies: FR-036, FR-037 · Priority: P0 · Test status: NOT RUN

### AC-029 Unsupported interrupt service
- **Given** a program calling INT 21h with AH=3Dh (open file)
- **When** it executes the call
- **Then** execution halts with a message naming INT 21h function 3Dh as unsupported, and no host file is accessed
- Verifies: FR-040, NFR-013 · Priority: P0 · Test status: NOT RUN

### AC-030 Runaway program
- **Given** a program with an endless loop
- **When** the user runs it
- **Then** the UI stays responsive within NFR-004 limits, Pause and Stop take effect within 500 ms, and on reaching the instruction budget execution pauses with an explanatory message
- Verifies: FR-042, FR-067, FR-096, NFR-004, NFR-010, NFR-015 · Priority: P0 · Test status: NOT RUN

### AC-031 Divide error
- **Given** execution paused before `div bl` with BL=0
- **When** the user steps
- **Then** execution halts with a 'divide error' runtime message at that CS:IP
- Verifies: FR-038 · Priority: P0 · Test status: NOT RUN

### AC-032 Breakpoints
- **Given** a breakpoint on source line 10 of a looping program
- **When** the user runs
- **Then** execution pauses before the line-10 instruction each time it is reached, and after the breakpoint is removed the run continues to termination
- Verifies: FR-052 · Priority: P1 · Test status: NOT RUN

### AC-033 Step over
- **Given** execution paused at a `call` to a procedure containing 20 instructions
- **When** the user presses Step Over
- **Then** execution pauses at the instruction after the call, unless a breakpoint inside the procedure is hit first
- Verifies: FR-050 · Priority: P1 · Test status: NOT RUN

### AC-034 Core workflows offline
- **Given** networking is disabled and a network monitor is active
- **When** the user opens a sample, builds, runs, steps and opens a tutorial
- **Then** every action succeeds and the monitor records no network requests from the application
- Verifies: FR-001, NFR-008, NFR-009 · Priority: P0 · Test status: NOT RUN

### AC-035 Samples verified
- **Given** the installed application
- **When** each built-in sample is opened, built and run by the automated suite
- **Then** each sample's output equals its documented expected output, and opening a sample creates an untitled copy
- Verifies: FR-068, FR-069 · Priority: P0 · Test status: NOT RUN

### AC-037 Explanation versus actual state
- **Given** the reference entry for `ADD`
- **When** it is compared with the engine's instruction tests
- **Then** its flags-affected statement matches the tested behavior, and explanatory panels are visually distinct from live CPU state
- Verifies: FR-072, NFR-018 · Priority: P0 · Test status: NOT RUN

### AC-040 Clean install and first run
- **Given** a clean supported Windows x64 machine without .NET or an assembler installed and without internet
- **When** the installer runs
- **Then** installation completes without downloads within the NFR-027 target, a Start menu shortcut exists, and the application launches and builds and runs the hello-world sample
- Verifies: FR-083, FR-084, FR-086, NFR-025, NFR-027 · Priority: P0 · Test status: NOT RUN

### AC-043 Keyboard-only core workflow
- **Given** a mouse is not used
- **When** the user opens a sample, builds, runs, sets a breakpoint, steps and reads registers
- **Then** every step is possible by keyboard and the focused element is always visibly indicated
- Verifies: FR-091, FR-092, NFR-022 · Priority: P0 · Test status: NOT RUN

### AC-044 Screen reader and high contrast
- **Given** Windows Narrator is running and a high-contrast theme is active
- **When** the user steps a program
- **Then** Narrator reads register names and values and announces the new paused location, and all text and controls remain readable
- Verifies: FR-093, FR-094, NFR-022 · Priority: P0 · Test status: NOT RUN

### AC-045 Output flood
- **Given** a program printing in an endless loop
- **When** the user runs it
- **Then** the console buffer stays within its cap with oldest output trimmed, the UI stays responsive and Stop works
- Verifies: FR-063, FR-096, NFR-015 · Priority: P0 · Test status: NOT RUN

### AC-047 Deterministic replay
- **Given** a program and a recorded input sequence
- **When** it is run twice
- **Then** both runs produce identical CPU state traces
- Verifies: FR-035, NFR-016 · Priority: P0 · Test status: NOT RUN

### AC-048 Running-state indication
- **Given** a program is running
- **When** the user looks at the debug panels
- **Then** they indicate that values are not current, and they refresh on pause
- Verifies: FR-057 · Priority: P1 · Test status: NOT RUN

### AC-049 Execution history
- **Given** a program paused after 20 single steps
- **When** the user opens the execution history
- **Then** it lists those 20 instructions in execution order with addresses and disassembly
- Verifies: FR-058 · Priority: P1 · Test status: NOT RUN

### AC-052 Number format toggle
- **Given** execution is paused
- **When** the user switches between hex, unsigned, signed and binary
- **Then** all register values re-render in the chosen format and represent the same values
- Verifies: FR-047 · Priority: P1 · Test status: NOT RUN

### AC-054 Execution preferences honored
- **Given** the instruction budget is set to 1,000
- **When** an endless-loop program runs
- **Then** execution pauses after 1,000 instructions with the budget message
- Verifies: FR-078 · Priority: P1 · Test status: NOT RUN

### AC-056 Performance targets
- **Given** the reference machine (OQ-014)
- **When** the performance suite runs
- **Then** the measured editor latency, execution speed, startup time, build time and memory use meet the NFR targets (or the targets are revised with approval)
- Verifies: NFR-001, NFR-003, NFR-005, NFR-006, NFR-007 · Priority: P1 · Test status: NOT RUN

### AC-058 Instruction semantics coverage
- **Given** the supported instruction list
- **When** the test report is compared with it
- **Then** every supported instruction has passing tests for its operand forms and defined flags
- Verifies: NFR-017 · Priority: P0 · Test status: NOT RUN

### AC-062 Beginner usability session
- **Given** at least 5 first-time participants
- **When** each is asked to open a sample, build, run and step
- **Then** at least 80% complete within 5 minutes without outside help
- Verifies: NFR-023 · Priority: P1 · Test status: NOT RUN

## Changelog
- 2026-10-26 v0.1: Initial draft (SDLC-003, Claude Code).
- 2026-10-26 v1.0: Approved by the user (2026-10-26 17:11 +0500: "approve requirements v0.1 and D-010"). Requirement statuses PROPOSED → APPROVED. NEEDS_RESEARCH unchanged. Content otherwise identical to v0.1.
