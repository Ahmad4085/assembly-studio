# Functional Requirements

| Field | Value |
|---|---|
| Document | Functional requirements |
| Status | Approved |
| Version | 2.0 |
| Last updated | 2026-10-26 |
| Depends on | `docs/00-project-overview/project-overview.md` v0.3. `DECISIONS.md` D-006 to D-009 |
| Requirements set | Approved as one set together with `PRODUCT_REQUIREMENTS.md` |

Definitions of priority and status: `PRODUCT_REQUIREMENTS.md` §2. Every requirement reads "The system shall ..." unless stated otherwise.

## A. Application and workspace
| ID | Requirement | Priority | Status | User stories | Acceptance criteria | Notes |
|---|---|---|---|---|---|---|
| FR-001 | The application shall start to a main window with an editable document without network access, sign-in or account. | P0 | APPROVED | US-001, US-020 | AC-001, AC-034 | CON-002 |
| FR-002 | The user shall be able to create a new source file, either empty or from a starter template for a .COM program (`org 100h`). | P0 | APPROVED | US-006 | AC-002 | — |
| FR-003 | The user shall be able to open existing source files through an Open dialog and by drag-and-drop onto the main window. | P0 | APPROVED | US-006 | AC-002 | — |
| FR-004 | The user shall be able to Save and Save As. Saving shall replace the target file atomically, so a failed save never leaves a partially written file. | P0 | APPROVED | US-006 | AC-003, AC-004 | NFR-029 |
| FR-005 | Closing a tab, closing the window or exiting with unsaved changes shall prompt Save / Don't Save / Cancel. Cancel shall abort the close with content unchanged. | P0 | APPROVED | US-006 | AC-005 | — |
| FR-006 | The user shall be able to open multiple files in tabs. Build and run actions apply to the active tab. At most one program executes at a time. | P1 | APPROVED | US-014 | AC-006 | ASM-005 |
| FR-007 | The application shall keep a recent-files list (PROPOSED: 10 entries). Selecting an entry whose file no longer exists shall show a message and offer to remove the entry. | P1 | APPROVED | US-014 | AC-007 | — |
| FR-008 | File operations shall handle invalid paths, missing files, permission denied, read-only targets, locked files and full disks with a message naming the path and reason. The in-memory document is never lost. | P0 | APPROVED | US-006 | AC-004, AC-007, AC-008 | — |
| FR-009 | The application shall detect when an open file is changed outside the application and offer to reload it. | P2 | APPROVED | US-006 | — | — |
| FR-010 | Programs consisting of several source files (`%include`) shall be supported. | P2 | NEEDS_RESEARCH | US-026 | — | MVP builds a single source file (ASM-002) |

## B. Code editor
| ID | Requirement | Priority | Status | User stories | Acceptance criteria | Notes |
|---|---|---|---|---|---|---|
| FR-011 | The editor shall highlight NASM syntax: 8086 instruction mnemonics, registers, directives, labels, numeric literals (hex, binary, decimal, character), strings and comments. | P0 | APPROVED | US-014 | AC-009 | D-006 |
| FR-012 | The editor shall show line numbers, use a monospaced font and highlight the current line. | P0 | APPROVED | US-014 | AC-009 | — |
| FR-013 | The editor shall support undo, redo, cut, copy, paste and select-all with standard Windows keyboard shortcuts. | P0 | APPROVED | US-014 | AC-010 | — |
| FR-014 | The editor shall support block indent and unindent (Tab / Shift+Tab), auto-indent of new lines to the previous line's indentation, and a configurable tab width with tabs or spaces. | P1 | APPROVED | US-014 | AC-010 | — |
| FR-015 | The editor shall provide Find with match-case and whole-word options and find next/previous. | P0 | APPROVED | US-014 | AC-011 | — |
| FR-016 | The editor shall provide Replace and Replace All. | P1 | APPROVED | US-014 | AC-011 | — |
| FR-017 | The editor shall provide Go to Line. | P1 | APPROVED | US-014 | AC-011 | — |
| FR-018 | Build diagnostics shall be marked in the editor on the affected line (marker plus gutter icon, distinguishable for errors and warnings) and listed in the diagnostics pane. Activating a list entry moves the caret to that line. | P0 | APPROVED | US-005 | AC-013 | — |
| FR-019 | Files shall open correctly as UTF-8 (with or without BOM) and ASCII. Files that are not valid UTF-8 open using the system ANSI code page with a visible notice. Saving keeps the file's encoding and BOM state. New files are UTF-8 without BOM. | P1 | APPROVED | US-006 | AC-012 | — |
| FR-020 | Saving shall preserve a file's existing line endings (CRLF or LF). New files use CRLF. Mixed line endings are normalized only after user confirmation. | P1 | APPROVED | US-006 | AC-012 | — |
| FR-021 | When a build fails and the source contains common MASM/TASM-only constructs (e.g. `.model`, `.code`, `.data`, `assume`), the diagnostics shall add a hint that NASM syntax is required, linking to the relevant help. | P1 | APPROVED | US-005 | AC-014 | D-006, OQ-018 |
| FR-022 | The user shall be able to zoom the editor and debug panels (PROPOSED range 50-300%) with Ctrl+Plus/Minus and Ctrl+mouse wheel. | P1 | APPROVED | US-017 | AC-046 | Instructor use |

## C. Assembler integration
| ID | Requirement | Priority | Status | User stories | Acceptance criteria | Notes |
|---|---|---|---|---|---|---|
| FR-023 | The application shall use a bundled assembler when its license permits redistribution, otherwise a user-configured assembler path. The active assembler path and its origin (bundled or configured) shall be visible. | P0 | NEEDS_RESEARCH | US-015 | AC-015 | OQ-002 (redistribution) |
| FR-024 | Before building, the application shall verify the assembler exists and runs, read its version, and warn if it is older than the minimum supported version. | P0 | APPROVED | US-015 | AC-015, AC-016 | Minimum version: OQ-002 |
| FR-025 | The assembler shall be invoked without a shell, with an argument list, in an isolated per-build temporary directory, with a timeout (PROPOSED: 10 s), an output-size cap and user cancellation. Unsaved editor content is assembled from a temporary copy without forcing a save. | P0 | APPROVED | US-007 | AC-017, AC-019 | NFR-014 |
| FR-026 | In the MVP, builds shall produce a flat binary .COM image with origin 0100h. No other output format is offered. | P0 | APPROVED | US-007 | AC-017 | OQ-004 |
| FR-027 | Builds shall restrict the instruction set to the supported CPU (e.g. NASM `CPU 8086`), so instructions the engine does not support are reported as build errors at their source line. | P0 | NEEDS_RESEARCH | US-010 | AC-018 | OQ-002, OQ-016 |
| FR-028 | A successful build shall report success, image size in bytes, build duration and warning count, and produce the address-to-source mapping data used by the debugger. | P0 | APPROVED | US-007 | AC-017 | OQ-009 |
| FR-029 | Assembler output shall be parsed into diagnostics (file, line, severity, message) mapped to source lines. Output that cannot be parsed is shown unmodified. | P0 | APPROVED | US-005 | AC-013 | — |
| FR-030 | Build failures shall be reported distinctly: assembler missing (message plus link to Settings), timeout (build cancelled), non-zero exit without diagnostics (raw output), assembler crash. The application remains usable in every case. | P0 | APPROVED | US-015, US-016 | AC-016, AC-019 | — |
| FR-031 | A build whose .COM image exceeds 65,280 bytes (64 KiB minus the 256-byte PSP) shall fail with a message stating the .COM size limit. | P0 | APPROVED | US-007 | AC-020 | — |
| FR-032 | The built-in help shall document the supported assembler, syntax, output format and the options the application passes to the assembler. | P1 | APPROVED | US-010 | AC-051 | — |

## D. Program execution
| ID | Requirement | Priority | Status | User stories | Acceptance criteria | Notes |
|---|---|---|---|---|---|---|
| FR-033 | Programs shall execute only inside an emulated 8086 real-mode machine with a 1 MiB address space, never as native code on the host. | P0 | NEEDS_RESEARCH | US-002 | AC-021 | Engine choice: OQ-001, OQ-008 |
| FR-034 | The loader shall place the .COM image at offset 0100h of a program segment with a minimal PSP at 0000h-00FFh and set initial registers per the documented DOS .COM convention (CS=DS=ES=SS=program segment, IP=0100h, SP near the segment top). The exact values are confirmed by research and documented. | P0 | NEEDS_RESEARCH | US-002 | AC-021 | OQ-003, OQ-004 |
| FR-035 | The user shall be able to Run, Pause, Stop and Reset. Reset reloads the original image and restores the initial machine state and console. | P0 | APPROVED | US-002, US-018 | AC-022, AC-027, AC-047 | — |
| FR-036 | The supported instruction set shall be documented. MVP target: the documented 8086 instruction set, excluding 8087 coprocessor (ESC) instructions, with defined behavior for HLT, WAIT and LOCK. The final list depends on research. | P0 | NEEDS_RESEARCH | US-010 | AC-028 | OQ-001, OQ-016 |
| FR-037 | When execution reaches an unsupported or invalid opcode, execution shall halt before it with the state preserved, showing the address (CS:IP), instruction bytes and reason. | P0 | APPROVED | US-010 | AC-028 | — |
| FR-038 | CPU-generated interrupts (divide error, INTO overflow, single-step, INT 3) with no handler installed by the program shall halt execution with a runtime error naming the condition and address. | P0 | APPROVED | US-010 | AC-031 | — |
| FR-039 | The runtime shall emulate the MVP DOS service subset for text I/O and termination (D-008, D-012): INT 21h functions 00h, 01h, 02h, 07h, 08h, 09h, 0Ah, 4Ch and INT 20h, with DOS 3.3/4.0 semantics and the documented deviations in `docs/02-research/dos-services.md`. | P0 | APPROVED | US-008 | AC-022, AC-023, AC-064 | D-008, D-012, OQ-003 (answered) |
| FR-040 | Any interrupt or service function outside the supported set shall halt execution with a message naming the interrupt and function (e.g. INT 21h AH=3Dh). | P0 | APPROVED | US-010 | AC-029 | — |
| FR-041 | Normal termination (INT 20h, INT 21h AH=00h, INT 21h AH=4Ch, or RET to PSP:0000) shall end the session and report the termination path and the exit code where one exists. | P0 | APPROVED | US-008 | AC-022, AC-064 | D-012 |
| FR-042 | Each run shall have an instruction budget (PROPOSED default: 10,000,000, configurable). When it is exceeded, execution pauses with an explanatory message and the user may continue or stop. | P0 | APPROVED | US-016 | AC-030 | NFR-015 |
| FR-043 | MZ .EXE programs shall be loaded and executed. | P2 | NEEDS_RESEARCH | US-026 | — | OQ-004 |
| FR-044 | A documented subset of BIOS video and keyboard services (INT 10h, INT 16h) shall be emulated. | P2 | NEEDS_RESEARCH | US-026 | — | D-008 (Post-MVP) |
| FR-045 | The built-in help shall list supported instructions, interrupts and service functions, executable formats and known deviations from real hardware. | P0 | APPROVED | US-010 | AC-051 | — |

## E. Debugging and CPU visualization
| ID | Requirement | Priority | Status | User stories | Acceptance criteria | Notes |
|---|---|---|---|---|---|---|
| FR-046 | A registers panel shall show AX, BX, CX, DX (with their 8-bit halves), SI, DI, BP, SP, CS, DS, ES, SS, IP and FLAGS, in hexadecimal by default. | P0 | APPROVED | US-003 | AC-025 | — |
| FR-047 | The user shall be able to switch value display between hexadecimal, unsigned decimal, signed decimal and binary. | P1 | APPROVED | US-003 | AC-052 | — |
| FR-048 | A flags panel shall show CF, PF, AF, ZF, SF, TF, IF, DF and OF individually with name and short description. Flags left undefined by the last instruction are displayed as undefined when the engine reports this. | P0 | APPROVED | US-003 | AC-025 | Undefined-flag reporting: OQ-015 |
| FR-049 | Step Into shall execute exactly one instruction and then update every debug view. | P0 | APPROVED | US-003 | AC-024 | — |
| FR-050 | Step Over shall execute a CALL or INT and run until control returns to the next instruction, stopping earlier at breakpoints, errors or termination. | P1 | APPROVED | US-012 | AC-033 | — |
| FR-051 | Run shall execute continuously until a breakpoint, Pause, termination, a runtime error or the instruction budget. | P0 | APPROVED | US-002, US-011 | AC-022, AC-027 | — |
| FR-052 | The user shall be able to toggle breakpoints on source lines that map to instructions. Execution pauses before executing a breakpointed instruction. Breakpoints persist while the file stays open. | P1 | APPROVED | US-011 | AC-032 | OQ-009 |
| FR-053 | While paused, the application shall show the current instruction (CS:IP, bytes, disassembly) and highlight its source line when mapping data exists. Otherwise it states that no source mapping is available. | P0 | APPROVED | US-003 | AC-024 | OQ-009 |
| FR-054 | A memory view shall show memory as hexadecimal and ASCII at a user-entered segment:offset, defaulting to the program image. | P0 | APPROVED | US-009 | AC-026 | — |
| FR-055 | A stack view shall show words from SS:SP towards the stack top, with SP marked. | P0 | APPROVED | US-009 | AC-026 | — |
| FR-056 | After each step or pause, registers, flags and memory bytes changed since the previous pause shall be highlighted using a cue that does not rely on color alone. | P0 | APPROVED | US-003, US-009 | AC-025, AC-026 | — |
| FR-057 | While a program is running, debug views shall state that values are not current, and refresh on pause. | P1 | APPROVED | US-016 | AC-048 | — |
| FR-058 | An execution history shall list the most recently executed instructions in order (PROPOSED: last 1,000) with address and disassembly. Forward history only. | P1 | APPROVED | US-013 | AC-049 | — |
| FR-059 | Memory access shall follow 8086 real-mode semantics (no protection, 20-bit wrap-around). Optional warnings for writes to the interrupt vector table or PSP shall be available. | P2 | APPROVED | US-025 | — | 8086 has no invalid addresses |
| FR-060 | Reverse stepping (step back) shall restore the previous machine state exactly. | P2 | NEEDS_RESEARCH | US-025 | — | OQ-017 |
| FR-061 | Run to Cursor shall run until the instruction at the caret's source line. | P2 | APPROVED | US-025 | — | — |
| FR-062 | While paused, the user shall be able to edit register, flag and memory values. | P2 | APPROVED | US-025 | — | — |

## F. Program output and diagnostics
| ID | Requirement | Priority | Status | User stories | Acceptance criteria | Notes |
|---|---|---|---|---|---|---|
| FR-063 | A program console panel shall show characters written by the program through supported services, interpreting CR, LF, BS and TAB. It is cleared on Run and Reset. | P0 | APPROVED | US-002, US-008 | AC-022, AC-045 | D-008 |
| FR-064 | When the program waits for keyboard input, the console shall show a waiting indicator and pass keystrokes to the emulated input. Buffered line input (INT 21h AH=0Ah) supports editing per the documented semantics. | P0 | APPROVED | US-008 | AC-023 | D-008, OQ-003 |
| FR-065 | Build diagnostics, program console output and runtime messages (errors, termination, budget) shall be shown in visually separate panes. | P0 | APPROVED | US-005 | AC-013 | — |
| FR-066 | Every build and runtime error message shall state what happened, where (source line or CS:IP) and a suggested next step, and carry a stable message code. | P1 | APPROVED | US-005 | AC-055 | NFR-024 |
| FR-067 | Assembling and executing shall never block the user interface. Pause and Stop remain available while a program runs. | P0 | APPROVED | US-016 | AC-030 | NFR-004 |

## G. Learning resources
| ID | Requirement | Priority | Status | User stories | Acceptance criteria | Notes |
|---|---|---|---|---|---|---|
| FR-068 | The application shall include built-in sample programs (PROPOSED: at least 8) covering text output, arithmetic and flags, loops, conditional jumps, stack and procedures, string instructions, keyboard input and addressing modes. Each sample's expected output is verified by an automated test. | P0 | APPROVED | US-002 | AC-035 | — |
| FR-069 | Opening a sample shall open an untitled copy. The originals are read-only and Save As writes to a user location. | P1 | APPROVED | US-002 | AC-035 | — |
| FR-070 | The application shall include beginner tutorials (PROPOSED: at least 5: registers, flags, memory and addressing, stack, loops and branches), each linked to a sample with guided steps. | P1 | APPROVED | US-004 | AC-036 | — |
| FR-071 | An offline reference shall explain registers, flags, segmented memory, the stack and each supported instruction (operation and flags affected). | P1 | APPROVED | US-004 | AC-036 | — |
| FR-072 | Explanatory content shall be visibly distinct from live CPU state. Instruction reference statements (e.g. flags affected) shall agree with the engine's tested behavior. | P0 | APPROVED | US-004, US-019 | AC-037 | NFR-018 |
| FR-073 | Exercises with automatic checking of program output shall be provided. | P2 | APPROVED | US-026 | — | — |
| FR-074 | A presentation mode shall apply a large-font, high-visibility layout for projectors in one action. | P2 | APPROVED | US-017 | — | — |

## H. Settings and tool configuration
| ID | Requirement | Priority | Status | User stories | Acceptance criteria | Notes |
|---|---|---|---|---|---|---|
| FR-075 | The user shall be able to configure the assembler path. The path is validated (exists, executes, reports a version) before it is saved. | P0 | APPROVED | US-015 | AC-015 | — |
| FR-076 | An About / Diagnostics view shall show the application version, assembler path and version, execution engine identity and version, .NET runtime version and OS version, as copyable text. | P0 | APPROVED | US-015 | AC-038 | — |
| FR-077 | The user shall be able to set editor preferences: font family and size, tab width, tabs versus spaces, and theme (dark or light). | P1 | APPROVED | US-022 | AC-053 | — |
| FR-078 | The user shall be able to set execution preferences: instruction budget and default number format. | P1 | APPROVED | US-022 | AC-054 | — |
| FR-079 | With default settings and the bundled assembler (if permitted), a new installation shall build and run samples without any configuration. | P0 | APPROVED | US-001 | AC-039 | Depends on FR-023 |
| FR-080 | Settings shall be stored locally per Windows user in a human-readable file and survive restarts. | P0 | APPROVED | US-022 | AC-039 | NFR-009 |
| FR-081 | If the settings file is missing, unreadable or invalid, the application shall start with defaults, keep a backup of the invalid file and tell the user. | P0 | APPROVED | US-022 | AC-039 | — |
| FR-082 | The user shall be able to reset all settings to defaults. | P1 | APPROVED | US-022 | AC-053 | — |

## I. Installation and updates
| ID | Requirement | Priority | Status | User stories | Acceptance criteria | Notes |
|---|---|---|---|---|---|---|
| FR-083 | A Windows x64 installer shall install the application, its runtime dependencies, the assembler (if redistribution is permitted), samples, tutorials, documentation and third-party notices, without network access. | P0 | APPROVED | US-001 | AC-040 | Packaging approach: OQ-013 |
| FR-084 | The installer shall create a Start menu shortcut and optionally associate .asm files (PROPOSED: off by default). | P1 | APPROVED | US-001 | AC-040 | — |
| FR-085 | Uninstalling shall remove installed program files and shortcuts. It shall not delete user source files. The documented behavior for settings (PROPOSED: kept) applies. | P0 | APPROVED | US-023 | AC-041 | — |
| FR-086 | Prerequisites shall be documented and either bundled or checked by the installer. Installation shall not download anything. | P0 | NEEDS_RESEARCH | US-001 | AC-040 | OQ-005, OQ-013 |
| FR-087 | The application version and release notes shall be viewable offline from within the application. | P0 | APPROVED | US-024 | AC-038 | — |
| FR-088 | Installing a newer version over an older one shall preserve user settings. | P1 | APPROVED | US-023 | AC-041 | — |
| FR-089 | Automatic updates or update checks. | P3 | APPROVED | — | — | Out of scope until approved. Core use never depends on updates |
| FR-090 | License texts and notices for every bundled third-party component shall be installed and viewable from within the application. | P0 | APPROVED | US-024 | AC-042 | NFR-030 |

## J. Accessibility and error recovery
| ID | Requirement | Priority | Status | User stories | Acceptance criteria | Notes |
|---|---|---|---|---|---|---|
| FR-091 | Every essential workflow (file operations, editing, build, run, step, breakpoints, panel navigation) shall be operable by keyboard alone. Shortcuts are shown in menus and tooltips. | P0 | APPROVED | US-021 | AC-043 | NFR-022 |
| FR-092 | A visible focus indicator shall be shown on the focused element in every theme. | P0 | APPROVED | US-021 | AC-043 | NFR-022 |
| FR-093 | Controls and register/flag values shall expose UI Automation names and values. Changes of execution state (paused at line N, error, terminated) shall be announced to screen readers. | P1 | APPROVED | US-021 | AC-044 | NFR-022 |
| FR-094 | The application shall honor Windows high-contrast themes and Windows text scaling. | P1 | APPROVED | US-021 | AC-044 | NFR-022, NFR-026 |
| FR-095 | If the assembler or the execution engine fails, the application shall stay usable with open documents intact, report the failure, and allow the next build or run without restarting. | P0 | APPROVED | US-016 | AC-019 | NFR-010 |
| FR-096 | Malformed or hostile programs (endless loops, self-modifying code, output floods, overwriting the interrupt vector table) shall not crash or hang the application, and shall not access the host system. | P0 | APPROVED | US-016 | AC-030, AC-045 | NFR-010, NFR-013, NFR-015 |

## Changelog
- 2026-10-26 v0.1: Initial draft (SDLC-003, Claude Code).
- 2026-10-26 v1.0: Approved by the user (2026-10-26 17:11 +0500: "approve requirements v0.1 and D-010"). Requirement statuses PROPOSED → APPROVED. NEEDS_RESEARCH unchanged. Content otherwise identical to v0.1.
- 2026-10-26 v2.0: **Substantive change (SDLC-005)** applying user-approved decisions D-011 (supported Windows: CON-001, NFR-025, AC-040, ASM-001, OQ-010) and D-012 (INT 21h AH=00h: FR-039, FR-041, new AC-064, OQ-022). Changed requirements are `PROPOSED` until re-approval. Set status `Awaiting approval`.
- 2026-10-26 v2.0: **Approved** by the user (2026-10-26 18:18 +0500: "approve requirements v2.0"). FR-039, FR-041, NFR-025 → APPROVED.
