# Research — DOS Service Subset and .COM Loading

| Field | Value |
|---|---|
| Document | Research: DOS services and .COM program loading |
| Status | Approved |
| Version | 1.0 |
| Last updated | 2026-10-26 |
| Depends on | `docs/01-requirements/PRODUCT_REQUIREMENTS.md` (set) v1.0 |

**Method.** Primary source: Microsoft's MS-DOS source release, **MIT license** ✔
(https://github.com/microsoft/MS-DOS, `LICENSE`: "MIT License, Copyright (c) Microsoft Corporation"). It contains
the full kernel for v1.25 and v4.0. The v2.0 console I/O body (`IO.ASM`) is missing. Secondary source: Ralf
Brown's Interrupt List r61. Paths are relative to `microsoft/MS-DOS/main/`, and line numbers are approximate.
Sources were read on 2026-10-26 by a research agent. The claims marked ✔ were independently re-fetched by the lead agent.
We use the source **only as a behavior reference**. No code is copied.

## 1. Questions
OQ-003 (exact service semantics), OQ-004 (.COM loading), and the FR-034 initial state. Related: FR-039–FR-042,
FR-063, FR-064, D-008.

## 2. Service behavior (target: the DOS 3.3/4.0 behavior from the v4.0 source)
| Function | Inputs → outputs | Echo / special characters | Ctrl-C check | Evidence |
|---|---|---|---|---|
| **01h** read char with echo | → AL = char | Echoed through the console output path (TAB expands, control characters raw). ^S pauses. ^P toggles printer echo, and those keys are consumed. ^Z is not EOF | Yes | [Verified: v4.0/src/DOS/CPMIO2.ASM, CPMIO.ASM, CTRLC.ASM `STATCHK`] |
| **02h** write char | DL = char → **AL = last char output** (DL=09h returns **AL=20h**) | TAB pads to the next multiple of 8 using the DOS column counter. CR sets the column to 0. BS decrements the column (not destructive). DEL and other bytes below 20h are output raw without advancing the column | v4: every 64th character. v1.25: every character | [Verified: CPMIO2.ASM `CTRLOUT`/`TAB`; v4.0/src/DOS/DISP.ASM `LEAVE` writes AL back] |
| **07h** direct input, no echo | → AL = char | No echo | **No.** ^C, ^S and ^P are returned as ordinary characters | [Verified: CPMIO.ASM `$RAW_CON_INPUT`] |
| **08h** input, no echo | → AL = char. Extended keys return AL=00h, and the next call returns the scan code | No echo | Yes (^S, ^P handled) | [Verified: CPMIO.ASM] |
| **09h** write string | DS:DX → '$'-terminated string. **Returns AL=24h** | Each character goes through the 02h output path. '$' is not printed. No length limit. A missing '$' wraps within the segment (inferred) | As 02h | [Verified: CPMIO.ASM; v1.25 `PRTBUF`] |
| **0Ah** buffered input | DS:DX → buffer: `[0]` = max including CR, `[1]` = count excluding CR (template on entry), `[2..]` = chars + CR | Max 0 returns at once. When full, the extra character is dropped and **BEL** is output. Control characters echo as `^X`. Enter stores CR and **echoes CR only (no LF)**. BS/DEL erase destructively (2 cells for `^X`). LF echoes CR LF and isn't stored. Cancel (**ESC** on IBM builds, ^X otherwise) prints `\` + CR LF and restarts the line. Template keys (F1–F7, INS, DEL) edit against the template | Yes | [Verified: v4.0/src/DOS/STRIN.ASM, DOSMES.ASM `ESCTAB`, MSSW.ASM] |
| **4Ch** terminate | AL = return code. Never returns. **CS does not need to equal the PSP.** Introduced in **DOS 2.0** (v1.25 rejects it) | — | With BREAK ON only | [Verified: v4.0/src/DOS/PROC.ASM `$EXIT`; v1.25 `MAXCOM EQU 46`; v2.0 SYSCALL.txt] |
| **INT 20h** terminate | Exit code 0. **Requires CS = PSP.** Restores the INT 22h/23h/24h vectors from the PSP | — | — | [Verified: DISP.ASM `QUIT`, PROC.ASM `$ABORT`; RBIL INT 20] |

**Ctrl-C protocol (DOS 2.0+).** DOS prints `^C` CR LF and executes INT 23h. If the handler returns with IRET, the
function is retried. RETF with CF set terminates the program (exit type "^C"). v1.25 always retries.
[Verified: v4.0/src/DOS/CTRLC.ASM `CNTCHAND`]. The default COMMAND.COM handler aborts the program
[Unverified].

## 3. .COM loading and initial state
| Item | Value | Evidence |
|---|---|---|
| Segments | CS = DS = ES = SS = PSP segment | ✔ [Verified: v4.0/src/DOS/EXEC.ASM: "CS=DS=ES=SS=PDB pointer, IP=100, SP=max size of block"] |
| IP | 0100h | ✔ [Verified: EXEC.ASM `MOV exec_init_IP,100h`] |
| SP | `SI + 0FEh`, where SI is the available read size (at most FF00h), so **FFFEh** with a full 64 KiB | ✔ [Verified: EXEC.ASM `ADD SI,0FEh`] |
| Return word | **0000h** stored at SS:SP ("0 for return") | ✔ [Verified: EXEC.ASM `MOV WORD PTR [SI],0`] |
| Near RET to 0000h | Jumps to PSP:0000, which holds `INT 20h`; CS = PSP, so the program terminates with exit code 0 | [Derived from the verified facts] |
| AX | AL / AH = FFh if the first/second FCB drive is invalid, else 00h | [Verified: EXEC.ASM; RBIL] |
| BX, CX, DX, SI, DI, BP | **Not set by DOS** (leftover values) | [Verified by absence; values Unverified] |
| PSP 00h | `CD 20` (INT 20h) | [Verified: v4.0/src/DOS/MISC.ASM; v4.0/src/INC/PDB.INC] |
| PSP 02h | First segment beyond the allocation | [Verified] |
| PSP 05h–09h | Far call to the DOS entry (CP/M CALL 5) | [Verified] |
| PSP 0Ah/0Eh/12h | Saved INT 22h/23h/24h vectors | [Verified] |
| PSP 16h, 18h, 2Ch | Parent PSP, job file table, environment segment (2.0+) | [Verified: PDB.INC] |
| PSP 50h | `INT 21h; RETF` | [Verified] |
| PSP 5Ch / 6Ch | Default FCBs | [Verified: EXEC.ASM] |
| PSP 80h | Command tail: length byte, text, 0Dh. Also the default DTA | [Verified: EXEC.ASM; format per RBIL] |

## 4. Recommendation R-DOS (input to an ADR)
- **Target the DOS 3.3/4.0 behavior** for the D-008 subset, faithfully, including return values (02h → AL = last
  character, 09h → AL=24h). These details are visible in the registers panel, so they matter for teaching.
- **Initial state (FR-034):** follow the verified values (CS=DS=ES=SS=PSP, IP=0100h, SP=FFFEh, word 0000h at
  FFFEh, AX=0000h). Always give the program a full 64 KiB. **Deviation:** set BX, CX, DX, SI, DI and BP to a
  documented fixed value (0000h) instead of DOS leftovers, for determinism (NFR-016).
- **PSP:** emulate 00h, 02h, 05h, 0Ah–15h, 16h, 2Ch (minimal or zero environment), 50h, 5Ch/6Ch (zeroed FCBs) and
  80h (empty tail `00 0D`).
- **Termination (FR-041):** 4Ch (any CS), INT 20h (CS = PSP, as DOS requires), and RET to PSP:0000. Consider
  adding **AH=00h** (DOS 1 terminate, same semantics as INT 20h). That needs a requirements change.
- **0Ah editing in the MVP:** BS/DEL, Enter (CR only), Esc cancel, buffer-full BEL, ^C. **Deviation for the
  MVP:** template keys (F1–F7, INS, DEL-skip) are not implemented and must be documented (FR-045).
  Candidate for P2.
- **Ctrl-C:** check on every character in 01h/02h/08h/09h/0Ah. This is a documented deviation from v4's
  every-64th-character timing, chosen for determinism and responsiveness. **No check in 07h.** Default handler:
  print `^C`, terminate with reason "Ctrl-C". If the program hooked INT 23h, follow the 2.0+ IRET/RETF protocol
  [Hypothesis on default]. **UI conflict:** Ctrl+C is "copy" in the IDE, so map Ctrl-C/Ctrl-Break to the
  program only while the console has focus. This is a design-phase item.
- **^S / ^P:** ^S (pause) is recommended. **^P (printer echo) is unsupported.** Treat it as consumed and
  document it.
- **BEL (07h) output:** the console shows a visual bell, with an optional sound (design phase).
- **Possible additions (not in the approved subset, need a requirements change):** 06h (direct console I/O) and
  0Bh (input status), which are common in exercises [Hypothesis on frequency]; 30h (get DOS version).

## 5. Unverified items and resolution
| Item | Resolution |
|---|---|
| v2.0 console I/O bodies (`IO.ASM` missing) | Irrelevant: we target the 3.3/4.0 behavior |
| Default COMMAND.COM INT 23h behavior | Our documented default (terminate) stands either way |
| Which cancel key retail builds used (ESC vs ^X) | Choose ESC (IBM PC convention) and document it |
| BX..BP leftover values | Not needed: deterministic 0000h deviation |
| Behavior of DOS 5+ retail binaries | Out of scope. 3.3/4.0 target documented |

## Changelog
- 2026-10-26 v0.1: Initial research (SDLC-004).
- 2026-10-26 v1.0: Approved by the user (2026-10-26 17:59 +0500). Content unchanged from v0.1.
