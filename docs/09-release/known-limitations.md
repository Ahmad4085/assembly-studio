# Known Limitations

| Field | Value |
|---|---|
| Document | Known limitations (input to the release notes) |
| Status | Draft |
| Version | 0.1 |
| Last updated | 2026-11-03 |
| Depends on | `docs/07-testing/defects.md`, `DECISIONS.md` (D-022), `docs/07-testing/test-report-2026-11-03-2.md`, help page "What Assembly Studio supports" (`src/AssemblyStudio.Content/Content/Help/scope.txt`) |
| Task | TASK-113 (QA 2026-11-03-2 condition 4) |

## 1. Purpose
The "Known limitations" section of the release notes (`docs/templates/release-notes.md`) is copied from §2 by
`/assembly-release`. The same limitations are in the help page "What Assembly Studio supports" (sections "Programs",
"Operating system services", "Input and output" and "Known limitations"), so users see them offline. §3 gives each item's
source. Keep §2, §3 and the help page in step; `HelpPagesTests.TASK_113_…` checks the help page's numbers against the code.

## 2. Text for the release notes
- **First start after a Windows restart:** can take several seconds (about 8 seconds was measured on an Intel Core
  i5-8265U); later starts take under 2 seconds.
- **Variables panel:** NASM doesn't record what a label's data means, so a 1-byte variable is shown as a byte and a 2-byte
  one as a word, even a text such as `db 'OK'`. At most 64 bytes of a variable are shown (a longer one shows its first 64
  bytes in hex followed by "…"). Variables are read in at most 2 regions of 4,096 bytes; a variable outside them shows
  "(not shown: too far from the other variables)".
- **Program size:** programs of 65,279 or 65,280 bytes build but don't load; keep programs at or below 65,278 bytes.
- **Processor:** 8086 only. The port I/O instructions (IN, OUT), the 8087 coprocessor (ESC) instructions and instructions
  of the 80186 and later are not supported; the help page "Unsupported instructions" lists them.
- **Programs:** DOS .COM programs only; no .EXE programs, segments or linking several source files.
- **DOS and BIOS services:** INT 21h functions 00h, 01h, 02h, 07h, 08h, 09h, 0Ah and 4Ch, INT 20h and Ctrl-C (INT 23h)
  only. No files, disks, date, time, memory allocation, and no BIOS services (INT 10h, INT 16h and so on).
- **Input and output:** text only, through the Console panel; no graphics, sound, mouse, timer or printer.
- **Assembler:** NASM syntax only (NASM 3.02 is installed); MASM and TASM syntax is not accepted.

## 3. Sources
| Item | Source | Evidence |
|---|---|---|
| First start | DEF-023 (Won't fix, approved), D-022, NFR-005 v2.1 | The user's cold-start run 2026-11-03 11:19: 7,752 ms after a restart; warm start median 1,562 ms, max 1,636 ms (test report 2026-11-03-2, TC-089). Reference machine D-014 |
| Variables panel | FR-097, D-020, TASK-106 | `VariablesViewModel.MaxShownBytes` (64), `MaxWindows` (2), `MaxWindowLength` (4096), `Describe` (1 byte as a byte, 2 bytes as a word, longer than 64 as hex with "…"), `Strings.VariableNotWatched` |
| Program size | DEF-003 (Open, Low; the user's decision) | `ComLoader.MaxComImageSize` 65,278; the build accepts 65,280 |
| Processor, programs, services, input and output, assembler | Help page "What Assembly Studio supports" (its numbers checked by `HelpTests`, `HelpConsistencyTests`, `HelpPagesTests`) | The help pages "Unsupported instructions" and "DOS services" |

**Not listed (no effect on users):** the open Low defects DEF-001, DEF-002, DEF-011, DEF-012, DEF-014, DEF-015, DEF-016 and
DEF-027 are intermittent failures of test runs (window focus, the shared clipboard, a stalled run, timing in full-solution
runs), not of the application.

## 4. Change log
- 2026-11-03 v0.1: Created (TASK-113).
