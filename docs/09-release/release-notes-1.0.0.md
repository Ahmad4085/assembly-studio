# Assembly Studio 1.0.0: Release Notes

Release date: 2026-11-04

## Highlights
- Write, build, run and debug 8086 assembly programs for DOS (.COM), in one window, fully offline.
- NASM 3.02 is included. A new installation builds and runs the samples with no setup.
- Step through programs and watch registers, flags, memory, the stack and your variables change. Changed values are marked.
- Samples, tutorials, an instruction reference and help are built in.
- Free and open source under the MIT License.

## New features
This is the first release.

| Feature | Requirements |
|---|---|
| Files: new from a template, open, save (atomic), tabs, recent files, unsaved-changes prompts, crash recovery of unsaved edits | FR-001..FR-008, FR-019, FR-020, NFR-011 |
| Editor: NASM syntax highlighting, line numbers, current line, undo/redo, block indent, find, replace, go to line, zoom | FR-011..FR-017, FR-022 |
| Build with the bundled NASM: diagnostics on the source lines, a hint for MASM/TASM syntax, clear errors with a next step | FR-018, FR-021, FR-023..FR-032, FR-065, FR-066 |
| Run inside an emulated 8086 PC (1 MB, real mode): Run, Pause, Stop, Reset, an instruction budget; the IDE never freezes | FR-033..FR-042, FR-067, FR-095, FR-096 |
| DOS text services: INT 21h functions 00h, 01h, 02h, 07h, 08h, 09h, 0Ah, 4Ch, INT 20h, Ctrl-C; a console panel with keyboard input | FR-039..FR-041, FR-063, FR-064 |
| Debugging: Step Into, Step Over, breakpoints, current instruction with disassembly, execution history | FR-049..FR-053, FR-057, FR-058 |
| Panels: registers (with 8-bit halves and number formats), flags, memory (hex and ASCII), stack, variables | FR-046..FR-048, FR-054..FR-056, FR-097 |
| Learn: 8 samples, 5 tutorials, an instruction and concept reference, help pages | FR-045, FR-068..FR-072 |
| Settings: assembler path, editor and execution preferences, reset; About with diagnostics, release notes and notices | FR-075..FR-082, FR-087, FR-090 |
| Accessibility: keyboard for every workflow, visible focus, screen-reader names and announcements, dark, light and high-contrast themes | FR-091..FR-094 |

## Fixes
None: this is the first release.

## Supported scope
- **Processor:** Intel 8086 in real mode with 1 MB of memory. Every documented 8086 instruction except IN, OUT and the 8087 (ESC)
  instructions. 80186 and later instructions are not supported.
- **Assembler:** NASM syntax, assembled by the bundled NASM 3.02. A different NASM, 3.02 or later, can be set in Settings.
- **Programs:** DOS .COM programs of up to 65,278 bytes, starting at offset 100h.
- **Services:** the DOS functions listed above. No files, disks, clock, memory allocation or BIOS services.

## Known limitations
- **First start after a Windows restart:** can take several seconds (about 8 seconds was measured on an Intel Core i5-8265U).
  Later starts take under 2 seconds.
- **Variables panel:** NASM doesn't record what a label's data means, so a 1-byte variable is shown as a byte and a 2-byte one
  as a word, even a text such as `db 'OK'`.
  - At most 64 bytes of a variable are shown; a longer one shows its first 64 bytes in hex followed by "…".
  - Variables are read in at most 2 regions of 4,096 bytes. A variable outside them shows "(not shown: too far from the other
    variables)".
- **Program size:** .COM programs can be up to 65,278 bytes (64 KiB minus the 256-byte PSP and the 2-byte stack word). Real DOS
  allows 65,280.
- **Processor:** 8086 only. IN, OUT, the 8087 (ESC) instructions and instructions of the 80186 and later are not supported; the
  help page "Unsupported instructions" lists them.
- **Programs:** DOS .COM programs only. No .EXE programs, segments, or linking several source files.
- **DOS and BIOS services:** only the INT 21h functions listed above, INT 20h and Ctrl-C (INT 23h). No files, disks, date, time
  or memory allocation, and no BIOS services (INT 10h, INT 16h and so on).
- **Input and output:** text only, through the Console panel. No graphics, sound, mouse, timer or printer.
- **Assembler:** NASM syntax only. MASM and TASM syntax is not accepted.
- **Tested systems:** this release was tested on one Windows 11 PC. Installing on a clean machine and running on Windows 10 LTSC
  were not tested.
- **Not yet tested with users:** the planned first-use session with new users hasn't been run.
- **English only.**
- **The installer is not code-signed,** so Windows may warn before running it (see the install guide).

## System requirements
- **Windows:** 64-bit Windows 11, or Windows 10 version 21H2 (build 19044) or later. Only Windows 11 was tested.
- **Disk:** about 190 MB. The installed size is 185 MB.
- **No .NET installation needed:** the runtime is included. Nothing is downloaded, during installation or afterwards.

## Installation and upgrade
Run `AssemblyStudio-1.0.0-setup.exe` and accept the license page. It installs for the current user and needs no administrator
rights. A newer version installs over an older one and keeps your settings. Uninstalling removes the program but never your
source files or settings. See `docs/09-release/install-guide.md`.

## Third-party components
Assembly Studio includes:
- the Microsoft .NET runtime and WPF;
- AvalonEdit, CommunityToolkit.Mvvm and Iced;
- NASM 3.02.

Their licenses are in `THIRD-PARTY-NOTICES.txt` in the installation folder, and in Help → About → Third-party notices. Six .NET
files are under Microsoft terms, which you accept on the installer's license page.
