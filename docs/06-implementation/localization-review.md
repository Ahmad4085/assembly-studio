# Localizability and message review (TASK-104)

| Field | Value |
|---|---|
| Document | Implementation verification record |
| Task | TASK-104 (NFR-032, NFR-024; AC-060, AC-055; ADR-0007 "all user-facing strings live in `.resx` resources") |
| Last updated | 2026-11-03 |
| Author | Claude Code (Opus 5.5) |

Evidence labels as in `docs/sdlc/guides/sdlc-documentation.md`. Part A covers where user-facing text lives (AC-060). Part B
covers each error message's code, what, where and next step (AC-055).

## Part A: user-facing text in resource files (AC-060, NFR-032)

### Method
`ArchitectureTests.LocalizableStringTests` reads every C# and XAML file under `src` at test time:
- **C#:** it reads every string literal (regular, verbatim and interpolated, with interpolation holes removed; comments skipped).
  It flags any literal that **reads as words**: a space and at least two words of two or more letters. Register names, formats
  and notation such as `INT 20h` or `{0:X4}` don't read as words.
- **Never on screen:** a flagged literal passes if its statement is a log entry (`.Write(…LogLevel.…)`), a `Trace.WriteLine`,
  an exception for developers (`throw`, `…Exception(`), a `ToString()` override, or a `[GeneratedRegex]` or
  `[SuppressMessage]` attribute.
  - Errors from files do reach the user, but `FileErrors` shows them as a `Strings` sentence. The exception's own message
    is only the system's detail, already in Windows' language.
- **XAML:** it flags `Text`, `Content`, `Header`, `ToolTip`, `Title`, `AutomationProperties.Name` / `HelpText` / `ItemStatus`
  or `InputGestureText` attributes with literal letters, and letters as element content.
- **Allow-list:** anything else flagged must be listed in the test's `Allowed` list, with its reason. A second test fails when
  an allowed entry no longer exists.

### Findings and what was done
| Where | What | Count | Result |
|---|---|---|---|
| XAML (all windows and styles) | only icon-font glyphs and the breakpoint dot (●) | 0 words | [Measured] compliant |
| `Runtime/Interrupts/RuntimeErrorPolicy.cs` | run-time stop messages RTE000–RTE005, RTE010, RTE011, RTE020, RTE021, RTE090 (what, where, next step) | 24 texts | Moved to `RuntimeMessages.resx` (new) |
| `Os.Dos/DosServices.cs` | DOS001, DOS002 (what, next step) and "at CS:IP" | 5 texts | Moved to `DosMessages.resx` (new) |
| `Toolchain.Nasm/NasmAssembler.cs` | why an assembler can't be used (Settings, status bar, BLD001/BLD008) | 12 texts | Moved to `AssemblerProblems.resx` (new; not `BuildMessages.resx`, whose test allows only catalog codes) |
| `App/Commands/BuildRunViewModel.cs` | how the program ended (`INT 21h function 4Ch`, `RET to PSP:0000`, …) | 5 texts | Moved to `Strings.resx` (`End*`) |

The English text is unchanged. [Measured] by exact-text assertions:
- `RuntimeMessageTests.TASK_104_…` checks all 11 runtime messages.
- `DosServicesTests` checks DOS001 and DOS002.
- `NasmAssemblerTests` checks 9 of the 12 assembler texts.

The other three ("No assembler is available", "could not be started", "did not answer a version request") were compared with
the old code in the diff [Inspected]. Their conditions (no bundled path, a file Windows can't start, a version-check timeout)
have no test of their own.

### Kept as they are (in the test's allow-list, with reasons)
| What | Reason |
|---|---|
| The About diagnostics block ("Execution engine", "Log folder", "(version unknown)", …) | English on purpose so it can be pasted into a bug report as-is (FR-076; documented in `AboutViewModel.BuildDiagnostics`) |
| Startup timeline step names, thread names, the log's date pattern, "[entry truncated]" | Written to the log or seen only in a debugger |
| `Cascadia Mono, Consolas` | Font family names (default setting) |
| `INT 21h AH=00h`, `[map all]`, NASM map headings | Technical notation or NASM's own text, the same in every language |
| `CommandTable` first column ("Save As", "Go to Line", …) and its planning note | Row identifiers used only by `CommandTableTests`; the menus and tooltips use the table's `Strings` columns [Inspected: no other use in `src`] |
| `Engine.I8086/OpcodeTable.cs` (whole file) | Opcode mnemonics and notes; used only inside the engine [Inspected: no use outside the file] |

Mutation [Measured]:
- A runtime message put back as a literal fails the C# test, naming `RuntimeErrorPolicy.cs:129`.
- A `<TextBlock Text="Hello world" />` added to the Settings window fails the XAML test.

## Part B: message structure (AC-055, NFR-024)
| Catalog | Code | What | Where | Next step | Evidence |
|---|---|---|---|---|---|
| Build messages (ASM, BLD) | yes | yes | file and line when known | yes | [Measured] `BuildDiagnosticsTests.Every_catalog_code_has_its_texts…` (TC-026) |
| Runtime messages (RTE) | yes | yes | "at CS:IP", or "in the emulator, not in your program" for RTE090 | yes | [Measured] `RuntimeMessageTests` (all 11, exact text) |
| DOS service errors (DOS) | yes | yes | "at CS:IP" | yes | [Measured] `DosServicesTests` (DOS001, DOS002, exact text) |
| Application errors (open, save, settings, recovery) | the dialog's title | yes (`Strings`) | the path | yes (`…Next` strings) | [Measured] TC-026 and the dialog tests |
| Assembler problems | shown inside BLD001/BLD008 (code and next step) and in Settings | yes | the path | yes ("Correct it in Settings", "Reinstall…") | [Inspected] |

**Finding: DEF-028 (Low).** RTE021's next step shows a requirement ID ("(NFR-013)") to the user. Its list of supported DOS
functions also leaves out 00h, which is supported (`DosServices`, help page "What Assembly Studio supports"). The wording is
outside this task, which moves text without changing it.
