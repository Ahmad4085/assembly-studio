# Research — Assembler (NASM) and Source Mapping

| Field | Value |
|---|---|
| Document | Research: assembler and source mapping |
| Status | Approved |
| Version | 1.0 |
| Last updated | 2026-10-26 |
| Depends on | `docs/01-requirements/PRODUCT_REQUIREMENTS.md` (set) v2.0 |

**Method.** `[Measured]` items were executed by the lead agent on 2026-10-26 against the locally installed
**NASM 3.02** (`C:\Program Files\Netwide Assembler 3.02`, Windows 11) using scratch files outside the
repository. This was tool measurement, not a POC. `[Verified: URL]` items were read by a research agent on the same day.

## 1. Questions
OQ-002 (redistribution, minimum version, CPU restriction, listing/map), OQ-004 (.COM vs MZ), OQ-009 (source
mapping), and OQ-018 for the record. Related: FR-023–FR-032, FR-021, FR-043, NFR-014, NFR-030.

## 2. Findings: NASM
| Attribute | Finding | Evidence |
|---|---|---|
| License | **BSD-2-Clause** (`SPDX-License-Identifier: BSD-2-Clause`, © 1996-2025 the NASM Authors). Binary redistribution must reproduce the notice, conditions and disclaimer in the documentation or other materials. No copyleft | [Measured: local `LICENSE` file] [Verified: https://raw.githubusercontent.com/netwide-assembler/nasm/master/LICENSE] |
| Latest stable | 3.02 (homepage date 2026-06-29). 3.01 (2025-10-10), 3.00 (2025-10-03) | [Verified: https://www.nasm.us/] [Verified: https://www.nasm.us/pub/nasm/releasebuilds/] |
| Windows binaries | `nasm-3.02-win64.zip` (executable only, 626K) and an x64 installer. No `.sig` files listed. Authenticode signature not checked | [Verified: https://www.nasm.us/pub/nasm/releasebuilds/3.02/win64/] |
| `CPU 8086` | `pusha`, `shl ax, 2` and `push 1234h` each give `error: no instruction for this cpu level`, exit code 1, and no output file is written. Without the directive all three assemble | [Measured] [Verified: https://www.nasm.us/docs/3.02/nasmdoc8.html §8.11] |
| Forcing the CPU from outside | `--before "cpu 8086"` enforces the restriction on source with no directive. **A later `cpu 386` in user source overrides it** (exit 0) | [Measured] |
| Map without touching user source | `--before "[map all <file>]"` produces the map. User source needs no NASM-specific directives | [Measured] |
| Diagnostics | GNU format (default): `file:line: error\|warning\|info: message [-w+class]`. `-Xvc`: `file(line) : …`. `-Z file` redirects to a file. Warnings still exit 0, errors exit 1 | [Measured] [Verified: https://www.nasm.us/docs/3.02/nasmdoc2.html §2.1.15–17] |
| Listing (`-l`) | Columns: line number, section-relative offset (8 hex digits), bytes, source. Relocated values are shown in brackets (`BA[0000]`). Long data wraps onto continuation lines with the same line number. **Offsets restart at 0 for each section.** Exact layout is not documented | [Measured] [Verified: nasmdoc2.html §2.1.3 (format undocumented)] |
| Listing on failure | **Written even when assembly fails**, with errors inline (`****************** error: …`) | [Measured] |
| Map (`[map all]`) | Gives each section's `Vstart`/`Start` (e.g. `.text` 100h, `.data` 10Ch, default align 4 for `.data`) plus symbols | [Measured] [Verified: https://raw.githubusercontent.com/netwide-assembler/nasm/master/doc/outfmt.src] |
| Reproducible builds | `--reproducible` produced byte-identical output on two runs | [Measured] |
| Build time | 80–143 ms per build for a 13-line program (5 runs, this machine). Not a benchmark | [Measured] |
| MASM-style input | `.model small` gives `error: instruction expected, found '.model small'`. These patterns are recognizable for FR-021 hints | [Measured] |
| MZ .EXE | Two documented routes: (a) `-f obj` plus a 16-bit linker; (b) **`-f bin` plus the `exebin.mac` macros (MZ header, no linker)** shipped in `misc/` | [Verified: https://raw.githubusercontent.com/netwide-assembler/nasm/master/doc/16bit.src] [Verified: https://github.com/netwide-assembler/nasm/tree/master/misc]. `exebin.mac` license header: [Unverified] |
| `ndisasm` | Bundled 16-bit disassembler, same license. Not suitable for per-step use (process per call) [Hypothesis] | [Measured: `ndisasm -b 16 -o 100h`] |

## 3. Alternatives
| Option | License | Status | Verdict |
|---|---|---|---|
| YASM | BSD-2 (bitvect component Artistic/GPL/LGPL; binary distribution allowed under BSD/Artistic) | Last tag v1.3.0 (2014). Master still gets fixes (2026-07-31) | Rejected: no release in 12 years, NASM-compatible only in part [Hypothesis] |
| FASM | BSD-like with a no-relicensing clause (Debian copy) | Active | Rejected: different syntax (D-006) |
| UASM / JWasm | OWPL 1.0 / license unclear | — | Out of scope (MASM syntax, D-006) |
| Linkers for MZ (ALINK, OpenWatcom wlink, JWlink) | ALINK unclear (2000). wlink OWPL 1.0 (weak copyleft with source-notice duty) | — | Not needed if the `exebin.mac` route is used for P2 |

## 4. Source mapping (OQ-009)
**Finding [Measured]:** absolute address = listing offset + section `Vstart` from the map. Mapping each source
line to an address therefore needs the listing (line → section-relative offset), the map (section start
addresses) and knowledge of which section each line belongs to. NASM appears to track that only through
`section` directives in source order [Hypothesis].

**Not yet covered:** macros (`-Lm`), `times` and `align` (`-Lt`), `%include`, and multi-line data.
Proposed POC-002.

## 5. Recommendation R-ASM (input to an ADR)
- **Bundle NASM 3.02 (win64 zip)** with its `LICENSE` text in the third-party notices (FR-023, NFR-030).
- **Invoke with fixed flags:** `-f bin --before "cpu 8086" --before "[map all <tmp>.map]" -l <tmp>.lst -Xgnu
  --reproducible -o <tmp>.com`, using an argument list and a temporary directory (FR-025, FR-026, NFR-014).
  Minimum supported version: 3.02, the version measured.
- **CPU-directive bypass (gap in FR-027):** scan the source (or `-E` preprocessed output) for `cpu`
  directives and reject or warn. Keep FR-037 (runtime halts on unsupported opcodes) as defense in depth.
- **Diagnostics parser:** parse the GNU format, including `info:` and `[-w+class]` suffixes (FR-029). Show the
  listing's inline errors as a fallback.
- **MZ .EXE (P2, FR-043):** use the `exebin.mac` route (no linker). Check its license before use.

**Gaps left for us:** the source-mapping algorithm, CPU-directive detection, and reading the diagnostic
format, which NASM documents only by example.

## 6. Unverified items and resolution
| Item | Resolution |
|---|---|
| Mapping with macros, `times`, `align`, `%include` | POC-002 |
| Authenticode signature of `nasm.exe` | Check with `Get-AuthenticodeSignature` before bundling (release phase) |
| `exebin.mac` license header | Read it before FR-043 work (P2) |
| Full 3.x changelog (the last ~20% was not read) | Low impact. Re-read before pinning the version |

## Changelog
- 2026-10-26 v0.1: Initial research (SDLC-004).
- 2026-10-26 v1.0: Approved by the user (2026-10-26 17:59 +0500). Content unchanged from v0.1.
- 2026-10-26: Minor. *Depends on* updated to requirements v2.0 after revalidation: v2.0 applies D-011/D-012, which came from this research. No content change.
